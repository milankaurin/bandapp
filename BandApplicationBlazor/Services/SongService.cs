using System.Net.Http;
using System.Net.Http.Json;
using Band.Shared.Domain;
using Band.Shared.Dto;
using BandApplicationBlazor.Helper;
using Microsoft.AspNetCore.Components; // NavigationManager
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.JSInterop; // LocalStorage

namespace BandApplicationFront.Services
{
    // DTO za /sessions odgovor
    internal sealed class CreateSessionResponse
    {
        public string code { get; set; } = "";
    }

    public class SongService : IAsyncDisposable
    {
        private readonly HttpClient _httpClient;
        private readonly IJSRuntime _js;
        private readonly NavigationManager _nav;
        private HubConnection? _hubConnection;
        private bool _signalRStarted;

        public event Action<NextSongResponseDto>? OnNextSongSelected;
        public event Action<bool>? OnSessionStateChanged;

        private readonly IConfiguration _config;
        private readonly string _sessionKey;
        private readonly string _hubUrl;

        public bool IsSessionActive { get; private set; }
        public string CurrentSessionCode { get; private set; } = "";

        public SongService(
            HttpClient httpClient,
            IJSRuntime js,
            NavigationManager nav,
            IConfiguration config
        )
        {
            _httpClient = httpClient;
            _js = js;
            _nav = nav;
            _config = config;
            _sessionKey =
                _config["Session:LocalStorageKey"]
                ?? throw new Exception("Session:LocalStorageKey nije definisan");

            _hubUrl =
                _config["SignalR:HubUrl"] ?? throw new Exception("SignalR:HubUrl nije definisan");
        }

        // ---------- SESSION: LocalStorage ----------
        public async Task<bool> EnsureSessionLoadedAsync()
        {
            var code = await _js.InvokeAsync<string?>("localStorage.getItem", _sessionKey);
            if (!string.IsNullOrWhiteSpace(code))
            {
                CurrentSessionCode = code!;
                return true;
            }
            return false;
        }

        private async Task SaveSessionCodeAsync(string code)
        {
            CurrentSessionCode = code;
            await _js.InvokeVoidAsync("localStorage.setItem", _sessionKey, code);
        }

        //public async Task ClearSessionAsync()
        //{
        //    var oldCode = CurrentSessionCode;

        //    CurrentSessionCode = "";
        //    await _js.InvokeVoidAsync("localStorage.removeItem", _sessionKey);

        //    try
        //    {
        //        if (!string.IsNullOrWhiteSpace(oldCode) && _hubConnection is not null)
        //            await _hubConnection.InvokeAsync("LeaveSession", oldCode);
        //    }
        //    catch { }
        //}

        public async Task ClearSessionAsync()
        {
            LoadedSongs.Current = null;
            LoadedSongs.PreviousSong = null;
            LoadedSongs.Queue = new();
            CurrentSessionCode = "";
            await _js.InvokeVoidAsync("localStorage.removeItem", _sessionKey);
        }

        // ---------- SESSION: create / join / status / start / stop ----------
        public async Task<string> CreateSessionAsync()
        {
            var resp = await _httpClient.PostAsync("sessions", null);
            resp.EnsureSuccessStatusCode();

            var obj = await resp.Content.ReadFromJsonAsync<CreateSessionResponse>();
            var code =
                (obj != null && !string.IsNullOrWhiteSpace(obj.code))
                    ? obj.code
                    : throw new Exception("Nema code iz /sessions.");

            await SaveSessionCodeAsync(code);
            await StartSignalRAsync(); // osiguraj vezu
            await JoinSignalRGroupAsync(); // uđi u grupu
            return code;
        }

        public async Task<bool> JoinSessionAsync(string code)
        {
            if (string.IsNullOrWhiteSpace(code))
                return false;

            var resp = await _httpClient.PostAsync($"sessions/{code}/join", null);
            if (!resp.IsSuccessStatusCode)
                return false;

            await SaveSessionCodeAsync(code.Trim().ToUpperInvariant());
            await StartSignalRAsync();
            await JoinSignalRGroupAsync();
            return true;
        }

        public async Task StartSessionAsync()
        {
            EnsureHasCode();
            var response = await _httpClient.PostAsync(
                $"sessions/{CurrentSessionCode}/start",
                null
            );
            response.EnsureSuccessStatusCode();
            IsSessionActive = true;
        }

        public async Task StopSessionAsync()
        {
            EnsureHasCode();
            var response = await _httpClient.PostAsync($"sessions/{CurrentSessionCode}/stop", null);
            response.EnsureSuccessStatusCode();
            IsSessionActive = false;
        }

        private void EnsureHasCode()
        {
            if (string.IsNullOrWhiteSpace(CurrentSessionCode))
                throw new InvalidOperationException("Session code nije postavljen.");
        }

        // ---------- SignalR ----------
        public async Task StartSignalRAsync()
        {
            // izbegni pattern matching sa 'or' da ne zavisiš od verzije C#
            if (
                _signalRStarted
                && _hubConnection != null
                && (
                    _hubConnection.State == HubConnectionState.Connected
                    || _hubConnection.State == HubConnectionState.Connecting
                )
            )
            {
                return;
            }

            if (_hubConnection == null)
            {
                // Hub URL: koristi BaseAddress ako postoji, inače fallback
                //var hubUrl = "https://192.168.100.8/songHub"; // eksplicitno na API portu

                _hubConnection = new HubConnectionBuilder()
                    .WithUrl(_hubUrl)
                    .WithAutomaticReconnect()
                    .Build();

                _hubConnection.On<SongHubMessage>(
                    SignalType.StateChanged,
                    async msg =>
                    {
                        switch (msg.Type)
                        {
                            case MessageTypes.NextSong:
                            case MessageTypes.QueueUpdated:
                                if (msg.Payload != null)
                                    OnNextSongSelected?.Invoke(msg.Payload);
                                break;
                            case MessageTypes.SessionStarted:
                                IsSessionActive = msg.IsSessionActive ?? false;
                                OnSessionStateChanged?.Invoke(IsSessionActive);
                                break;
                            case MessageTypes.SessionStopped:
                                IsSessionActive = false;
                                await ClearSessionAsync();
                                OnSessionStateChanged?.Invoke(false);
                                _nav.NavigateTo("/session", forceLoad: false);
                                break;
                        }
                    }
                );

                _hubConnection.Closed += async _ =>
                {
                    _signalRStarted = false;
                    await Task.Delay(Random.Shared.Next(1000, 3000));
                    try
                    {
                        await _hubConnection.StartAsync();
                        _signalRStarted = true;
                        // po želji: ponovo se pridruži grupi ako imamo code
                        if (!string.IsNullOrWhiteSpace(CurrentSessionCode))
                            await JoinSignalRGroupAsync();
                    }
                    catch { }
                };
            }

            if (_hubConnection.State == HubConnectionState.Disconnected)
                await _hubConnection.StartAsync();

            _signalRStarted = true;

            // Ako već imamo code u trenutku starta, pridruži grupi
            if (!string.IsNullOrWhiteSpace(CurrentSessionCode))
                await JoinSignalRGroupAsync();
        }

        private async Task JoinSignalRGroupAsync()
        {
            if (_hubConnection is null)
                return;
            if (string.IsNullOrWhiteSpace(CurrentSessionCode))
                return;

            try
            {
                await _hubConnection.InvokeAsync("JoinSession", CurrentSessionCode);
            }
            catch
            { /* ignore for now */
            }
        }

        public async ValueTask DisposeAsync()
        {
            try
            {
                if (_hubConnection != null)
                {
                    await _hubConnection.StopAsync();
                    await _hubConnection.DisposeAsync();
                }
            }
            catch { }
        }

        public async Task LoadSongs()
        {
            var response = await _httpClient.GetAsync("songs");
            response.EnsureSuccessStatusCode();
            LoadedSongs.ALLSONGS = await response.Content.ReadFromJsonAsync<List<Song>>() ?? new();
        }

        // ---------- Live queue (po sesiji) ----------
        public async Task GetQueueAsync()
        {
            EnsureHasCode();
            var response = await _httpClient.GetAsync($"sessions/{CurrentSessionCode}/songs/queue");
            response.EnsureSuccessStatusCode();
            LoadedSongs.Queue = await response.Content.ReadFromJsonAsync<List<Song>>() ?? new();
        }

        public async Task AddSong(Song song)
        {
            EnsureHasCode();
            var response = await _httpClient.PostAsync(
                $"sessions/{CurrentSessionCode}/songs/queue/new/{song.Id}",
                null
            );
            if (!response.IsSuccessStatusCode)
            {
                var error = await response.Content.ReadAsStringAsync();
                throw new HttpRequestException($"API greška ({response.StatusCode}): {error}");
            }
        }

        public async Task UpdateQueueOrder(List<Guid> orderedSongIds)
        {
            EnsureHasCode();
            var response = await _httpClient.PostAsJsonAsync(
                $"sessions/{CurrentSessionCode}/songs/queue/reorder",
                orderedSongIds
            );
            if (!response.IsSuccessStatusCode)
            {
                var error = await response.Content.ReadAsStringAsync();
                throw new HttpRequestException($"Reorder greška ({response.StatusCode}): {error}");
            }
        }

        public async Task<NextSongResponseDto?> NextSong()
        {
            EnsureHasCode();
            var response = await _httpClient.PostAsync(
                $"sessions/{CurrentSessionCode}/songs/queue/next",
                null
            );
            if (!response.IsSuccessStatusCode)
            {
                var error = await response.Content.ReadAsStringAsync();
                Console.WriteLine($"API greška ({response.StatusCode}): {error}");
                return null;
            }

            var dto = await response.Content.ReadFromJsonAsync<NextSongResponseDto>();
            if (dto == null)
                return null;

            LoadedSongs.Queue = dto.QueueList;
            LoadedSongs.Current = dto.CurrentSong;
            LoadedSongs.PreviousSong = dto.PreviousSong;
            return dto;
        }

        public async Task<NextSongResponseDto?> PreviousSong()
        {
            EnsureHasCode();
            var response = await _httpClient.PostAsync(
                $"sessions/{CurrentSessionCode}/songs/queue/previous",
                null
            );
            if (!response.IsSuccessStatusCode)
            {
                var error = await response.Content.ReadAsStringAsync();
                Console.WriteLine($"API greška ({response.StatusCode}): {error}");
                return null;
            }

            var dto = await response.Content.ReadFromJsonAsync<NextSongResponseDto>();
            if (dto == null)
                return null;

            LoadedSongs.Queue = dto.QueueList;
            LoadedSongs.Current = dto.CurrentSong;
            LoadedSongs.PreviousSong = dto.PreviousSong;
            return dto;
        }

        //public async Task CloseSessionAsync()
        //{
        //    EnsureHasCode();

        //    var response = await _httpClient.PostAsync(
        //        $"sessions/{CurrentSessionCode}/close",
        //        null
        //    );

        //    response.EnsureSuccessStatusCode();

        //    // lokalno čišćenje (SignalR će ionako poslati SessionStopped)
        //    await ClearSessionAsync();
        //}

        public async Task CloseSessionAsync()
        {
            var code = CurrentSessionCode;

            await ClearSessionAsync();

            if (!string.IsNullOrWhiteSpace(code) && _hubConnection != null)
            {
                try
                {
                    await _hubConnection.InvokeAsync("LeaveSession", code);
                }
                catch { }
            }
        }
    }
}
