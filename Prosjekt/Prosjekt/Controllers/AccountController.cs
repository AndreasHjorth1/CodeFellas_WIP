using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.WebUtilities;
using System.Text;
using System.Text.Encodings.Web;
using Prosjekt.Models.Authorization;
using Prosjekt.Models.ModelView.Account;

namespace Prosjekt.Controllers
{
    /// <summary>
    /// Registrering, innlogging, utlogging og tilbakestilling av passord.
    /// POST-handlingene er CSRF-beskyttet av et globalt filter i Program.cs.
    /// </summary>
    public sealed class AccountController(UserManager<IdentityUser> userManager, SignInManager<IdentityUser> signInManager,
    IEmailSender emailSender) : Controller
    {
        [AllowAnonymous]
        [HttpGet]
        public IActionResult Login(string? returnUrl = null)
        {
            return View(new LoginViewModel { ReturnUrl = returnUrl });
        }


        [AllowAnonymous]
        [HttpGet]
        public IActionResult AccessDenied()
        {
            return View();
        }


        /// <summary>
        /// Logger inn brukeren. Kontoen sperres etter for mange feil forsøk.
        /// Feilmeldingen er lik for ukjent e-post og feil passord, så den ikke avslører hvem som er registrert.
        /// </summary>
        [AllowAnonymous]
        [HttpPost]
        public async Task<IActionResult> Login(LoginViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var user = await userManager.FindByEmailAsync(model.Email);
            if (user is not null)
            {
                var result = await signInManager.PasswordSignInAsync(
                    user,
                    model.Password,
                    model.RememberMe,
                    lockoutOnFailure: true);
                if (result.Succeeded)
                {
                    /// <summary>
                    /// Ressursgivere startet på oversikts-siden
                    /// </summary>
                    if (!Url.IsLocalUrl(model.ReturnUrl)
                        && !await userManager.IsInRoleAsync(user, RoleNames.Admin)
                        && !await userManager.IsInRoleAsync(user, RoleNames.Coordinator))
                    {
                        return RedirectToAction("Overview", "Resource");
                    }
                    return RedirectToLocalUrl(model.ReturnUrl);
                }
            }

            ModelState.AddModelError(string.Empty, "Ugyldig e-post eller passord.");
            return View(model);
        }


        [Authorize]
        [HttpPost]
        public async Task<IActionResult> Logout()
        {
            await signInManager.SignOutAsync();
            return RedirectToAction(nameof(Login));
        }


        [AllowAnonymous]
        [HttpGet]
        public IActionResult Register()
        {
            return View(new RegisterViewModel());
        }


        /// <summary>
        /// Oppretter en ny bruker. Appen sender ikke ekte e-post, så adressen regnes som bekreftet.
        /// </summary>
        [AllowAnonymous]
        [HttpPost]
        public async Task<IActionResult> Register(RegisterViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var user = new IdentityUser
            {
                UserName = model.Email,
                Email = model.Email,
                PhoneNumber = model.PhoneNumber,
                EmailConfirmed = true
            };
            var result = await userManager.CreateAsync(user, model.Password);

            if (result.Succeeded)
            {
                return RedirectToAction(nameof(Login));
            }

            foreach (var error in result.Errors)
            {
                ModelState.AddModelError(string.Empty, error.Description);
            }

            return View(model);
        }


        [Authorize(Roles = RoleNames.Admin)]
        [HttpGet]
        public IActionResult CreateUser()
        {
            return View(new CreateUserViewModel());
        }


        [Authorize(Roles = RoleNames.Admin)]
        [HttpPost]
        public async Task<IActionResult> CreateUser(CreateUserViewModel model)
        {
            if (model.Role != RoleNames.Admin && model.Role != RoleNames.Coordinator)
            {
                ModelState.AddModelError(nameof(model.Role), "Velg enten admin eller koordinator.");
            }

            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var user = new IdentityUser
            {
                UserName = model.Email,
                Email = model.Email,
                EmailConfirmed = true
            };
            var result = await userManager.CreateAsync(user, model.Password);

            if (result.Succeeded)
            {
                await userManager.AddToRoleAsync(user, model.Role);
                TempData["CreateUserSuccess"] = $"Brukeren {model.Email} ble opprettet.";
                return RedirectToAction(nameof(CreateUser));
            }

            foreach (var error in result.Errors)
            {
                ModelState.AddModelError(string.Empty, error.Description);
            }

            return View(model);
        }


        [AllowAnonymous]
        [HttpGet]
        public IActionResult ForgotPassword()
        {
            return View(new ForgotPasswordViewModel());
        }


        /// <summary>
        /// Lager en lenke for å tilbakestille passordet (se ConsoleEmailSender).
        /// Svaret er det samme enten kontoen finnes eller ikke.
        /// </summary>
        [AllowAnonymous]
        [HttpPost]
        public async Task<IActionResult> ForgotPassword(ForgotPasswordViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var user = await userManager.FindByEmailAsync(model.Email);
            if (user is not null && await userManager.IsEmailConfirmedAsync(user))
            {
                var code = await userManager.GeneratePasswordResetTokenAsync(user);
                var encodedCode = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(code));
                var callbackUrl = Url.Action(
                    nameof(ResetPassword),
                    "Account",
                    new { code = encodedCode, email = user.Email },
                    Request.Scheme)
                    ?? throw new InvalidOperationException("Kunne ikke generere URL for tilbakestilling av passord.");

                await emailSender.SendEmailAsync(
                    user.Email!,
                    "Tilbakestill passord",
                    $"Tilbakestill passord ved å <a href='{HtmlEncoder.Default.Encode(callbackUrl)}'>klikke her</a>.");
            }

            /// <summary>   
            /// Ber en innlogget bruker om nytt passord (fra Min profil), logges de ut og må logge inn igjen
            /// <summary>
           
            if (signInManager.IsSignedIn(User))
            {
                await signInManager.SignOutAsync();
            }

            return RedirectToAction(nameof(ForgotPasswordConfirmation));
        }


        [AllowAnonymous]
        [HttpGet]
        public IActionResult ForgotPasswordConfirmation()
        {
            return View();
        }


        [AllowAnonymous]
        [HttpGet]
        public IActionResult ResetPassword(string? code = null, string? email = null)
        {
            if (string.IsNullOrWhiteSpace(code) || string.IsNullOrWhiteSpace(email))
            {
                return BadRequest("En tilbakestillingskode og e-postadresse er påkrevd.");
            }

            return View(new ResetPasswordViewModel { Code = code, Email = email });
        }


        /// <summary>
        /// Setter nytt passord. Ukjent konto gir samme side som suksess, så siden ikke avslører hvem som er registrert.
        /// </summary>
        [AllowAnonymous]
        [HttpPost]
        public async Task<IActionResult> ResetPassword(ResetPasswordViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var user = await userManager.FindByEmailAsync(model.Email);
            if (user is null)
            {
                return RedirectToAction(nameof(ResetPasswordConfirmation));
            }

            string code;
            
            try
            {
                code = Encoding.UTF8.GetString(WebEncoders.Base64UrlDecode(model.Code));
            }
            catch (FormatException)
            {
                ModelState.AddModelError(string.Empty, "Koden for tilbakestilling av passord er ugyldig.");
                return View(model);
            }

            var result = await userManager.ResetPasswordAsync(user, code, model.Password);
            if (result.Succeeded)
            {
                return RedirectToAction(nameof(ResetPasswordConfirmation));
            }

            foreach (var error in result.Errors)
            {
                ModelState.AddModelError(string.Empty, error.Description);
            }

            return View(model);
        }


        [AllowAnonymous]
        [HttpGet]
        public IActionResult ResetPasswordConfirmation()
        {
            return View();
        } 

        /// <summary>
        /// Sender bare til lokale adresser, så returnUrl ikke kan føre brukeren til en annen nettside.
        /// </summary>
        private IActionResult RedirectToLocalUrl(string? returnUrl)
        {
            return Url.IsLocalUrl(returnUrl) ? Redirect(returnUrl) : RedirectToAction("Index", "Home")!;
        }
    }
}
