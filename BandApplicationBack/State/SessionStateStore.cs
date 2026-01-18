using System.Collections.Concurrent;
using Band.Shared.Domain;

namespace BandApplicationBack.State
{
    public static class SessionStateStore
    {
        private static readonly ConcurrentDictionary<string, SessionState> _sessions = new(
            StringComparer.OrdinalIgnoreCase
        );

        public static void Create(string code)
        {
            _sessions.TryAdd(code, new SessionState());
        }

        public static SessionState? Get(string code)
        {
            _sessions.TryGetValue(code, out var state);
            return state;
        }

        public static void Update(string code, SessionState newState)
        {
            _sessions.AddOrUpdate(code, newState, (_, __) => newState);
        }

        public static bool Exists(string code) => _sessions.ContainsKey(code);

        public static bool Remove(string code) => _sessions.TryRemove(code, out _);
    }

    public class SessionState
    {
        public bool IsSessionActive { get; set; } = false;
        public List<Song> QueueList { get; set; } = new();
        public DateTime LastTouched { get; set; } = DateTime.Now;

        public int ConnectedClients { get; set; } = 0;
    }
}
