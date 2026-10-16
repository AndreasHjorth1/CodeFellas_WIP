using Microsoft.AspNetCore.Mvc;
using Prosjekt.DataAccess.Repositories;
using Prosjekt.Models.Entities;
using Prosjekt.Models.ModelView;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Prosjekt.Controllers
{
    /// <summary>
    /// ASP.NET MVC controller for creating, listing, editing, and deleting Need entities, coordinating model
    /// validation, coordinate validation, and persistence via an INeedRepository.
    /// </summary>
    /// <remarks>Exposes Index (GET) to show the creation UI, Create (POST) to validate input (including map
    /// coordinates) and create one or more Need records, Overview (GET) to list needs, Edit (GET/POST) to load and
    /// update a need with validation, and Delete (POST) to remove a need. Create, Edit, and Delete use anti-forgery
    /// validation. Maps between NeedViewModel and the domain Need, performs coordinate validation, and calls
    /// asynchronous repository methods for data operations.</remarks>
    public class NeedController : Controller
    {
        private readonly INeedRepository _repo;

        public NeedController(INeedRepository repo)
        {
            _repo = repo;
        }

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
        public async Task<IActionResult> Create(NeedCreatorViewModel model)
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

            foreach (var baseNeed in model.NeedList)
            {
                if (!string.IsNullOrWhiteSpace(baseNeed.Name))
                {
                    var needEntity = new Need
                    {
                        Name = baseNeed.Name,
                        Description = baseNeed.Description ?? string.Empty,
                        Category = baseNeed.Type.ToString(),
                        PhoneNumber = baseNeed.Phone ?? string.Empty,
                        Priority = baseNeed.Importance.ToString(),
                        NeededBy = baseNeed.Deadline ?? model.Deadline,
                        IsFulfilled = baseNeed.IsFulfilled,
                        Coords = new Coordinates(model.Latitude.Value, model.Longitude.Value),
                        CreatedAt = DateTime.UtcNow
                    };

                    await _repo.AddAsync(needEntity);
                }
            }

            return View("Create", model);
        }

        [HttpGet]
        public async Task<IActionResult> Overview()
        {
            var needEntities = await _repo.GetAllAsync();

            var needViewModels = needEntities.Select(n => new NeedViewModel
            {
                Id = n.Id,
                Name = n.Name,
                Description = n.Description,
                Phone = n.PhoneNumber,
                Type = Enum.TryParse<NeedViewModel.NeedType>(n.Category, out var cat) ? cat : NeedViewModel.NeedType.Other,
                Importance = Enum.TryParse<ImportanceLevel>(n.Priority, out var prio) ? prio : default,
                Deadline = n.NeededBy,
                IsFulfilled = n.IsFulfilled,
                Latitude = n.Coords.Latitude,
                Longitude = n.Coords.Longitude
            }).ToList();

            return View(needViewModels);
        }

        [HttpGet]
        public async Task<IActionResult> Edit(int id)
        {
            var need = await _repo.GetByIdAsync(id);
            if (need == null)
            {
                return NotFound();
            }

            var viewModel = new NeedViewModel
            {
                Id = need.Id,
                Name = need.Name,
                Description = need.Description,
                Phone = need.PhoneNumber,
                Type = Enum.TryParse<NeedViewModel.NeedType>(need.Category, out var cat) ? cat : NeedViewModel.NeedType.Other,
                Importance = Enum.TryParse<ImportanceLevel>(need.Priority, out var prio) ? prio : default,
                Deadline = need.NeededBy,
                IsFulfilled = need.IsFulfilled,
                Latitude = need.Coords.Latitude,
                Longitude = need.Coords.Longitude
            };

            return View(viewModel);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, NeedViewModel model)
        {
            if (id != model.Id)
            {
                return BadRequest();
            }

            if (!model.Latitude.HasValue || !model.Longitude.HasValue)
            {
                ModelState.AddModelError("", "Du må velge et punkt på kartet.");
                return View(model);
            }

            var coords = new Coordinates(model.Latitude.Value, model.Longitude.Value);
            if (!coords.IsValid())
            {
                ModelState.AddModelError("", "De oppgitte koordinatene er ugyldige.");
                return View(model);
            }

            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var existingNeed = await _repo.GetByIdAsync(id);
            if (existingNeed == null)
            {
                return NotFound();
            }

            existingNeed.Name = model.Name;
            existingNeed.Description = model.Description;
            existingNeed.PhoneNumber = model.Phone;
            existingNeed.Category = model.Type.ToString();
            existingNeed.Priority = model.Importance.ToString();
            existingNeed.NeededBy = model.Deadline;
            existingNeed.IsFulfilled = model.IsFulfilled;
            existingNeed.Coords = new Coordinates(model.Latitude.Value, model.Longitude.Value);

            await _repo.UpdateAsync(existingNeed);

            return RedirectToAction(nameof(Overview));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            await _repo.DeleteAsync(id);
            return RedirectToAction(nameof(Overview));
        }
    }
}