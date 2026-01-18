using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Band.Shared.Dto
{
    public class SongHubMessage
    {
        public string Type { get; set; } = ""; // npr. "SessionStarted", "QueueUpdated", "NextSong"
        public NextSongResponseDto? Payload { get; set; }
        public bool? IsSessionActive { get; set; }
    }

    public static class MessageTypes
    {
        public const string NextSong = "NextSong";
        public const string SessionStarted = "SessionStarted";
        public const string QueueUpdated = "QueueUpdated";
        public const string SessionStopped = "SessionStopped";
    }

    public static class SignalType
    {
        public const string StateChanged = "StateChanged";
    }
}
