using Prosjekt.Models.Entities;

namespace Prosjekt.DataAccess.Repositories
{
    /// <summary>
    /// Varsler til brukere.
    /// </summary>
    public interface INotificationRepository
    {
        /// <summary>
        /// Henter varslene til brukeren, nyeste først.
        /// </summary>
        Task<IEnumerable<Notification>> GetByUserAsync(string userId);

        /// <summary>
        /// Teller hvor mange uleste varsler brukeren har.
        /// </summary>
        Task<int> CountUnreadAsync(string userId);

        /// <summary>
        /// Lagrer et nytt varsel.
        /// </summary>
        Task AddAsync(Notification notification);

        /// <summary>
        /// Markerer alle varslene til brukeren som lest.
        /// </summary>
        Task MarkAllReadAsync(string userId);
    }
}