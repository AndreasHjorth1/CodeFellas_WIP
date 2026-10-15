using Prosjekt.Models.Entities;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Prosjekt.DataAccess.Repositories
{
    public interface INeedRepository
    {
        Task<IEnumerable<Need>> GetAllAsync();
        Task<Need?> GetByIdAsync(int id);
        Task AddAsync(Need need);
        Task UpdateAsync(Need need);
        Task DeleteAsync(int id);
    }
}