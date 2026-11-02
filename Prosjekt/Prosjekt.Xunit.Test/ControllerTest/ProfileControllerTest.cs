using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Prosjekt.Controllers;
using Prosjekt.Models.Entities;
using Prosjekt.Models.ModelView;
using Prosjekt.Utils;
using Prosjekt.Xunit.Test.Fakes;
using System;
using System.Text.Json;
using System.Threading.Tasks;
using Xunit;

namespace Prosjekt.Xunit.Test.ControllerTest
{
    public class ProfileControllerTest
    {
        private static ProfileController CreateController(FakeResourceRepository repo, ProfileFormModel? savedProfile = null)
        {
            var http = new DefaultHttpContext();
            if (savedProfile != null)
            {
                var json = Uri.EscapeDataString(JsonSerializer.Serialize(savedProfile));
                http.Request.Headers.Cookie = $"{ProfileController.CookieName}={json}";
            }

            return new ProfileController(repo)
            {
                ControllerContext = new ControllerContext { HttpContext = http }
            };
        }

        private static Resource MakeResource(string name, string phone) => new Resource
        {
            Name = name,
            PhoneNumber = phone,
            Category = "Vehicle",
            Coords = new Coordinates(58.15, 8.0)
        };

        [Fact]
        public async Task Index_NoProfile_ShowsNoResources()
        {
            // Arrange
            var repo = new FakeResourceRepository();
            await repo.AddAsync(MakeResource("Traktor", "91234567"));
            var controller = CreateController(repo);

            // Act
            var result = await controller.Index();

            // Assert
            var view = Assert.IsType<ViewResult>(result);
            var model = Assert.IsType<ProfilePageViewModel>(view.Model);
            Assert.False(model.HasProfile);
            Assert.Empty(model.Resources);
        }

        [Fact]
        public async Task Index_WithProfile_ShowsOnlyResourcesWithSamePhone()
        {
            // Arrange
            var repo = new FakeResourceRepository();
            await repo.AddAsync(MakeResource("Traktor", "+47 912 34 567"));
            await repo.AddAsync(MakeResource("Aggregat", "41234567"));
            var profile = new ProfileFormModel { Username = "kari", PhoneNumber = "912 34 567" };
            var controller = CreateController(repo, profile);

            // Act
            var result = await controller.Index();

            // Assert
            var view = Assert.IsType<ViewResult>(result);
            var model = Assert.IsType<ProfilePageViewModel>(view.Model);
            Assert.True(model.HasProfile);
            var item = Assert.Single(model.Resources);
            Assert.Equal("Traktor", item.Name);
            Assert.Equal("Kjøretøy", item.CategoryLabel);
        }

        [Fact]
        public async Task Post_InvalidModelState_ReturnsViewAndDoesNotSave()
        {
            // Arrange
            var repo = new FakeResourceRepository();
            var controller = CreateController(repo);
            controller.ModelState.AddModelError("Profile.Username", "Required");

            // Act
            var result = await controller.Index(new ProfileFormModel());

            // Assert
            Assert.IsType<ViewResult>(result);
            Assert.DoesNotContain(ProfileController.CookieName, controller.HttpContext.Response.Headers.SetCookie.ToString());
        }

        [Fact]
        public async Task Post_Valid_SavesCookieAndRedirects()
        {
            // Arrange
            var repo = new FakeResourceRepository();
            var controller = CreateController(repo);
            var profile = new ProfileFormModel { Username = " kari ", PhoneNumber = "+4791234567", Address = "Storgata 1, 4610 Kristiansand" };

            // Act
            var result = await controller.Index(profile);

            // Assert
            var redirect = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal("Index", redirect.ActionName);
            Assert.Contains(ProfileController.CookieName, controller.HttpContext.Response.Headers.SetCookie.ToString());
            Assert.Equal("kari", profile.Username);
            Assert.Equal("912 34 567", profile.PhoneNumber);
        }

        [Fact]
        public async Task Post_Valid_TrimsEmailAndStoresNullWhenEmpty()
        {
            // Arrange
            var repo = new FakeResourceRepository();
            var withEmail = new ProfileFormModel { Username = "kari", PhoneNumber = "91234567", Email = "  kari@eksempel.no " };
            var withoutEmail = new ProfileFormModel { Username = "ola", PhoneNumber = "41234567", Email = "   " };

            // Act
            await CreateController(repo).Index(withEmail);
            await CreateController(repo).Index(withoutEmail);

            // Assert
            Assert.Equal("kari@eksempel.no", withEmail.Email);
            Assert.Null(withoutEmail.Email);
        }

        [Fact]
        public async Task ToggleAvailability_OwnResource_FlipsAvailabilityAndRedirects()
        {
            // Arrange
            var repo = new FakeResourceRepository();
            await repo.AddAsync(MakeResource("Traktor", "91234567"));
            var resource = repo.Resources[0];
            resource.IsAvailable = true;
            var controller = CreateController(repo, new ProfileFormModel { Username = "kari", PhoneNumber = "912 34 567" });

            // Act
            var first = await controller.ToggleAvailability(resource.Id);
            await controller.ToggleAvailability(resource.Id);

            // Assert
            var redirect = Assert.IsType<RedirectToActionResult>(first);
            Assert.Equal("Index", redirect.ActionName);
            Assert.True(resource.IsAvailable); // av og på igjen
        }

        [Fact]
        public async Task ToggleAvailability_SetsUnavailable()
        {
            // Arrange
            var repo = new FakeResourceRepository();
            await repo.AddAsync(MakeResource("Traktor", "91234567"));
            var resource = repo.Resources[0];
            resource.IsAvailable = true;
            var controller = CreateController(repo, new ProfileFormModel { Username = "kari", PhoneNumber = "91234567" });

            // Act
            await controller.ToggleAvailability(resource.Id);

            // Assert
            Assert.False(resource.IsAvailable);
        }

        [Fact]
        public async Task ToggleAvailability_SomeoneElsesResource_ReturnsForbiddenAndDoesNotChange()
        {
            // Arrange
            var repo = new FakeResourceRepository();
            await repo.AddAsync(MakeResource("Aggregat", "41234567"));
            var resource = repo.Resources[0];
            resource.IsAvailable = true;
            var controller = CreateController(repo, new ProfileFormModel { Username = "kari", PhoneNumber = "91234567" });

            // Act
            var result = await controller.ToggleAvailability(resource.Id);

            // Assert
            var status = Assert.IsType<StatusCodeResult>(result);
            Assert.Equal(StatusCodes.Status403Forbidden, status.StatusCode);
            Assert.True(resource.IsAvailable);
        }

        [Fact]
        public async Task ToggleAvailability_NoProfile_RedirectsAndDoesNotChange()
        {
            // Arrange
            var repo = new FakeResourceRepository();
            await repo.AddAsync(MakeResource("Traktor", "91234567"));
            var resource = repo.Resources[0];
            resource.IsAvailable = true;
            var controller = CreateController(repo);

            // Act
            var result = await controller.ToggleAvailability(resource.Id);

            // Assert
            Assert.IsType<RedirectToActionResult>(result);
            Assert.True(resource.IsAvailable);
        }

        [Fact]
        public async Task ToggleAvailability_UnknownId_ReturnsNotFound()
        {
            // Arrange
            var repo = new FakeResourceRepository();
            var controller = CreateController(repo, new ProfileFormModel { Username = "kari", PhoneNumber = "91234567" });

            // Act
            var result = await controller.ToggleAvailability(99);

            // Assert
            Assert.IsType<NotFoundResult>(result);
        }

        [Theory]
        [InlineData("912 34 567", "91234567")]
        [InlineData("+47 912 34 567", "91234567")]
        [InlineData("0047 91234567", "91234567")]
        [InlineData("", "")]
        [InlineData(null, "")]
        public void PhoneNumberHelper_Normalize_RemovesSpacesAndCountryCode(string? input, string expected)
        {
            Assert.Equal(expected, PhoneNumberHelper.Normalize(input));
        }
    }
}