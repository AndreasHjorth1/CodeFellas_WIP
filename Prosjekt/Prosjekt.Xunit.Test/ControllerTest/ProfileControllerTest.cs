using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Prosjekt.Controllers;
using Prosjekt.Models.ModelView.Account;
using Prosjekt.Xunit.Test.Fakes;
using Xunit;
using Prosjekt.Models.Entities;

namespace Prosjekt.Xunit.Test.ControllerTest
{
    public class ProfileControllerTest
    {
        private readonly FakeUserManager _userManager = new();
        private readonly ProfileController _controller;

        public ProfileControllerTest()
        {
            var httpContext = new DefaultHttpContext();

            _controller = new ProfileController(_userManager)
            {
                ControllerContext = new ControllerContext { HttpContext = httpContext },
                TempData = new TempDataDictionary(httpContext, new FakeTempDataProvider())
            };
        }

        private static ApplicationUser TestUser() =>
            new() { Email = "ola@test.no", UserName = "ola@test.no", PhoneNumber = "98765432", FullName = "Ola Nordmann" };

        [Fact]
        public async Task Index_ShowsNameEmailAndPhoneNumber()
        {
            _userManager.CurrentUser = TestUser();

            var result = await _controller.Index();

            var view = Assert.IsType<ViewResult>(result);
            var model = Assert.IsType<ProfileViewModel>(view.Model);
            Assert.Equal("Ola Nordmann", model.FullName);
            Assert.Equal("ola@test.no", model.Email);
            Assert.Equal("98765432", model.PhoneNumber);
        }

        /// Sikkerhet: navn og e-post sendt inn i skjemaet skal ikke brukes, de hentes fra databasen
        [Fact]
        public async Task UpdatePhoneNumber_InvalidModel_KeepsNameAndEmailFromDatabase()
        {
            _userManager.CurrentUser = TestUser();
            _controller.ModelState.AddModelError("PhoneNumber", "Ugyldig telefonnummer.");

            var result = await _controller.UpdatePhoneNumber(
                new ProfileViewModel { FullName = "Hacker", Email = "annen@test.no", PhoneNumber = "tull" });

            var model = Assert.IsType<ProfileViewModel>(Assert.IsType<ViewResult>(result).Model);
            Assert.Equal("Ola Nordmann", model.FullName);
            Assert.Equal("ola@test.no", model.Email);
        }

        [Fact]
        public async Task UpdatePhoneNumber_SavesNewNumber()
        {
            _userManager.CurrentUser = TestUser();

            var result = await _controller.UpdatePhoneNumber(new ProfileViewModel { PhoneNumber = "41414141" });

            Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal("41414141", _userManager.SavedPhoneNumber);
        }

        [Fact]
        public async Task UpdatePhoneNumber_InvalidModel_ReturnsViewWithoutSaving()
        {
            _userManager.CurrentUser = TestUser();
            _controller.ModelState.AddModelError("PhoneNumber", "Ugyldig telefonnummer.");

            var result = await _controller.UpdatePhoneNumber(new ProfileViewModel { PhoneNumber = "tull" });

            Assert.IsType<ViewResult>(result);
            Assert.Null(_userManager.SavedPhoneNumber);
        }
    }
}