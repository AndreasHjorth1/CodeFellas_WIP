using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Prosjekt.DataAccess;
using Prosjekt.Models.Authorization;

namespace Prosjekt.Controllers
{
    [Authorize(Roles = RoleNames.Admin)]
    public class AuditLogController : Controller
    {
        private readonly ApplicationDbContext _context;

        public AuditLogController(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            var logs = await _context.AuditLogs
                .AsNoTracking()
                .OrderByDescending(log => log.Timestamp)
                .ThenByDescending(log => log.Id)
                .Take(100)
                .ToListAsync();

            return View(logs);
        }
    }
}