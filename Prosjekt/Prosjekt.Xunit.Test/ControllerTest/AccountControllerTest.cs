using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.WebUtilities;
using Prosjekt.Controllers;
using Prosjekt.Models.ModelView.Account;
using Prosjekt.Xunit.Test.Fakes;
using System.Text;
using SignInResult = Microsoft.AspNetCore.Identity.SignInResult;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.AspNetCore.Authorization;
using Prosjekt.Models.Authorization;
using Prosjekt.Models.Entities;

namespace Prosjekt.Xunit.Test.ControllerTest
{
    public class AccountControllerTest
    {
        private readonly FakeUserManager _userManager = new();
        private readonly FakeSignInManager _signInManager;
        private readonly FakeEmailSender _emailSender = new();
        private readonly AccountController _controller;

        public AccountControllerTest()
        {
            _signInManager = new FakeSignInManager(_userManager);

            var httpContext = new DefaultHttpContext();
            var actionContext = new ActionContext(httpContext, new RouteData(), new ActionDescriptor());

            _controller = new AccountController(_userManager, _signInManager, _emailSender)
            {
                ControllerContext = new ControllerContext { HttpContext = httpContext },
                Url = new FakeUrlHelper(actionContext),
                TempData = new TempDataDictionary(httpContext, new FakeTempDataProvider())
            };
        }

        private static ApplicationUser TestUser() => new() { Email = "ola@test.no", UserName = "ola@test.no" };

        private static string EncodeCode(string code) =>
            WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(code));

        // ---------- Sider som bare viser et view ----------

        [Fact]
        public void Login_Get_KeepsReturnUrl()
        {
            var result = _controller.Login("/Need/Overview");

            var view = Assert.IsType<ViewResult>(result);
            var model = Assert.IsType<LoginViewModel>(view.Model);
            Assert.Equal("/Need/Overview", model.ReturnUrl);
        }

        [Fact]
        public void AccessDenied_ReturnsView()
        {
            Assert.IsType<ViewResult>(_controller.AccessDenied());
        }

        [Fact]
        public void Register_Get_ReturnsViewWithEmptyModel()
        {
            var view = Assert.IsType<ViewResult>(_controller.Register());
            Assert.IsType<RegisterViewModel>(view.Model);
        }

        [Fact]
        public void ForgotPassword_Get_ReturnsViewWithEmptyModel()
        {
            var view = Assert.IsType<ViewResult>(_controller.ForgotPassword());
            Assert.IsType<ForgotPasswordViewModel>(view.Model);
        }

        [Fact]
        public void ForgotPasswordConfirmation_ReturnsView()
        {
            Assert.IsType<ViewResult>(_controller.ForgotPasswordConfirmation());
        }

        [Fact]
        public void ResetPasswordConfirmation_ReturnsView()
        {
            Assert.IsType<ViewResult>(_controller.ResetPasswordConfirmation());
        }

        // ---------- Login ----------

        [Fact]
        public async Task Login_Post_InvalidModel_ReturnsViewWithoutSigningIn()
        {
            _userManager.UserToFind = TestUser();
            _controller.ModelState.AddModelError("Email", "Required");
            var model = new LoginViewModel();

            var result = await _controller.Login(model);

            var view = Assert.IsType<ViewResult>(result);
            Assert.Same(model, view.Model);
            Assert.False(_signInManager.SignInCalled);
        }

        [Fact]
        public async Task Login_Post_UnknownEmail_ShowsErrorWithoutSigningIn()
        {
            var model = new LoginViewModel { Email = "ukjent@test.no", Password = "passord" };

            var result = await _controller.Login(model);

            Assert.IsType<ViewResult>(result);
            Assert.False(_signInManager.SignInCalled);
            Assert.Contains(_controller.ModelState[string.Empty]!.Errors,
                e => e.ErrorMessage == "Ugyldig e-post eller passord.");
        }

        [Fact]
        public async Task Login_Post_WrongPassword_ShowsSameErrorAsUnknownEmail()
        {
            _userManager.UserToFind = TestUser();
            _signInManager.SignInResult = SignInResult.Failed;
            var model = new LoginViewModel { Email = "ola@test.no", Password = "feil" };

            var result = await _controller.Login(model);

            Assert.IsType<ViewResult>(result);
            Assert.Contains(_controller.ModelState[string.Empty]!.Errors,
                e => e.ErrorMessage == "Ugyldig e-post eller passord.");
        }

        [Fact]
        public async Task Login_Post_UsesLockoutOnFailure()
        {
            _userManager.UserToFind = TestUser();
            var model = new LoginViewModel { Email = "ola@test.no", Password = "passord" };

            await _controller.Login(model);

            Assert.True(_signInManager.LockoutOnFailure);
        }

        [Fact]
        public async Task Login_Post_Success_WithoutReturnUrl_UserRedirectsToResourceOverview()
        {
            _userManager.UserToFind = TestUser();
            var model = new LoginViewModel { Email = "ola@test.no", Password = "passord" };

            var result = await _controller.Login(model);

            var redirect = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal("Overview", redirect.ActionName);
            Assert.Equal("Resource", redirect.ControllerName);
        }

        [Fact]
        public async Task Login_Post_Success_WithoutReturnUrl_AdminRedirectsToHome()
        {
            _userManager.UserToFind = TestUser();
            _userManager.IsAdmin = true;
            var model = new LoginViewModel { Email = "ola@test.no", Password = "passord" };

            var result = await _controller.Login(model);

            var redirect = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal("Index", redirect.ActionName);
            Assert.Equal("Home", redirect.ControllerName);
        }

        [Fact]
        public async Task Login_Post_Success_WithLocalReturnUrl_RedirectsThere()
        {
            _userManager.UserToFind = TestUser();
            var model = new LoginViewModel { Email = "ola@test.no", Password = "passord", ReturnUrl = "/Need/Overview" };

            var result = await _controller.Login(model);

            var redirect = Assert.IsType<RedirectResult>(result);
            Assert.Equal("/Need/Overview", redirect.Url);
        }

        /// Sikkerhet: innlogging skal ikke kunne sende brukeren til en annen nettside
        [Theory]
        [InlineData("https://evil.example.com")]
        [InlineData("//evil.example.com")]
        public async Task Login_Post_Success_WithExternalReturnUrl_StaysOnSite(string returnUrl)
        {
            _userManager.UserToFind = TestUser();
            var model = new LoginViewModel { Email = "ola@test.no", Password = "passord", ReturnUrl = returnUrl };

            var result = await _controller.Login(model);

            Assert.IsType<RedirectToActionResult>(result);
        }

        // ---------- Logout ----------

        [Fact]
        public async Task Logout_SignsOutAndRedirectsToLogin()
        {
            var result = await _controller.Logout();

            var redirect = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal("Login", redirect.ActionName);
            Assert.True(_signInManager.SignOutCalled);
        }

        // ---------- Register ----------

        [Fact]
        public async Task Register_Post_InvalidModel_DoesNotCreateUser()
        {
            _controller.ModelState.AddModelError("Email", "Required");

            var result = await _controller.Register(new RegisterViewModel());

            Assert.IsType<ViewResult>(result);
            Assert.Null(_userManager.CreatedUser);
        }

        [Fact]
        public async Task Register_Post_Success_CreatesConfirmedUserAndRedirectsToLogin()
        {
            var model = new RegisterViewModel
            {
                FullName = "  Ola Nordmann ",
                Email = "ny@test.no",
                Password = "Passord1!",
                ConfirmPassword = "Passord1!"
            };

            var result = await _controller.Register(model);

            var redirect = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal("Login", redirect.ActionName);
            Assert.Equal("ny@test.no", _userManager.CreatedUser!.Email);
            Assert.Equal("ny@test.no", _userManager.CreatedUser.UserName);
            Assert.True(_userManager.CreatedUser.EmailConfirmed);
            Assert.Equal("Passord1!", _userManager.CreatedPassword);

            /// Navnet lagres på brukeren, uten mellomrom i endene
            Assert.Equal("Ola Nordmann", _userManager.CreatedUser.FullName);
        }

        [Fact]
        public async Task Register_Post_Failure_ShowsErrorsFromIdentity()
        {
            _userManager.CreateResult = IdentityResult.Failed(new IdentityError { Description = "For kort passord" });
            var model = new RegisterViewModel { Email = "ny@test.no", Password = "a", ConfirmPassword = "a" };

            var result = await _controller.Register(model);

            Assert.IsType<ViewResult>(result);
            Assert.Contains(_controller.ModelState[string.Empty]!.Errors,
                e => e.ErrorMessage == "For kort passord");
        }

        // ---------- Opprett bruker (admin) ----------

        [Fact]
        public void CreateUser_Get_ReturnsViewWithEmptyModel()
        {
            var view = Assert.IsType<ViewResult>(_controller.CreateUser());
            Assert.IsType<CreateUserViewModel>(view.Model);
        }

        /// Sikkerhet: begge CreateUser-metodene skal bare kunne brukes av admin
        [Fact]
        public void CreateUser_OnlyAdminHasAccess()
        {
            var methods = typeof(AccountController).GetMethods().Where(m => m.Name == nameof(AccountController.CreateUser));

            Assert.Equal(2, methods.Count());
            foreach (var method in methods)
            {
                var authorize = method.GetCustomAttributes(typeof(AuthorizeAttribute), false).Cast<AuthorizeAttribute>().Single();
                Assert.Equal(RoleNames.Admin, authorize.Roles);
            }
        }

        [Theory]
        [InlineData(RoleNames.Admin)]
        [InlineData(RoleNames.Coordinator)]
        public async Task CreateUser_Post_Success_CreatesUserWithRoleAndRedirects(string role)
        {
            var model = new CreateUserViewModel
            {
                FullName = "Kari Nordmann",
                Email = "ny@test.no",
                Password = "Passord1!",
                ConfirmPassword = "Passord1!",
                Role = role
            };

            var result = await _controller.CreateUser(model);

            var redirect = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal("CreateUser", redirect.ActionName);
            Assert.Equal("ny@test.no", _userManager.CreatedUser!.Email);
            Assert.True(_userManager.CreatedUser.EmailConfirmed);
            Assert.Equal("Kari Nordmann", _userManager.CreatedUser.FullName);
            Assert.Equal(role, _userManager.AddedRole);
            Assert.NotNull(_controller.TempData["CreateUserSuccess"]);
        }

        /// Sikkerhet: man skal ikke kunne sende inn en annen rolle enn admin eller koordinator
        [Theory]
        [InlineData("")]
        [InlineData("User")]
        [InlineData("admin")]
        public async Task CreateUser_Post_InvalidRole_DoesNotCreateUser(string role)
        {
            var model = new CreateUserViewModel
            {
                Email = "ny@test.no",
                Password = "Passord1!",
                ConfirmPassword = "Passord1!",
                Role = role
            };

            var result = await _controller.CreateUser(model);

            Assert.IsType<ViewResult>(result);
            Assert.Null(_userManager.CreatedUser);
            Assert.Null(_userManager.AddedRole);
            Assert.True(_controller.ModelState.ContainsKey(nameof(CreateUserViewModel.Role)));
        }

        [Fact]
        public async Task CreateUser_Post_InvalidModel_DoesNotCreateUser()
        {
            _controller.ModelState.AddModelError("Email", "Required");
            var model = new CreateUserViewModel { Role = RoleNames.Coordinator };

            var result = await _controller.CreateUser(model);

            var view = Assert.IsType<ViewResult>(result);
            Assert.Same(model, view.Model);
            Assert.Null(_userManager.CreatedUser);
        }

        [Fact]
        public async Task CreateUser_Post_Failure_ShowsErrorsAndGivesNoRole()
        {
            _userManager.CreateResult = IdentityResult.Failed(new IdentityError
            {
                Description = "E-posten er allerede i bruk"
            });
            var model = new CreateUserViewModel
            {
                Email = "ny@test.no",
                Password = "Passord1!",
                ConfirmPassword = "Passord1!",
                Role = RoleNames.Admin
            };

            var result = await _controller.CreateUser(model);

            Assert.IsType<ViewResult>(result);
            Assert.Null(_userManager.AddedRole);
            Assert.Contains(_controller.ModelState[string.Empty]!.Errors,
                e => e.ErrorMessage == "E-posten er allerede i bruk");
        }

        // ---------- Glemt passord ----------

        [Fact]
        public async Task ForgotPassword_Post_InvalidModel_SendsNoEmail()
        {
            _userManager.UserToFind = TestUser();
            _controller.ModelState.AddModelError("Email", "Required");

            var result = await _controller.ForgotPassword(new ForgotPasswordViewModel());

            Assert.IsType<ViewResult>(result);
            Assert.Null(_emailSender.SentTo);
        }

        /// Sikkerhet: svaret skal være likt, så siden ikke avslører hvem som er registrert
        [Fact]
        public async Task ForgotPassword_Post_UnknownEmail_SendsNoEmailButGivesSameRedirect()
        {
            var result = await _controller.ForgotPassword(new ForgotPasswordViewModel { Email = "ukjent@test.no" });

            var redirect = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal("ForgotPasswordConfirmation", redirect.ActionName);
            Assert.Null(_emailSender.SentTo);
        }

        [Fact]
        public async Task ForgotPassword_Post_UnconfirmedEmail_SendsNoEmail()
        {
            _userManager.UserToFind = TestUser();
            _userManager.EmailConfirmed = false;

            var result = await _controller.ForgotPassword(new ForgotPasswordViewModel { Email = "ola@test.no" });

            Assert.IsType<RedirectToActionResult>(result);
            Assert.Null(_emailSender.SentTo);
        }

        [Fact]
        public async Task ForgotPassword_Post_KnownEmail_SendsEmailWithEncodedCode()
        {
            _userManager.UserToFind = TestUser();

            var result = await _controller.ForgotPassword(new ForgotPasswordViewModel { Email = "ola@test.no" });

            var redirect = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal("ForgotPasswordConfirmation", redirect.ActionName);
            Assert.Equal("ola@test.no", _emailSender.SentTo);
            Assert.Contains(EncodeCode("reset-token"), _emailSender.SentMessage);
        }

        // ---------- Tilbakestille passord ----------

        [Theory]
        [InlineData(null, "ola@test.no")]
        [InlineData("kode", null)]
        [InlineData("", "ola@test.no")]
        [InlineData("kode", " ")]
        public void ResetPassword_Get_MissingCodeOrEmail_ReturnsBadRequest(string? code, string? email)
        {
            Assert.IsType<BadRequestObjectResult>(_controller.ResetPassword(code, email));
        }

        [Fact]
        public void ResetPassword_Get_WithCodeAndEmail_FillsModel()
        {
            var view = Assert.IsType<ViewResult>(_controller.ResetPassword("kode", "ola@test.no"));

            var model = Assert.IsType<ResetPasswordViewModel>(view.Model);
            Assert.Equal("kode", model.Code);
            Assert.Equal("ola@test.no", model.Email);
        }

        [Fact]
        public async Task ResetPassword_Post_InvalidModel_DoesNotResetPassword()
        {
            _userManager.UserToFind = TestUser();
            _controller.ModelState.AddModelError("Password", "Required");

            var result = await _controller.ResetPassword(new ResetPasswordViewModel());

            Assert.IsType<ViewResult>(result);
            Assert.Null(_userManager.UsedResetCode);
        }

        /// Sikkerhet: ukjent konto gir samme side som suksess
        [Fact]
        public async Task ResetPassword_Post_UnknownEmail_RedirectsToConfirmation()
        {
            var model = new ResetPasswordViewModel { Email = "ukjent@test.no", Code = EncodeCode("kode"), Password = "Nytt1!" };

            var result = await _controller.ResetPassword(model);

            var redirect = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal("ResetPasswordConfirmation", redirect.ActionName);
            Assert.Null(_userManager.UsedResetCode);
        }

        [Fact]
        public async Task ResetPassword_Post_InvalidCodeFormat_ShowsError()
        {
            _userManager.UserToFind = TestUser();
            var model = new ResetPasswordViewModel { Email = "ola@test.no", Code = "a", Password = "Nytt1!" };

            var result = await _controller.ResetPassword(model);

            Assert.IsType<ViewResult>(result);
            Assert.Null(_userManager.UsedResetCode);
            Assert.Contains(_controller.ModelState[string.Empty]!.Errors,
                e => e.ErrorMessage == "Koden for tilbakestilling av passord er ugyldig.");
        }

        [Fact]
        public async Task ResetPassword_Post_Success_DecodesCodeAndRedirects()
        {
            _userManager.UserToFind = TestUser();
            var model = new ResetPasswordViewModel { Email = "ola@test.no", Code = EncodeCode("kode"), Password = "Nytt1!" };

            var result = await _controller.ResetPassword(model);

            var redirect = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal("ResetPasswordConfirmation", redirect.ActionName);
            Assert.Equal("kode", _userManager.UsedResetCode);
            Assert.Equal("Nytt1!", _userManager.NewPassword);
        }

        [Fact]
        public async Task ResetPassword_Post_Failure_ShowsErrorsFromIdentity()
        {
            _userManager.UserToFind = TestUser();
            _userManager.ResetResult = IdentityResult.Failed(new IdentityError { Description = "Ugyldig kode" });
            var model = new ResetPasswordViewModel { Email = "ola@test.no", Code = EncodeCode("kode"), Password = "Nytt1!" };

            var result = await _controller.ResetPassword(model);

            Assert.IsType<ViewResult>(result);
            Assert.Contains(_controller.ModelState[string.Empty]!.Errors,
                e => e.ErrorMessage == "Ugyldig kode");
        }
    }
}