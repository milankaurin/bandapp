using Band.Shared.Domain;

namespace BandApplicationBack.Domain
{
    public static class State
    {
        public static bool IsSessionActive = false;

        public static List<Song> QueueList = new List<Song>();
    }
}
