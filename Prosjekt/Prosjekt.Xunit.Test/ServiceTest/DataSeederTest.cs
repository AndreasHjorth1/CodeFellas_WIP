using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Prosjekt.DataAccess;
using Prosjekt.Models.Entities;
using Prosjekt.Models.Enums;
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

        [Fact]
        public async Task SeedAsync_EmptyDatabase_SevenOpenThreeAssignedTwoResolved()
        {
            using var context = CreateContext();

            await DataSeeder.SeedAsync(context, "user", "coordinator", "admin");

            Assert.Equal(7, context.Needs.Count(n => n.Status == EntryStatus.New || n.Status == EntryStatus.UnderReview));
            Assert.Equal(3, context.Needs.Count(n => n.Status == EntryStatus.Assigned));
            Assert.Equal(2, context.Needs.Count(n => n.Status == EntryStatus.Resolved));
        }

        [Fact]
        public async Task SeedAsync_EmptyDatabase_OnlyAssignedNeedsHaveAllocations()
        {
            using var context = CreateContext();

            await DataSeeder.SeedAsync(context, "user", "coordinator", "admin");

            var needsWithAllocations = context.Allocations.Select(a => a.NeedId).Distinct().ToList();
            var assignedNeeds = context.Needs.Where(n => n.Status == EntryStatus.Assigned).Select(n => n.Id).ToList();
            Assert.Equal(assignedNeeds.OrderBy(id => id), needsWithAllocations.OrderBy(id => id));
        }

        [Fact]
        public async Task SeedAsync_EmptyDatabase_AllocationsFitWithinResourceQuantity()
        {
            using var context = CreateContext();

            await DataSeeder.SeedAsync(context, "user", "coordinator", "admin");

            foreach (var resource in context.Resources.ToList())
            {
                var allocated = context.Allocations.Where(a => a.ResourceId == resource.Id).Sum(a => a.Quantity);
                Assert.True(allocated <= resource.Quantity, $"{resource.Name} er tildelt mer enn den har");
            }
        }

        [Fact]
        public async Task SeedAsync_EmptyDatabase_ThreeUnreadAndFourReadNotifications()
        {
            using var context = CreateContext();

            await DataSeeder.SeedAsync(context, "user", "coordinator", "admin");

            Assert.Equal(3, context.Notifications.Count(n => !n.IsRead));
            Assert.Equal(4, context.Notifications.Count(n => n.IsRead));
        }

        [Fact]
        public async Task SeedAsync_EmptyDatabase_DatesMakeSense()
        {
            using var context = CreateContext();
            var now = DateTime.UtcNow;

            await DataSeeder.SeedAsync(context, "user", "coordinator", "admin");

            Assert.All(context.Resources.ToList(), r =>
            {
                Assert.True(r.CreatedAt < now, $"{r.Name} er registrert i fremtiden");
                Assert.True(r.AvailableFrom < r.AvailableTo, $"{r.Name} slutter før den starter");
            });
            Assert.All(context.Needs.ToList(), n =>
            {
                Assert.True(n.CreatedAt < now, $"{n.Name} er meldt inn i fremtiden");
                Assert.True(n.NeededBy > now, $"{n.Name} har allerede gått ut");
            });
        }

        [Fact]
        public async Task SeedAsync_EmptyDatabase_AllTimesAreWholeHours()
        {
            using var context = CreateContext();

            await DataSeeder.SeedAsync(context, "user", "coordinator", "admin");

            var times = context.Resources.ToList().SelectMany(r => new[] { r.CreatedAt, r.AvailableTo })
                .Concat(context.Needs.ToList().SelectMany(n => new[] { n.CreatedAt, n.NeededBy }));
            Assert.All(times, t => Assert.True(t.Minute == 0 && t.Second == 0, $"{t} er ikke et helt klokkeslett"));
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
                new ApplicationUser { Id = "user", UserName = "user" },
                new ApplicationUser { Id = "coordinator", UserName = "coordinator" },
                new ApplicationUser { Id = "admin", UserName = "admin" });
            context.SaveChanges();

            return context;
        }
    }
}