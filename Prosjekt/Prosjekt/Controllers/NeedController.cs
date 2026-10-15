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
            var model = new NeedCreatorViewModel
            {
                NeedList = new List<NeedViewModel> { new NeedViewModel() }
            };

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Create(NeedCreatorViewModel model)
        {
            if (model.NeedList == null)
            {
                model.NeedList = new List<NeedViewModel> { new NeedViewModel() };
            }

            if (!model.Latitude.HasValue || !model.Longitude.HasValue)
            {
                ModelState.AddModelError("", "Du må velge et punkt på kartet.");
                return View("Index", model);
            }

            var coords = new Coordinates(model.Latitude.Value, model.Longitude.Value);
            if (!coords.IsValid())
            {
                ModelState.AddModelError("", "De oppgitte koordinatene er ugyldige.");
                return View("Index", model);
            }

            foreach (var item in model.NeedList)
            {
                item.Latitude = model.Latitude;
                item.Longitude = model.Longitude;
                item.Importance = model.Importance;
                item.Deadline = model.Deadline;
                item.IsFulfilled = model.IsFulfilled;
            }

            for (int i = 0; i < model.NeedList.Count; i++)
            {
                ModelState.Remove($"NeedList[{i}].Deadline");
                ModelState.Remove($"NeedList[{i}].Latitude");
                ModelState.Remove($"NeedList[{i}].Longitude");
            }

            if (!model.NeedList.Any(n => !string.IsNullOrWhiteSpace(n.Name)))
            {
                ModelState.AddModelError("", "Du må fylle ut minst ett behov.");
                return View("Index", model);
            }

            if (!ModelState.IsValid)
            {
                return View("Index", model);
            }

            foreach (var item in model.NeedList)
            {
                if (!string.IsNullOrWhiteSpace(item.Name))
                {
                    _needDatabase[item.Name] = item;
                }
            }

            return View("Create", model);
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

            var coords = new Coordinates(model.Latitude.Value, model.Longitude.Value);

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