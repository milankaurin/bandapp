using Band.Shared.Domain;

namespace BandApplicationBack.State
{
    public class SessionState
    {
        public bool IsSessionActive { get; set; } = false;
        public List<Song> QueueList { get; set; } = new();
        public DateTime LastTouched { get; set; } = DateTime.Now;

        public int ConnectedClients { get; set; } = 0;
    }
}
