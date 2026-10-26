using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Prosjekt.DataAccess.Repositories;
using Prosjekt.Models.Entities;
using Prosjekt.Models.ModelView;
using Prosjekt.Utils;

namespace Prosjekt.Controllers
{
    [Authorize]
    public class GeoChangeController : Controller
    {
        private readonly IGeoChangeRepository _repo;
        private readonly UserManager<IdentityUser> _userManager; // 2. Legg til UserManager

        public GeoChangeController(IGeoChangeRepository repo, UserManager<IdentityUser> userManager)
        {
            _repo = repo;
            _userManager = userManager;
        }

        [HttpGet]
        public IActionResult CorrectMap()
        {
            var model = new GeoChangeViewModel();
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CorrectMap(GeoChangeViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var coords = new Coordinates(model.Latitude!.Value, model.Longitude!.Value);

            if (!coords.IsValid())
            {
                ModelState.AddModelError(string.Empty, "De oppgitte koordinatene er ugyldige.");
                return View(model);
            }

            // 3. Hent den innloggede brukeren
            var user = await _userManager.GetUserAsync(User);

            var geoChange = new GeoChange
            {
                Coords = coords,
                Description = model.Description,
                Radius = model.Radius,
                ChangeTypes = model.SelectedChangeTypes != null && model.SelectedChangeTypes.Any()
                    ? string.Join(", ", model.SelectedChangeTypes)
                    : string.Empty,
                CreatedAt = NorwegianTime.Now,
                UserId = user?.Id ?? string.Empty
            };

            await _repo.AddAsync(geoChange);

            return RedirectToAction(nameof(CorrectionOverview));
        }

        [HttpGet]
        public async Task<IActionResult> CorrectionOverview()
        {
            var entities = await _repo.GetAllAsync();

            var viewModels = entities.Select(e => new GeoChangeViewModel
            {
                Latitude = e.Coords.Latitude,
                Longitude = e.Coords.Longitude,
                Description = e.Description,
                Radius = e.Radius,
                SelectedChangeTypes = !string.IsNullOrEmpty(e.ChangeTypes)
                    ? e.ChangeTypes.Split(new[] { ", " }, StringSplitOptions.RemoveEmptyEntries).ToList()
                    : new List<string>()
            }).ToList();

            return View(viewModels);
        }
    }
}