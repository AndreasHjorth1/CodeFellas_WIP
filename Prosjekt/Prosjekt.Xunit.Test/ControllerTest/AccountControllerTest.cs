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
                Url = new FakeUrlHelper(actionContext)
            };
        }

        private static IdentityUser TestUser() => new() { Email = "test@test.no", UserName = "test@test.no" };

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

        // Neste tester kommer her
    }
}