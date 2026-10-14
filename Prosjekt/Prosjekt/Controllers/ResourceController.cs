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
        private static int Next_Id = 0;
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
                ModelState.AddModelError("", "You must add at least one resource to the list.");
                return View("Index", model);
            }

            var coords = new Coordinates(model.Latitude!.Value, model.Longitude!.Value);
            if (!coords.IsValid())
            {
                ModelState.AddModelError("", "The provided coordinates are invalid.");
                return View("Index", model);
            }

            ResourceViewModel createdResourceModel = null;

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
                        if (!string.IsNullOrEmpty(sizeStr)) extraInfo = $"[Size: {sizeStr}]";
                        break;
                }

                var resourceEntity = new Resource
                {
                    Name = baseResource.Name ?? "Unknown resource",
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
                };

                resourceEntity.Id = Next_Id++;

                await _repo.AddAsync(resourceEntity);

                if (i == 0)
                {
                    createdResourceModel = baseResource;
                    createdResourceModel.Id = resourceEntity.Id;
                }
            }

            return View("Create", createdResourceModel);
        }

        [HttpGet]
        public async Task<IActionResult> Overview()
        {
            var resourceEntities = await _repo.GetAllAsync();

            // Mapper fra Database-entitet til ViewModel for visning
            var resourceViewModels = resourceEntities.Select(r => new ResourceViewModel
            {
                Id = r.Id,
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

            // Map fra database-entiteten (Resource) til ResourceViewModel
            var viewModel = new ResourceViewModel
            {
                Id = resource.Id,
                Name = resource.Name,
                Description = resource.Description,
                PhoneNumber = resource.PhoneNumber,
                Quantity = resource.Quantity,
                Availability = resource.IsAvailable,
                Latitude = resource.Coords.Latitude,
                Longitude = resource.Coords.Longitude,
                Category = Enum.TryParse<ResourceViewModel.ResourceType>(resource.Category, out var cat)
                    ? cat
                    : ResourceViewModel.ResourceType.Other,
                AvailableFrom = resource.AvailableFrom,
                AvailableTo = resource.AvailableTo
            };

            return View(viewModel);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, ResourceViewModel model)
        {
            if (id != model.Id) return BadRequest();

            // Sjekker at koordinatene faktisk er oppgitt
            if (!model.Latitude.HasValue || !model.Longitude.HasValue)
            {
                ModelState.AddModelError("", "Du må velge en posisjon i kartet.");
                return View(model);
            }

            // Bruker .Value for å konvertere fra 'double?' til 'double'
            var coords = new Coordinates(model.Latitude.Value, model.Longitude.Value);
            if (!coords.IsValid())
            {
                ModelState.AddModelError("", "De oppgitte koordinatene er ugyldige.");
                return View(model);
            }

            if (ModelState.IsValid)
            {
                var resourceEntity = await _repo.GetByIdAsync(id);
                if (resourceEntity == null) return NotFound();

                resourceEntity.Name = model.Name;
                resourceEntity.Description = model.Description;
                resourceEntity.PhoneNumber = model.PhoneNumber;
                resourceEntity.Quantity = model.Quantity;
                resourceEntity.IsAvailable = model.Availability;
                resourceEntity.Category = model.Category.ToString();
                resourceEntity.Coords = coords;
                resourceEntity.AvailableFrom = model.AvailableFrom;
                resourceEntity.AvailableTo = model.AvailableTo;

                await _repo.UpdateAsync(resourceEntity);
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