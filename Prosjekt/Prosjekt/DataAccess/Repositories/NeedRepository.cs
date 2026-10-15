using Microsoft.EntityFrameworkCore;
using Prosjekt.Models.Entities;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Prosjekt.DataAccess.Repositories
{
    public class NeedRepository : INeedRepository
    {
        private readonly ApplicationDbContext _context;

        public NeedRepository(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IEnumerable<Need>> GetAllAsync()
        {
            return await _context.Needs.ToListAsync();
        }

        public async Task<Need?> GetByIdAsync(int id)
        {
            return await _context.Needs.FindAsync(id);
        }

        public async Task AddAsync(Need need)
        {
            _context.Needs.Add(need);
            await _context.SaveChangesAsync();
        }

        public async Task UpdateAsync(Need need)
        {
            _context.Needs.Update(need);
            await _context.SaveChangesAsync();
        }

        public async Task DeleteAsync(int id)
        {
            var need = await _context.Needs.FindAsync(id);
            if (need != null)
            {
                _context.Needs.Remove(need);
                await _context.SaveChangesAsync();
            }
        }
    }
}