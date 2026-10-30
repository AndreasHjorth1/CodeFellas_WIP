using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Prosjekt.DataAccess;
using Prosjekt.DataAccess.Repositories;
using Prosjekt.Models.Entities;
using Xunit;

namespace Prosjekt.Xunit
{
    public class ResourceRepositoryTest
    {
        [Fact]
        public async Task GetAllAsync_ReturnsAllResources()
        {
            using var context = CreateContext();
            AddResource(context, "Lastebil");
            AddResource(context, "Aggregat");
            var repository = new ResourceRepository(context);

            var result = await repository.GetAllAsync();

            Assert.Equal(2, result.Count());
            Assert.Contains(result, r => r.Name == "Lastebil");
            Assert.Contains(result, r => r.Name == "Aggregat");
        }

        [Fact]
        public async Task GetAllAsync_WhenEmpty_ReturnsEmptyList()
        {
            using var context = CreateContext();
            var repository = new ResourceRepository(context);

            var result = await repository.GetAllAsync();

            Assert.Empty(result);
        }

        [Fact]
        public async Task GetByIdAsync_WhenResourceExists_ReturnsResource()
        {
            using var context = CreateContext();
            var resource = AddResource(context);
            var repository = new ResourceRepository(context);

            var result = await repository.GetByIdAsync(resource.Id);

            Assert.NotNull(result);
            Assert.Equal(resource.Id, result.Id);
            Assert.Equal(resource.Name, result.Name);
        }

        [Fact]
        public async Task GetByIdAsync_WhenResourceDoesNotExist_ReturnsNull()
        {
            using var context = CreateContext();
            var repository = new ResourceRepository(context);

            var result = await repository.GetByIdAsync(404);

            Assert.Null(result);
        }

        [Fact]
        public async Task AddAsync_SavesResource()
        {
            using var context = CreateContext();
            var repository = new ResourceRepository(context);
            var resource = CreateResource("Båt");

            await repository.AddAsync(resource);

            var saved = Assert.Single(context.Resources);
            Assert.NotEqual(0, saved.Id);
            Assert.Equal("Båt", saved.Name);
            Assert.Equal(3, saved.Quantity);
            Assert.Equal(58.1467, saved.Coords.Latitude);
        }

        [Fact]
        public async Task UpdateAsync_SavesChanges()
        {
            using var context = CreateContext();
            var resource = AddResource(context);
            var repository = new ResourceRepository(context);

            resource.Quantity = 10;
            resource.IsAvailable = false;
            await repository.UpdateAsync(resource);

            var updated = context.Resources.Find(resource.Id);
            Assert.NotNull(updated);
            Assert.Equal(10, updated.Quantity);
            Assert.False(updated.IsAvailable);
        }

        [Fact]
        public async Task DeleteAsync_WhenResourceExists_RemovesResource()
        {
            using var context = CreateContext();
            var resource = AddResource(context);
            var repository = new ResourceRepository(context);

            await repository.DeleteAsync(resource.Id);

            Assert.Empty(context.Resources);
        }

        [Fact]
        public async Task DeleteAsync_WhenResourceDoesNotExist_DoesNothing()
        {
            using var context = CreateContext();
            AddResource(context);
            var repository = new ResourceRepository(context);

            await repository.DeleteAsync(404);

            Assert.Single(context.Resources);
        }

        private static ApplicationDbContext CreateContext()
        {
            var connection = new SqliteConnection("DataSource=:memory:");
            connection.Open();

            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseSqlite(connection)
                .Options;

            var context = new ApplicationDbContext(options);
            context.Database.EnsureCreated();
            return context;
        }

        private static Resource CreateResource(string name = "Lastebil")
        {
            return new Resource
            {
                Name = name,
                Description = "Lastebil med henger",
                Category = "Vehicle",
                PhoneNumber = "12345678",
                Quantity = 3,
                IsAvailable = true,
                Coords = new Coordinates(58.1467, 7.9956)
            };
        }

        private static Resource AddResource(ApplicationDbContext context, string name = "Lastebil")
        {
            var resource = CreateResource(name);
            context.Resources.Add(resource);
            context.SaveChanges();
            return resource;
        }
    }
}