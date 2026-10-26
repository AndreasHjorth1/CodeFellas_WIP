using Prosjekt.Models.Entities;

namespace Prosjekt.DataAccess.Repositories
{
    public interface INeedRepository
    {
        Task<IEnumerable<Need>> GetAllAsync();
        Task<IEnumerable<Need>> GetAllByUserIdAsync(string userId);
        Task<Need?> GetByIdAsync(int id);
        Task<Need?> GetByIdAndUserIdAsync(int id, string userId);
        Task AddAsync(Need need);
        Task UpdateAsync(Need need);
        Task DeleteAsync(int id, string userId);
    }
}