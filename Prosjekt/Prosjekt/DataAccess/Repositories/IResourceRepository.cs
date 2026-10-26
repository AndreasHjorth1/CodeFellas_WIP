using Prosjekt.Models.Entities;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Prosjekt.DataAccess.Repositories
{
    public interface IResourceRepository
    {
        Task<IEnumerable<Resource>> GetAllAsync();
        Task<IEnumerable<Resource>> GetAllByUserIdAsync(string userId);
        Task<Resource?> GetByIdAsync(int id);
        Task<Resource?> GetByIdAndUserIdAsync(int id, string userId);
        Task AddAsync(Resource resource);
        Task UpdateAsync(Resource resource);
        Task DeleteAsync(int id);
        Task DeleteAsync(int id, string userId);
    }
}