using Prosjekt.DataAccess.Repositories;
using Prosjekt.Models.Entities;

namespace Prosjekt.Xunit.Test.Fakes
{
    public class FakeAllocationRepository : IAllocationRepository
    {
        private readonly List<Allocation> _allocations = new();
        private int _nextId = 1;

        public Task<IEnumerable<Allocation>> GetAllAsync()
            => Task.FromResult<IEnumerable<Allocation>>(_allocations);

        public Task<IEnumerable<Allocation>> GetByNeedAsync(int needId)
            => Task.FromResult<IEnumerable<Allocation>>(_allocations.Where(a => a.NeedId == needId).ToList());

        public Task AddAsync(Allocation allocation)
        {
            typeof(Allocation).GetProperty("Id")?.SetValue(allocation, _nextId++);
            _allocations.Add(allocation);
            return Task.CompletedTask;
        }

        public Task DeleteAsync(int id)
        {
            _allocations.RemoveAll(a => a.Id == id);
            return Task.CompletedTask;
        }
        public Task DeleteByNeedAsync(int needId)
        {
            _allocations.RemoveAll(a => a.NeedId == needId);
            return Task.CompletedTask;
        }

        public Task DeleteByResourceAsync(int resourceId)
        {
            _allocations.RemoveAll(a => a.ResourceId == resourceId);
            return Task.CompletedTask;
        }
    }
}