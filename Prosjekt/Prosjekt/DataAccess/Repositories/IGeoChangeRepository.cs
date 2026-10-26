using Prosjekt.Models.Entities;

namespace Prosjekt.DataAccess.Repositories
{
    public interface IGeoChangeRepository
    {
        Task AddAsync(GeoChange geoChange);
        Task<IEnumerable<GeoChange>> GetAllAsync();
        Task<IEnumerable<GeoChange>> GetAllByUserIdAsync(string userId);
        Task<GeoChange?> GetByIdAndUserIdAsync(int id, string userId);
        Task DeleteAsync(int id, string userId);
    }
}