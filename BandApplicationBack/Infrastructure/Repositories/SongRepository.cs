using System;
using Band.Shared.Domain;
using BandApplicationBack.Domain;
using Microsoft.EntityFrameworkCore;

namespace BandApplicationBack.Infrastructure.Repositories
{
    public class SongRepository
    {
        private readonly BandAppDbContext _context;

        public SongRepository(BandAppDbContext context)
        {
            _context = context;
        }

        public async Task<List<Song>> GetAllAsync()
        {
            var songs = await _context.Songs.Include(s => s.Izvodjac).ToListAsync();

            // Popuni ArtistName za svaku pesmu
            foreach (var song in songs)
            {
                song.ArtistName = song.Izvodjac != null ? song.Izvodjac.Name : "Nepoznato";
            }

            return songs;
        }

        public async Task<Song?> GetByIdAsync(int id)
        {
            return await _context
                .Songs.Include(s => s.Izvodjac)
                .FirstOrDefaultAsync(s => s.Id == id);
        }

        public async Task<Song> AddAsync(Song song)
        {
            song.Izvodjac = _context.Artists.Find(song.ArtistId);
            _context.Songs.Add(song);
            await _context.SaveChangesAsync();
            return song;
        }

        public async Task UpdateAsync(Song song)
        {
            _context.Songs.Update(song);
            await _context.SaveChangesAsync();
        }

        public async Task DeleteAsync(int id)
        {
            var song = await _context.Songs.FindAsync(id);
            if (song != null)
            {
                _context.Songs.Remove(song);
                await _context.SaveChangesAsync();
            }
        }
    }
}
