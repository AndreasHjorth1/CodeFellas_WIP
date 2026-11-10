using Prosjekt.DataAccess.Repositories;
using Prosjekt.Models.Entities;

namespace Prosjekt.Xunit.Test.Fakes
{
    public class FakeNotificationRepository : INotificationRepository
    {
        /// <summary>
        /// Varslene som er lagret, så testene kan sjekke dem.
        /// </summary>
        public List<Notification> Notifications { get; } = new();

        public Task<IEnumerable<Notification>> GetByUserAsync(string userId)
            => Task.FromResult<IEnumerable<Notification>>(
                Notifications.Where(n => n.UserId == userId).OrderByDescending(n => n.CreatedAt).ToList());

        public Task<int> CountUnreadAsync(string userId)
            => Task.FromResult(Notifications.Count(n => n.UserId == userId && !n.IsRead));

        public Task AddAsync(Notification notification)
        {
            Notifications.Add(notification);
            return Task.CompletedTask;
        }

        public Task MarkAllReadAsync(string userId)
        {
            foreach (var n in Notifications.Where(n => n.UserId == userId))
            {
                n.MarkAsRead();
            }
            return Task.CompletedTask;
        }
    }
}