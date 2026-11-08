using Microsoft.EntityFrameworkCore;
using Prosjekt.Models.Entities;
using System.Collections.Generic;
using System.Threading.Tasks;
using Prosjekt.Services;

namespace Prosjekt.DataAccess.Repositories
{
    public class NeedRepository : INeedRepository
    {
        private readonly ApplicationDbContext _context;
        private readonly AuditLogService _auditLogService;

        public NeedRepository(
            ApplicationDbContext context,
            AuditLogService auditLogService)
        {
            _context = context;
            _auditLogService = auditLogService;
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
            var original = await _context.Needs
                .AsNoTracking()
                .FirstOrDefaultAsync(n => n.Id == need.Id);

            if (original == null)
            {
                throw new InvalidOperationException("Behovet finnes ikke.");
            }

            if (original.Status != need.Status)
            {
                _auditLogService.Add(
                    action: "Update",
                    entityType: "Need",
                    entityId: need.Id,
                    fieldName: "Status",
                    oldValue: original.Status.ToString(),
                    newValue: need.Status.ToString());
            }

            _context.Needs.Update(need);
            await _context.SaveChangesAsync();
        }

        public async Task DeleteAsync(int id)
        {
            var need = await _context.Needs.FindAsync(id);

            if (need != null)
            {
                _auditLogService.Add(
                    action: "Delete",
                    entityType: "Need",
                    entityId: need.Id);

                _context.Needs.Remove(need);
                await _context.SaveChangesAsync();
            }
        }
    }
}