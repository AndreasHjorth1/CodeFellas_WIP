using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Prosjekt.DataAccess;
using Prosjekt.DataAccess.Repositories;
using Prosjekt.Models.Entities;
using Xunit;

namespace Prosjekt.Xunit
{
    public class NotificationRepositoryTest
    {
        [Fact]
        public async Task AddAsync_SavesNotification()
        {
            using var context = CreateContext();
            var repository = new NotificationRepository(context);

            await repository.AddAsync(new Notification { UserId = "user-1", Message = "Hei" });

            var saved = Assert.Single(context.Notifications);
            Assert.Equal("Hei", saved.Message);
            Assert.False(saved.IsRead);
        }

        [Fact]
        public async Task GetByUserAsync_ReturnsOnlyThatUserNewestFirst()
        {
            using var context = CreateContext();
            context.Notifications.Add(new Notification { UserId = "user-1", Message = "Eldst", CreatedAt = new DateTime(2026, 11, 1) });
            context.Notifications.Add(new Notification { UserId = "user-1", Message = "Nyest", CreatedAt = new DateTime(2026, 11, 10) });
            context.Notifications.Add(new Notification { UserId = "user-2", Message = "Noen andres" });
            context.SaveChanges();
            var repository = new NotificationRepository(context);

            var result = (await repository.GetByUserAsync("user-1")).ToList();

            Assert.Equal(2, result.Count);
            Assert.Equal("Nyest", result[0].Message);
            Assert.Equal("Eldst", result[1].Message);
        }

        [Fact]
        public async Task CountUnreadAsync_CountsOnlyUnreadForThatUser()
        {
            using var context = CreateContext();
            var read = new Notification { UserId = "user-1", Message = "Lest" };
            read.MarkAsRead();
            context.Notifications.Add(read);
            context.Notifications.Add(new Notification { UserId = "user-1", Message = "Ulest" });
            context.Notifications.Add(new Notification { UserId = "user-2", Message = "Noen andres" });
            context.SaveChanges();
            var repository = new NotificationRepository(context);

            var count = await repository.CountUnreadAsync("user-1");

            Assert.Equal(1, count);
        }

        [Fact]
        public async Task MarkAllReadAsync_OnlyMarksThatUsersNotifications()
        {
            using var context = CreateContext();
            context.Notifications.Add(new Notification { UserId = "user-1", Message = "Min" });
            context.Notifications.Add(new Notification { UserId = "user-1", Message = "Min også" });
            context.Notifications.Add(new Notification { UserId = "user-2", Message = "Noen andres" });
            context.SaveChanges();
            var repository = new NotificationRepository(context);

            await repository.MarkAllReadAsync("user-1");

            Assert.All(context.Notifications.Where(n => n.UserId == "user-1"), n => Assert.True(n.IsRead));
            Assert.False(context.Notifications.Single(n => n.UserId == "user-2").IsRead);
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
    }
}
