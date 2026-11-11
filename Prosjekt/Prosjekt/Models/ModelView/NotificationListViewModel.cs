using Prosjekt.Models.Entities;

namespace Prosjekt.Models.ModelView
{
    /// <summary>
    /// Varslene til brukeren, og hvilke av dem som var uleste da siden ble åpnet.
    /// </summary>
    public class NotificationListViewModel
    {
        public IEnumerable<Notification> Notifications { get; init; } = [];

        public HashSet<int> UnreadIds { get; init; } = [];

        public bool IsNew(Notification notification) => UnreadIds.Contains(notification.Id);
    }
}