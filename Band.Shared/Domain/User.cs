namespace BandApplicationBack.Domain
{
    public class User
    {
        public int Id { get; set; }  // Primary key
        public string FullName { get; set; } = string.Empty;  // User's full name
    }
}
