using System.Text.Json.Serialization;

namespace Band.Shared.Domain

{
    public class Artist
    {
        public int Id { get; set; }  // Primarni ključ
        public string Name { get; set; } = string.Empty;  // Naziv benda/izvođača

        [JsonIgnore]
        public List<Song> Songs { get; set; } = new();  // Lista pesama koje pripadaju izvođaču
    }
}
