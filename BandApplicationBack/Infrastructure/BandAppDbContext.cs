using Band.Shared.Domain;
using BandApplicationBack.Domain;
using Microsoft.EntityFrameworkCore;

namespace BandApplicationBack.Infrastructure
{
    public class BandAppDbContext : DbContext
    {
        public DbSet<Song> Songs { get; set; }
        public DbSet<Artist> Artists { get; set; }
        public DbSet<User> Users { get; set; }

        public DbSet<BandSession> Sessions { get; set; }

        public BandAppDbContext(DbContextOptions<BandAppDbContext> options)
            : base(options) { }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Define relationships and constraints if necessary
            modelBuilder
                .Entity<Song>()
                .HasOne(s => s.Izvodjac)
                .WithMany(a => a.Songs)
                .HasForeignKey(s => s.ArtistId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}
