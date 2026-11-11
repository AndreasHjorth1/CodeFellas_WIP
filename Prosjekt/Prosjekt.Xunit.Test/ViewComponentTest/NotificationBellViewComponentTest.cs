using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.ViewComponents;
using Prosjekt.Models.Entities;
using Prosjekt.ViewComponents;
using Prosjekt.Xunit.Test.Fakes;
using System.Security.Claims;

namespace Prosjekt.Xunit.Test.ViewComponentTest
{
    public class NotificationBellViewComponentTest
    {
        private static NotificationBellViewComponent CreateComponent(FakeNotificationRepository repo, ClaimsPrincipal user)
        {
            return new NotificationBellViewComponent(repo)
            {
                ViewComponentContext = new ViewComponentContext
                {
                    ViewContext = new ViewContext { HttpContext = new DefaultHttpContext { User = user } }
                }
            };
        }

        [Fact]
        public async Task InvokeAsync_SignedIn_ReturnsUnreadCount()
        {
            // Arrange
            var repo = new FakeNotificationRepository();
            repo.Notifications.Add(new Notification { Id = 1, UserId = "user-1", Message = "Ulest" });
            repo.Notifications.Add(new Notification { Id = 2, UserId = "user-1", Message = "Ulest" });
            var read = new Notification { Id = 3, UserId = "user-1", Message = "Lest" };
            read.MarkAsRead();
            repo.Notifications.Add(read);
            repo.Notifications.Add(new Notification { Id = 4, UserId = "user-2", Message = "Noen andres" });

            var user = new ClaimsPrincipal(new ClaimsIdentity(
                new[] { new Claim(ClaimTypes.NameIdentifier, "user-1") }, "Test"));
            var component = CreateComponent(repo, user);

            // Act
            var result = await component.InvokeAsync();

            // Assert
            var view = Assert.IsType<ViewViewComponentResult>(result);
            Assert.Equal(2, view.ViewData!.Model);
        }

        [Fact]
        public async Task InvokeAsync_NotSignedIn_ReturnsZero()
        {
            // Arrange
            var repo = new FakeNotificationRepository();
            repo.Notifications.Add(new Notification { Id = 1, UserId = "user-1", Message = "Ulest" });
            var component = CreateComponent(repo, new ClaimsPrincipal(new ClaimsIdentity()));

            // Act
            var result = await component.InvokeAsync();

            // Assert
            var view = Assert.IsType<ViewViewComponentResult>(result);
            Assert.Equal(0, view.ViewData!.Model);
        }
    }
}
