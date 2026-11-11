using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Prosjekt.Models.ModelView.Account;
using Prosjekt.Models.Entities;

namespace Prosjekt.Controllers
{
    /// <summary>
    /// Brukerens egen profilside. Viser kontaktinformasjon og lar brukeren endre telefonnummeret sitt.
    /// </summary>
    [Authorize]
    public class ProfileController(UserManager<ApplicationUser> userManager) : Controller
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
                FullName = user.FullName,
                Email = user.Email ?? string.Empty,
                PhoneNumber = user.PhoneNumber ?? string.Empty
            });
        }

        /// <summary>
        /// Lagrer nytt telefonnummer på den innloggede brukeren.
        /// Navn og e-post hentes fra databasen, ikke fra skjemaet, så de ikke kan endres herfra.
        /// </summary>
        [HttpPost]
        public async Task<IActionResult> UpdatePhoneNumber(ProfileViewModel model)
        {
            var user = await userManager.GetUserAsync(User);
            if (user is null)
            {
                return RedirectToAction("Login", "Account");
            }

            model.FullName = user.FullName;
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