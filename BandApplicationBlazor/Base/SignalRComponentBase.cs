using Band.Shared.Dto;
using BandApplicationFront.Services;
using Microsoft.AspNetCore.Components;

public abstract class SignalRComponentBase : ComponentBase, IDisposable
{
    [Inject]
    protected SongService SongService { get; set; } = default!;

    protected override void OnInitialized()
    {
        // zajedničke pretplate
        SongService.OnNextSongSelected += HandleNextSongSelected;
        SongService.OnSessionStateChanged += HandleSessionStateChanged;
    }

    // overriduj na konkretnim stranicama, ako treba
    protected virtual void HandleNextSongSelected(NextSongResponseDto dto) { }

    protected virtual void HandleSessionStateChanged(bool active) { }

    public void Dispose()
    {
        // OBAVEZNO ukloni pretplate
        SongService.OnNextSongSelected -= HandleNextSongSelected;
        SongService.OnSessionStateChanged -= HandleSessionStateChanged;
    }
}
