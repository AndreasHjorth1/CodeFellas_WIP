//using Prosjekt.DataAccess.Repositories;
//using Prosjekt.Models.Entities;

//namespace Prosjekt.Xunit.Test.Fakes
//{
//    public class FakeGeoChangeRepository : IGeoChangeRepository
//    {
//        public List<GeoChange> GeoChanges { get; } = new();

//        public Task AddAsync(GeoChange geoChange)
//        {
//            geoChange.Id = GeoChanges.Count + 1;
//            GeoChanges.Add(geoChange);

//            return Task.CompletedTask;
//        }

//        public Task<IEnumerable<GeoChange>> GetAllAsync()
//        {
//            return Task.FromResult<IEnumerable<GeoChange>>(GeoChanges);
//        }
//    }
//}