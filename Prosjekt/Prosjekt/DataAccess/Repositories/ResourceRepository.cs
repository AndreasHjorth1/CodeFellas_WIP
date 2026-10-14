using Microsoft.EntityFrameworkCore;
using Prosjekt.Models.Entities;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Prosjekt.DataAccess.Repositories
{
    public class ResourceRepository : IResourceRepository
    {
        private readonly ApplicationDbContext _context;

        public ResourceRepository(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IEnumerable<Resource>> GetAllAsync()
        {
            return await _context.Resources.ToListAsync();
        }

        public async Task<Resource?> GetByIdAsync(int id)
        {
            return await _context.Resources.FindAsync(id);
        }

        public async Task AddAsync(Resource resource)
        {
            await _context.Resources.AddAsync(resource);
            await _context.SaveChangesAsync();
        }

        public async Task UpdateAsync(Resource resource)
        {
            _context.Resources.Update(resource);
            await _context.SaveChangesAsync();
        }

        public async Task DeleteAsync(int id)
        {
            var resource = await _context.Resources.FindAsync(id);
            if (resource != null)
            {
                _context.Resources.Remove(resource);
                await _context.SaveChangesAsync();
            }
        }
    }
}