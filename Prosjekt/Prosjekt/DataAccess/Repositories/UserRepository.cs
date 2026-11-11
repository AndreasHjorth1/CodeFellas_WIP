using Microsoft.EntityFrameworkCore;

namespace Prosjekt.DataAccess.Repositories
{
    public class UserRepository : IUserRepository
    {
        private readonly ApplicationDbContext _context;

        public UserRepository(ApplicationDbContext context)
        {
            _context = context;
        }

        /// <summary>
        /// Henter alle navnene i én spørring, i stedet for én per bruker.
        /// </summary>
        public async Task<IReadOnlyDictionary<string, string>> GetFullNamesAsync(IEnumerable<string?> userIds)
        {
            var ids = userIds
                .Where(id => !string.IsNullOrEmpty(id))
                .Select(id => id!)
                .Distinct()
                .ToList();

            if (ids.Count == 0)
            {
                return new Dictionary<string, string>();
            }

            return await _context.Users
                .AsNoTracking()
                .Where(user => ids.Contains(user.Id))
                .ToDictionaryAsync(user => user.Id, user => user.FullName);
        }
    }
}
