using Band.Shared.Domain;

namespace BandApplicationBlazor.Helper
{
    public static class LoadedSongs
    {
        public static List<Song> ALLSONGS { get; set; } = new List<Song>();

        public static List<Song> Queue { get; set; } = new List<Song>();

        public static Song? PreviousSong { get; set; }
        public static Song? Current { get; set; }
    }
}
