using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Diagnostics.Contracts;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace Band.Shared.Domain;

public partial class Song
{
    [NotMapped]
    public string? SongNameArtistDisplay => $"{this.Name} - {this.ArtistName}";

    [NotMapped]
    public Guid SongInListUniqueId { get; set; }

    [NotMapped]
    public bool IsActive { get; set; } = false;
}
