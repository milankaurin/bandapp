using System.Collections.Concurrent;
using Band.Shared.Domain;
using Band.Shared.Dto;
using BandApplicationBack.Domain;
using BandApplicationBack.State;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;

namespace BandApplicationBack.Infrastructure.Hubs
{
    public class BandAppHub : Hub
    {
        private static readonly ConcurrentDictionary<string, string> _connections = new(
            StringComparer.OrdinalIgnoreCase
        );

        //public async Task JoinSession(string code)
        //{
        //    await Groups.AddToGroupAsync(Context.ConnectionId, code);

        //    var state = SessionStateStore.Get(code);
        //    if (state == null)
        //        return;

        //    state.ConnectedClients++;
        //    state.LastTouched = DateTime.Now;
        //}
        public async Task JoinSession(string code)
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, code);

            _connections[Context.ConnectionId] = code;

            var state = SessionStateStore.Get(code);
            if (state == null)
                return;

            lock (state)
            {
                state.ConnectedClients++;
                state.LastTouched = DateTime.Now;
            }

            var index = state.QueueList.FindIndex(x => x.IsActive);

            var dto = new NextSongResponseDto
            {
                CurrentSong = index >= 0 ? state.QueueList[index] : null,
                PreviousSong = index > 0 ? state.QueueList[index - 1] : null,
                QueueList = state.QueueList,
                IdNaredne =
                    (index >= 0 && index + 1 < state.QueueList.Count)
                        ? state.QueueList[index + 1].Id
                        : 0,
            };

            await Clients.Caller.SendAsync(
                SignalType.StateChanged,
                new SongHubMessage { Type = MessageTypes.QueueUpdated, Payload = dto }
            );
        }

        public override async Task OnDisconnectedAsync(Exception? exception)
        {
            if (_connections.TryRemove(Context.ConnectionId, out var code))
            {
                var state = SessionStateStore.Get(code);
                if (state != null)
                {
                    bool shouldClose = false;

                    lock (state)
                    {
                        state.ConnectedClients--;

                        if (state.ConnectedClients <= 0)
                            shouldClose = true;
                    }

                    if (shouldClose)
                    {
                        SessionStateStore.Remove(code);

                        await Clients
                            .Group(code)
                            .SendAsync(
                                SignalType.StateChanged,
                                new SongHubMessage { Type = MessageTypes.SessionStopped }
                            );
                    }
                }
            }

            await base.OnDisconnectedAsync(exception);
        }

        public async Task LeaveSession(string code)
        {
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, code);

            if (_connections.TryRemove(Context.ConnectionId, out _))
            {
                var state = SessionStateStore.Get(code);
                if (state == null)
                    return;

                bool shouldClose = false;

                lock (state)
                {
                    state.ConnectedClients--;

                    if (state.ConnectedClients <= 0)
                        shouldClose = true;
                }

                if (shouldClose)
                {
                    SessionStateStore.Remove(code);

                    await Clients
                        .Group(code)
                        .SendAsync(
                            SignalType.StateChanged,
                            new SongHubMessage { Type = MessageTypes.SessionStopped }
                        );
                }
            }
        }
    }
}
