using Prosjekt.DataAccess.Repositories;

namespace Prosjekt.Xunit.Test.Fakes
{
    public class FakeUserRepository : IUserRepository
    {
        /// <summary>
        /// Navnene testene legger inn, med bruker-ID som nøkkel.
        /// </summary>
        public Dictionary<string, string> Names { get; } = new();

        public Task<IReadOnlyDictionary<string, string>> GetFullNamesAsync(IEnumerable<string?> userIds)
        {
            IReadOnlyDictionary<string, string> result = userIds
                .Where(id => id != null && Names.ContainsKey(id))
                .Distinct()
                .ToDictionary(id => id!, id => Names[id!]);
            return Task.FromResult(result);
        }
    }
}
