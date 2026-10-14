using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Prosjekt.DataAccess.Repositories;
using Prosjekt.Models.Entities;
using Prosjekt.Models.ModelView;
using Prosjekt.Utils;

namespace Prosjekt.Controllers
{
    public class ResourceController : Controller
    {
        private readonly IResourceRepository _repo;

        public ResourceController(IResourceRepository repo)
        {
            _repo = repo;
        }

        [HttpGet]
        public IActionResult Index()
        {
            return View(new ResourceCreatorViewModel());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(ResourceCreatorViewModel model)
        {
            if (model.Latitude.HasValue && model.Longitude.HasValue && model.ResourceList != null)
            {
                foreach (var item in model.ResourceList)
                {
                    item.Latitude = model.Latitude;
                    item.Longitude = model.Longitude;
                }

                foreach (var key in ModelState.Keys.Where(k => k.EndsWith(".Latitude") || k.EndsWith(".Longitude")).ToList())
                {
                    ModelState.Remove(key);
                }
            }

            if (!ModelState.IsValid)
            {
                return View("Index", model);
            }

            if (model.ResourceList == null || !model.ResourceList.Any())
            {
                ModelState.AddModelError("", "Du må legge til minst én ressurs i listen.");
                return View("Index", model);
            }

            var coords = new Coordinates(model.Latitude!.Value, model.Longitude!.Value);
            if (!coords.IsValid())
            {
                ModelState.AddModelError("", "De oppgitte koordinatene er ugyldige.");
                return View("Index", model);
            }

            for (int i = 0; i < model.ResourceList.Count; i++)
            {
                var baseResource = model.ResourceList[i];

                string extraInfo = "";
                switch (baseResource.Category)
                {
                    case ResourceViewModel.ResourceType.Vehicle:
                        string licensePlate = Request.Form[$"ResourceList[{i}].LicensePlate"].ToString();
                        if (!string.IsNullOrEmpty(licensePlate)) extraInfo = $"[Reg.nr: {licensePlate}]";
                        break;
                    case ResourceViewModel.ResourceType.Clothing:
                        string sizeStr = Request.Form[$"ResourceList[{i}].Size"].ToString();
                        if (!string.IsNullOrEmpty(sizeStr)) extraInfo = $"[Størrelse: {sizeStr}]";
                        break;
                }

                var resourceEntity = new Resource
                {
                    Name = baseResource.Name ?? "Ukjent ressurs",
                    Description = string.IsNullOrWhiteSpace(extraInfo)
                        ? baseResource.Description ?? ""
                        : $"{baseResource.Description} {extraInfo}".Trim(),
                    Category = baseResource.Category.ToString(),
                    IsAvailable = baseResource.Availability,
                    PhoneNumber = baseResource.PhoneNumber,
                    Quantity = baseResource.Quantity,
                    AvailableFrom = baseResource.AvailableFrom,
                    AvailableTo = baseResource.AvailableTo,
                    Coords = new Coordinates(model.Latitude.Value, model.Longitude.Value),
                    CreatedAt = NorwegianTime.Now,
                }
            ;

                await _repo.AddAsync(resourceEntity);
            }

            return RedirectToAction(nameof(Overview));
        }

        [HttpGet]
        public async Task<IActionResult> Overview()
        {
            var resourceEntities = await _repo.GetAllAsync();

            // Mapper fra Database-entitet til ViewModel for visning
            var resourceViewModels = resourceEntities.Select(r => new ResourceViewModel
            {
                Name = r.Name,
                Description = r.Description,
                PhoneNumber = r.PhoneNumber,
                Quantity = r.Quantity,
                Availability = r.IsAvailable,
                Latitude = r.Coords.Latitude,
                Longitude = r.Coords.Longitude,
                Category = Enum.TryParse<ResourceViewModel.ResourceType>(r.Category, out var cat) ? cat : ResourceViewModel.ResourceType.Other,
                AvailableFrom = r.AvailableFrom,
                AvailableTo = r.AvailableTo
            }).ToList();

            return View(resourceViewModels);
        }

        [HttpGet]
        public async Task<IActionResult> Edit(int id)
        {
            var resource = await _repo.GetByIdAsync(id);
            if (resource == null) return NotFound();

            return View(resource);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, Resource model)
        {
            if (id != model.Id) return BadRequest();

            if (ModelState.IsValid)
            {
                if (!model.Coords.IsValid())
                {
                    ModelState.AddModelError("", "Du må velge en gyldig posisjon.");
                    return View(model);
                }

                await _repo.UpdateAsync(model);
                return RedirectToAction(nameof(Overview));
            }
            return View(model);
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