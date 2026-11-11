using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Prosjekt.Controllers;
using Prosjekt.Models.Entities;
using Prosjekt.Models.ModelView;
using Prosjekt.Xunit.Test.Fakes;
using System.Security.Claims;

namespace Prosjekt.Xunit.Test.ControllerTest
{
    public class NotificationControllerTest
    {
        private static NotificationController CreateController(FakeNotificationRepository repo, string userId = "user-1")
        {
            var claims = new List<Claim> { new Claim(ClaimTypes.NameIdentifier, userId) };

            return new NotificationController(repo)
            {
                ControllerContext = new ControllerContext
                {
                    HttpContext = new DefaultHttpContext
                    {
                        User = new ClaimsPrincipal(new ClaimsIdentity(claims, "Test"))
                    }
                }
            };
        }

        [Fact]
        public async Task Index_ReturnsOnlyOwnNotifications()
        {
            // Arrange
            var repo = new FakeNotificationRepository();
            repo.Notifications.Add(new Notification { Id = 1, UserId = "user-1", Message = "Min" });
            repo.Notifications.Add(new Notification { Id = 2, UserId = "user-2", Message = "Ikke min" });
            var controller = CreateController(repo);

            // Act
            var result = await controller.Index();

            // Assert
            var view = Assert.IsType<ViewResult>(result);
            var model = Assert.IsType<NotificationListViewModel>(view.Model);
            var notification = Assert.Single(model.Notifications);
            Assert.Equal("Min", notification.Message);
        }

        [Fact]
        public async Task Index_MarksOwnNotificationsAsRead()
        {
            // Arrange
            var repo = new FakeNotificationRepository();
            repo.Notifications.Add(new Notification { Id = 1, UserId = "user-1", Message = "Min" });
            repo.Notifications.Add(new Notification { Id = 2, UserId = "user-2", Message = "Ikke min" });
            var controller = CreateController(repo);

            // Act
            await controller.Index();

            // Assert
            Assert.True(repo.Notifications.Single(n => n.Id == 1).IsRead);
            Assert.False(repo.Notifications.Single(n => n.Id == 2).IsRead);
        }

        [Fact]
        public async Task Index_UnreadNotifications_AreStillShownAsNew()
        {
            // Arrange
            var repo = new FakeNotificationRepository();
            var alreadyRead = new Notification { Id = 1, UserId = "user-1", Message = "Gammel" };
            alreadyRead.MarkAsRead();
            repo.Notifications.Add(alreadyRead);
            repo.Notifications.Add(new Notification { Id = 2, UserId = "user-1", Message = "Ny" });
            var controller = CreateController(repo);

            // Act
            var result = await controller.Index();

            // Assert
            var model = Assert.IsType<NotificationListViewModel>(Assert.IsType<ViewResult>(result).Model);
            Assert.False(model.IsNew(model.Notifications.Single(n => n.Id == 1)));
            Assert.True(model.IsNew(model.Notifications.Single(n => n.Id == 2)));
        }
    }
}
