using Prosjekt.DataAccess.Repositories;
using System;
using System.Collections.Generic;
using Prosjekt.Models.Entities;

namespace Prosjekt.Xunit.Test.Fakes
{
    public class FakeResourceRepository : IResourceRepository
    {
        public List<Resource> Resources { get; } = new();

        public Task AddAsync(Resource resource)
        {
            resource.Id = Resources.Count + 1;
            Resources.Add(resource);

            return Task.CompletedTask;
        }

        public Task DeleteAsync(int id)
        {
            Resources.RemoveAll(n => n.Id == id);

            return Task.CompletedTask;
        }

        public Task<IEnumerable<Resource>> GetAllAsync()
        {
            return Task.FromResult<IEnumerable<Resource>>(Resources);
        }

        public Task<Resource?> GetByIdAsync(int id)
        {
            var resource = Resources.FirstOrDefault(n => n.Id == id);

            return Task.FromResult(resource);
        }

        public Task UpdateAsync(Resource resource)
        {
            return Task.CompletedTask;
        }
    }
}
