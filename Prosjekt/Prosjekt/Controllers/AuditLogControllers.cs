using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Prosjekt.DataAccess;
using Prosjekt.Models.Authorization;

namespace Prosjekt.Controllers
{
    [Authorize(Roles = RoleNames.AdminOrCoordinator)]
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

            /// <summary>
            /// Loggen lagrer bare ID-er. Her slås navnet på brukerne og behovene opp,
            /// så siden kan vise hvem og hva. Slettede behov og brukere finnes ikke lenger og får ikke navn.
            /// </summary>
            var brukerIder = logs
                .Where(log => log.UserId != null)
                .Select(log => log.UserId!)
                .Distinct()
                .ToList();

            ViewBag.Brukere = await _context.Users
                .AsNoTracking()
                .Where(user => brukerIder.Contains(user.Id))
                .ToDictionaryAsync(user => user.Id, user => (Navn: user.FullName, Epost: user.Email ?? string.Empty));

            var behovIder = logs
                .Where(log => log.EntityType == "Need")
                .Select(log => log.EntityId)
                .Distinct()
                .ToList();

            ViewBag.Behov = await _context.Needs
                .AsNoTracking()
                .Where(need => behovIder.Contains(need.Id))
                .ToDictionaryAsync(need => need.Id, need => need.Name);

            return View(logs);
        }
    }
}