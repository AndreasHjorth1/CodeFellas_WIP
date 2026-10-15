using Prosjekt.DataAccess.Repositories;
using System;
using System.Collections.Generic;
using Prosjekt.Models.Entities;

namespace Prosjekt.Xunit.Test.Fakes
{ 
    public class FakeNeedRepository : INeedRepository
    {
        public List<Need> Needs { get; } = new();

        public Task AddAsync(Need need)
        {
            need.Id = Needs.Count + 1;
            Needs.Add(need);

            return Task.CompletedTask;
        }

        public Task DeleteAsync(int id)
        {
            Needs.RemoveAll(n => n.Id == id);

            return Task.CompletedTask;
        }

        public Task<IEnumerable<Need>> GetAllAsync()
        {
            return Task.FromResult<IEnumerable<Need>>(Needs);
        }

        public Task<Need?> GetByIdAsync(int id)
        {
            var need = Needs.FirstOrDefault(n => n.Id == id);

            return Task.FromResult(need);
        }

        public Task UpdateAsync(Need need)
        {
            return Task.CompletedTask;
        }
    }
}
