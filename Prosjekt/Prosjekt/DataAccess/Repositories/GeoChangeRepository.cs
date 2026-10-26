using Microsoft.EntityFrameworkCore;
using Prosjekt.Models.Entities;

namespace Prosjekt.DataAccess.Repositories
{
    public class GeoChangeRepository : IGeoChangeRepository
    {
        private readonly ApplicationDbContext _context;

        public GeoChangeRepository(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task AddAsync(GeoChange geoChange)
        {
            await _context.GeoChanges.AddAsync(geoChange);
            await _context.SaveChangesAsync();
        }

        public async Task<IEnumerable<GeoChange>> GetAllAsync()
        {
            return await _context.GeoChanges.ToListAsync();
        }

        public async Task<IEnumerable<GeoChange>> GetAllByUserIdAsync(string userId)
        {
            return await _context.GeoChanges
                .Where(g => g.UserId == userId)
                .ToListAsync();
        }

        public async Task<GeoChange?> GetByIdAndUserIdAsync(int id, string userId)
        {
            return await _context.GeoChanges
                .FirstOrDefaultAsync(g => g.Id == id && g.UserId == userId);
        }

        public async Task DeleteAsync(int id, string userId)
        {
            var entity = await GetByIdAndUserIdAsync(id, userId);
            if (entity != null)
            {
                _context.GeoChanges.Remove(entity);
                await _context.SaveChangesAsync();
            }
        }
    }
}