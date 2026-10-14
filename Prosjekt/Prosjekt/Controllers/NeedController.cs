using Microsoft.AspNetCore.Mvc;
using Prosjekt.Models.ModelView;
using Prosjekt.Models.Entities;
using System.Collections.Concurrent;
namespace Prosjekt.Controllers
{
    /// <summary>
    /// Provides MVC actions for creating, viewing, editing, and deleting NeedViewModel instances, using an in-memory,
    /// thread-safe dictionary keyed by name (case-insensitive).
    /// </summary>
    /// <remarks>POST actions use anti-forgery validation. Inputs are validated for required fields, positive
    /// totals, and valid geographic coordinates; invalid submissions return the input view with model-state errors.
    /// Edit supports renaming by removing the original key when the name changes; Delete removes an entry if
    /// present.</remarks>
    public class NeedController : Controller
    {
        private static readonly ConcurrentDictionary<string, NeedViewModel> _needDatabase = new(StringComparer.OrdinalIgnoreCase);

        [HttpGet]
        public IActionResult Index()
        {
            return View(new NeedCreatorViewModel());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Create(NeedCreatorViewModel model)
        {
            if (model.Latitude.HasValue && model.Longitude.HasValue && model.NeedList != null)
            {
                foreach (var item in model.NeedList)
                {
                    item.Latitude = model.Latitude;
                    item.Longitude = model.Longitude;
                    item.Importance = model.Importance;
                }

                foreach (var key in ModelState.Keys
                    .Where(k => k.EndsWith(".Latitude")
                    || k.EndsWith(".Longitude") || k.EndsWith(".Importance"))
                    .ToList())
                {
                    ModelState.Remove(key);
                }
            }

            if (!ModelState.IsValid)
            {
                return View("Index", model);
            }

            if (model.NeedList == null || !model.NeedList.Any())
            {
                ModelState.AddModelError("", "Du må legge til minst ett behov i listen.");
                return View("Index", model);
            }

            var coords = new Coordinates(model.Latitude!.Value, model.Longitude!.Value);
            if (!coords.IsValid())
            {
                ModelState.AddModelError("", "De oppgitte koordinatene er ugyldige.");
                return View("Index", model);
            }

            foreach (var need in model.NeedList)
            {
                _needDatabase[need.Name] = need;
            }

            return RedirectToAction("Overview");
        }

        [HttpGet]
        public IActionResult Overview()
        {
            var allNeeds = _needDatabase.Values.ToList();
            return View(allNeeds);
        }

        [HttpGet]
        public IActionResult Edit(string name)
        {
            if (string.IsNullOrEmpty(name) || !_needDatabase.TryGetValue(name, out var need))
            {
                return NotFound();
            }

            return View(need);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Edit(string originalName, NeedViewModel model)
        {
            if (string.IsNullOrEmpty(originalName))
            {
                return BadRequest();
            }

            var coords = new Coordinates(model.Latitude!.Value, model.Longitude!.Value);

            if (!coords.IsValid())
            {
                return View(model);
            }


            if (!ModelState.IsValid)
            {
                ModelState.AddModelError("", "De oppgitte koordinatene er ugyldige.");
                return View(model);
            }

            if (!originalName.Equals(model.Name, StringComparison.OrdinalIgnoreCase))
            {
                _needDatabase.TryRemove(originalName, out _);
            }

            _needDatabase[model.Name] = model;

            return RedirectToAction(nameof(Overview));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Delete(string name)
        {
            if (!string.IsNullOrEmpty(name))
            {
                _needDatabase.TryRemove(name, out _);
            }

            return RedirectToAction(nameof(Overview));
        }
    }
}