using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Prosjekt.DataAccess.Repositories;
using Prosjekt.Models.ModelView;
using System.Security.Claims;

namespace Prosjekt.Controllers
{
    /// <summary>
    /// Viser varslene til innlogget bruker.
    /// </summary>
    [Authorize]
    public class NotificationController : Controller
    {
        private readonly INotificationRepository _repo;

        public NotificationController(INotificationRepository repo)
        {
            _repo = repo;
        }

        /// <summary>
        /// Viser alle varslene og markerer dem som lest.
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
            var notifications = (await _repo.GetByUserAsync(userId)).ToList();

            // Husker hvilke som var uleste før de markeres, ellers vises alle som lest på siden
            var unreadIds = notifications.Where(n => !n.IsRead).Select(n => n.Id).ToHashSet();

            if (unreadIds.Count > 0)
            {
                await _repo.MarkAllReadAsync(userId);
            }

            return View(new NotificationListViewModel
            {
                Notifications = notifications,
                UnreadIds = unreadIds
            });
        }
    }
}