using Microsoft.EntityFrameworkCore;
using Prosjekt.Models.Entities;

namespace Prosjekt.DataAccess.Repositories
{
    public class AllocationRepository : IAllocationRepository
    {
        private readonly ApplicationDbContext _context;

        public AllocationRepository(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IEnumerable<Allocation>> GetAllAsync()
        {
            return await _context.Allocations.ToListAsync();
        }

        public async Task<IEnumerable<Allocation>> GetByNeedAsync(int needId)
        {
            return await _context.Allocations
                .Where(a => a.NeedId == needId)
                .ToListAsync();
        }

        public async Task AddAsync(Allocation allocation)
        {
            _context.Allocations.Add(allocation);
            await _context.SaveChangesAsync();
        }

        public async Task DeleteAsync(int id)
        {
            var allocation = await _context.Allocations.FindAsync(id);
            if (allocation != null)
            {
                _context.Allocations.Remove(allocation);
                await _context.SaveChangesAsync();
            }
        }
    }
}