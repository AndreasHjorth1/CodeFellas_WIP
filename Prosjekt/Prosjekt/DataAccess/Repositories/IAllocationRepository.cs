using Microsoft.EntityFrameworkCore;
using Prosjekt.Models.Entities;

namespace Prosjekt.DataAccess.Repositories
{
    public interface IAllocationRepository
    {
        Task<IEnumerable<Allocation>> GetAllAsync();
        Task<IEnumerable<Allocation>> GetByNeedAsync(int needId);
        Task AddAsync(Allocation allocation);
        Task DeleteAsync(int id);
        Task DeleteByNeedAsync(int needId);
        Task DeleteByResourceAsync(int resourceId);
    }
}