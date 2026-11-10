using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Prosjekt.DataAccess;
using Prosjekt.Models.Entities;
using Prosjekt.Services;
using Xunit;
using Microsoft.AspNetCore.Identity;

namespace Prosjekt.Xunit
{
    public class DataSeederTest
    {
        [Fact]
        public async Task SeedAsync_EmptyDatabase_Adds20ResourcesAnd12Needs()
        {
            using var context = CreateContext();

            await DataSeeder.SeedAsync(context, "user", "coordinator", "admin");

            Assert.Equal(20, context.Resources.Count());
            Assert.Equal(12, context.Needs.Count());
        }

        [Fact]
        public async Task SeedAsync_RunTwice_DoesNotAddDuplicates()
        {
            using var context = CreateContext();

            await DataSeeder.SeedAsync(context, "user", "coordinator", "admin");
            await DataSeeder.SeedAsync(context, "user", "coordinator", "admin");

            Assert.Equal(20, context.Resources.Count());
            Assert.Equal(12, context.Needs.Count());
        }

        [Fact]
        public async Task SeedAsync_ExistingResource_AddsNothing()
        {
            using var context = CreateContext();
            context.Resources.Add(new Resource { Name = "Egen ressurs", Coords = new Coordinates(58.1, 8.0) });
            await context.SaveChangesAsync();

            await DataSeeder.SeedAsync(context, "user", "coordinator", "admin");

            Assert.Single(context.Resources);
            Assert.Empty(context.Needs);
        }

        [Fact]
        public async Task SeedAsync_SetsOwners_UserAndCoordinatorOwnResources_CoordinatorAndAdminOwnNeeds()
        {
            using var context = CreateContext();

            await DataSeeder.SeedAsync(context, "user", "coordinator", "admin");

            Assert.Equal(14, context.Resources.Count(r => r.UserId == "user"));
            Assert.Equal(6, context.Resources.Count(r => r.UserId == "coordinator"));
            Assert.Equal(6, context.Needs.Count(n => n.UserId == "coordinator"));
            Assert.Equal(6, context.Needs.Count(n => n.UserId == "admin"));
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

            // UserId er en fremmednøkkel, så brukerne må finnes i databasen
            context.Users.AddRange(
                new IdentityUser { Id = "user", UserName = "user" },
                new IdentityUser { Id = "coordinator", UserName = "coordinator" },
                new IdentityUser { Id = "admin", UserName = "admin" });
            context.SaveChanges();

            return context;
        }
    }
}