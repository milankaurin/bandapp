using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Band.Shared.Domain;

namespace Band.Shared.Dto
{
    public class NextSongResponseDto
    {
        public List<Song> QueueList { get; set; } = new List<Song>();
        public Song? CurrentSong { get; set; }

        public Song? PreviousSong { get; set; }

        public int IdNaredne { get; set; } = 0;
    }
}
