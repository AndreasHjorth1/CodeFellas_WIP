using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Prosjekt.Models.ModelView.Account;

namespace Prosjekt.Controllers
{
    /// <summary>
    /// Brukerens egen profilside. Viser kontaktinformasjon og lar brukeren endre telefonnummeret sitt.
    /// </summary>
    [Authorize]
    public class ProfileController(UserManager<IdentityUser> userManager) : Controller
    {
        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var user = await userManager.GetUserAsync(User);
            if (user is null)
            {
                return RedirectToAction("Login", "Account");
            }

            return View(new ProfileViewModel
            {
                Email = user.Email ?? string.Empty,
                PhoneNumber = user.PhoneNumber ?? string.Empty
            });
        }

        /// <summary>
        /// Lagrer nytt telefonnummer på den innloggede brukeren.
        /// E-posten hentes fra databasen, ikke fra skjemaet, så den ikke kan endres herfra.
        /// </summary>
        [HttpPost]
        public async Task<IActionResult> UpdatePhoneNumber(ProfileViewModel model)
        {
            var user = await userManager.GetUserAsync(User);
            if (user is null)
            {
                return RedirectToAction("Login", "Account");
            }

            model.Email = user.Email ?? string.Empty;

            if (!ModelState.IsValid)
            {
                return View(nameof(Index), model);
            }

            var result = await userManager.SetPhoneNumberAsync(user, model.PhoneNumber);
            if (!result.Succeeded)
            {
                foreach (var error in result.Errors)
                {
                    ModelState.AddModelError(string.Empty, error.Description);
                }
                return View(nameof(Index), model);
            }

            TempData["Melding"] = "Telefonnummeret er oppdatert.";
            return RedirectToAction(nameof(Index));
        }
    }
}