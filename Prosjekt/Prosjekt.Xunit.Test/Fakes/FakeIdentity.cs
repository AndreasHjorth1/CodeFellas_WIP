using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Routing;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Logging.Abstractions;
using System.Security.Claims;
using SignInResult = Microsoft.AspNetCore.Identity.SignInResult;
using Prosjekt.Models.Entities;

namespace Prosjekt.Xunit.Test.Fakes
{
    /// <summary>
    /// UserManager uten database. Testen bestemmer hva metodene svarer, og kan sjekke hva de fikk inn.
    /// </summary>
    public class FakeUserManager : UserManager<ApplicationUser>
    {
        public ApplicationUser? UserToFind { get; set; }
        public bool EmailConfirmed { get; set; } = true;
        public IdentityResult CreateResult { get; set; } = IdentityResult.Success;
        public IdentityResult ResetResult { get; set; } = IdentityResult.Success;
        public string ResetToken { get; set; } = "reset-token";
        public bool IsAdmin { get; set; }
        public ApplicationUser? CurrentUser { get; set; }
        public IdentityResult PhoneResult { get; set; } = IdentityResult.Success;
        public string? SavedPhoneNumber { get; private set; }

        public ApplicationUser? CreatedUser { get; private set; }
        public string? CreatedPassword { get; private set; }
        public string? UsedResetCode { get; private set; }
        public string? NewPassword { get; private set; }
        public string? AddedRole { get; private set; }

        public FakeUserManager()
            : base(new FakeUserStore(), null!, null!, null!, null!, null!, null!, null!,
                NullLogger<UserManager<ApplicationUser>>.Instance)
        {
        }

        public override Task<ApplicationUser?> FindByEmailAsync(string email) => Task.FromResult(UserToFind);

        public override Task<bool> IsEmailConfirmedAsync(ApplicationUser user) => Task.FromResult(EmailConfirmed);

        public override Task<bool> IsInRoleAsync(ApplicationUser user, string role) =>
            Task.FromResult(IsAdmin && role == "Admin");

        public override Task<string> GeneratePasswordResetTokenAsync(ApplicationUser user) => Task.FromResult(ResetToken);

        public override Task<IdentityResult> CreateAsync(ApplicationUser user, string password)
        {
            CreatedUser = user;
            CreatedPassword = password;
            return Task.FromResult(CreateResult);
        }
        public override Task<IdentityResult> AddToRoleAsync(ApplicationUser user, string role)
        {
            AddedRole = role;
            return Task.FromResult(IdentityResult.Success);
        }

        public override Task<IdentityResult> ResetPasswordAsync(ApplicationUser user, string token, string newPassword)
        {
            UsedResetCode = token;
            NewPassword = newPassword;
            return Task.FromResult(ResetResult);
        }
        public override Task<ApplicationUser?> GetUserAsync(ClaimsPrincipal principal) =>
            Task.FromResult(CurrentUser);

        public override Task<IdentityResult> SetPhoneNumberAsync(ApplicationUser user, string? phoneNumber)
        {
            SavedPhoneNumber = phoneNumber;
            if (PhoneResult.Succeeded)
            {
                user.PhoneNumber = phoneNumber;
            }
            return Task.FromResult(PhoneResult);
        }
    }

    /// <summary>
    /// UserManager krever en store, men alle metodene som brukes er overstyrt, så denne kalles aldri.
    /// </summary>
    public class FakeUserStore : IUserStore<ApplicationUser>
    {
        public void Dispose() { }
        public Task<IdentityResult> CreateAsync(ApplicationUser user, CancellationToken cancellationToken) => throw new NotImplementedException();
        public Task<IdentityResult> DeleteAsync(ApplicationUser user, CancellationToken cancellationToken) => throw new NotImplementedException();
        public Task<ApplicationUser?> FindByIdAsync(string userId, CancellationToken cancellationToken) => throw new NotImplementedException();
        public Task<ApplicationUser?> FindByNameAsync(string normalizedUserName, CancellationToken cancellationToken) => throw new NotImplementedException();
        public Task<string?> GetNormalizedUserNameAsync(ApplicationUser user, CancellationToken cancellationToken) => throw new NotImplementedException();
        public Task<string> GetUserIdAsync(ApplicationUser user, CancellationToken cancellationToken) => throw new NotImplementedException();
        public Task<string?> GetUserNameAsync(ApplicationUser user, CancellationToken cancellationToken) => throw new NotImplementedException();
        public Task SetNormalizedUserNameAsync(ApplicationUser user, string? normalizedName, CancellationToken cancellationToken) => throw new NotImplementedException();
        public Task SetUserNameAsync(ApplicationUser user, string? userName, CancellationToken cancellationToken) => throw new NotImplementedException();
        public Task<IdentityResult> UpdateAsync(ApplicationUser user, CancellationToken cancellationToken) => throw new NotImplementedException();
    }

    public class FakeSignInManager : SignInManager<ApplicationUser>
    {
        public SignInResult SignInResult { get; set; } = SignInResult.Success;
        public bool SignInCalled { get; private set; }
        public bool LockoutOnFailure { get; private set; }
        public bool SignOutCalled { get; private set; }

        public FakeSignInManager(FakeUserManager userManager)
            : base(
                userManager,
                new HttpContextAccessor(),
                new UserClaimsPrincipalFactory<ApplicationUser>(userManager, Microsoft.Extensions.Options.Options.Create(new IdentityOptions())),
                Microsoft.Extensions.Options.Options.Create(new IdentityOptions()),
                NullLogger<SignInManager<ApplicationUser>>.Instance,
                new AuthenticationSchemeProvider(Microsoft.Extensions.Options.Options.Create(new AuthenticationOptions())),
                new DefaultUserConfirmation<ApplicationUser>())
        {
        }

        public override Task<SignInResult> PasswordSignInAsync(
            ApplicationUser user, string password, bool isPersistent, bool lockoutOnFailure)
        {
            SignInCalled = true;
            LockoutOnFailure = lockoutOnFailure;
            return Task.FromResult(SignInResult);
        }

        public override Task SignOutAsync()
        {
            SignOutCalled = true;
            return Task.CompletedTask;
        }
    }

    public class FakeEmailSender : IEmailSender
    {
        public string? SentTo { get; private set; }
        public string? SentMessage { get; private set; }

        public Task SendEmailAsync(string email, string subject, string htmlMessage)
        {
            SentTo = email;
            SentMessage = htmlMessage;
            return Task.CompletedTask;
        }
    }

    /// <summary>
    /// Bruker den ekte IsLocalUrl fra UrlHelperBase, men Action lager bare en enkel adresse uten ruting.
    /// </summary>
    public class FakeUrlHelper(ActionContext context) : UrlHelperBase(context)
    {
        public override string? Action(UrlActionContext actionContext)
        {
            var values = new RouteValueDictionary(actionContext.Values);
            return $"http://test/{actionContext.Controller}/{actionContext.Action}?code={values["code"]}&email={values["email"]}";
        }

        public override string? RouteUrl(UrlRouteContext routeContext) => throw new NotImplementedException();
    }
}