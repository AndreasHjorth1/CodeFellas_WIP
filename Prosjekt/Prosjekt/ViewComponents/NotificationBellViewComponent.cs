using Microsoft.AspNetCore.Mvc;
using Prosjekt.DataAccess.Repositories;
using System.Security.Claims;

namespace Prosjekt.ViewComponents
{
    /// <summary>
    /// Bjella i navbaren med antall uleste varsler.
    /// </summary>
    public class NotificationBellViewComponent : ViewComponent
    {
        private readonly INotificationRepository _repo;

        public NotificationBellViewComponent(INotificationRepository repo)
        {
            _repo = repo;
        }

        public async Task<IViewComponentResult> InvokeAsync()
        {
            var userId = UserClaimsPrincipal.FindFirstValue(ClaimTypes.NameIdentifier);
            var unread = userId == null ? 0 : await _repo.CountUnreadAsync(userId);

            return View(unread);
        }
    }
}