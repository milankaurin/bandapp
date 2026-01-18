using Band.Shared.Domain;
using BandApplicationBack.Domain;
using Microsoft.EntityFrameworkCore;

namespace BandApplicationBack.Infrastructure.Repositories
{
   

    public class ArtistRepository
    {
        //create everything that is needed for the repository
        private readonly BandAppDbContext _context;

        public ArtistRepository(BandAppDbContext context)
        {
            _context = context;
        }

        public async Task<List<Artist>> GetAllAsync()
        {
            return await _context.Artists.Include(b => b.Songs).ToListAsync();
        }

        public async Task<Artist?> GetByIdAsync(int id)
        {
            return await _context.Artists.Include(b => b.Songs)
                                       .FirstOrDefaultAsync(b => b.Id == id);
        }

        public async Task<Artist> AddAsync(Artist band)
        {
            _context.Artists.Add(band);
            await _context.SaveChangesAsync();
            return band;
        }

        public async Task UpdateAsync(Artist band)
        {
            _context.Artists.Update(band);
            await _context.SaveChangesAsync();
        }

        public async Task DeleteAsync(int id)
        {
            var band = await _context.Artists.FindAsync(id);
            if (band != null)
            {
                _context.Artists.Remove(band);
                await _context.SaveChangesAsync();
            }
        }

        

     



    }
}
