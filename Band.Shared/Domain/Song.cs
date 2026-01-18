using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace Band.Shared.Domain;

public partial class Song
{
    public int Id { get; set; } // Primarni ključ
    public string Name { get; set; } = string.Empty; // Naziv pesme

    public string? Chords { get; set; } = string.Empty; // Akordi u formatu stringa

    [NotMapped]
    public List<Section> Sections { get; set; } = new();

    public int? ArtistId { get; set; } // Strani ključ ka Izvodjac

    //[NotMapped]
    //public string? ArtistName1 { get; set; }

    [NotMapped]
    public string? ArtistName { get; set; }

    [JsonIgnore]
    public Artist? Izvodjac { get; set; } // Navigaciona osobina
}

public class Section
{
    /// <summary>
    /// Tip sekcije, npr. "verse", "chorus" itd.
    /// </summary>
    public string Type { get; set; } = string.Empty;

    /// <summary>
    /// Lista linija unutar ove sekcije.
    /// </summary>
    public List<Line> Lines { get; set; } = new();
}

public class Line
{
    /// <summary>
    /// Tekst stihova ove linije.
    /// </summary>
    public string Text { get; set; } = string.Empty;

    /// <summary>
    /// Lista akorda pozicioniranih unutar teksta.
    /// </summary>
    public List<Chord> Chords { get; set; } = new();
}

public class Chord
{
    /// <summary>
    /// Tekst akorda, npr. "Am", "C".
    /// </summary>
    public string Text { get; set; } = string.Empty;

    /// <summary>
    /// Pozicija (indeks) u liniji gde počinje akord.
    /// </summary>
    public int Position { get; set; }
}
