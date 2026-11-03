using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Prosjekt.DataAccess.Repositories;
using Prosjekt.Models.Entities;
using Prosjekt.Models.ModelView;
using Prosjekt.Utils;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace Prosjekt.Controllers
{
    [Authorize]
    public class ResourceController : Controller
    {
        private readonly IResourceRepository _repo;
        private readonly IAllocationRepository _allocationRepo;

        public ResourceController(IResourceRepository repo, IAllocationRepository allocationRepo)
        {
            _repo = repo;
            _allocationRepo = allocationRepo;
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

            ResourceViewModel? createdResourceModel = null;

            for (int i = 0; i < model.ResourceList.Count; i++)
            {
                var baseResource = model.ResourceList[i];

                string extraInfo = "";
                string? subCategoryValue = null;

                switch (baseResource.Category)
                {
                    case ResourceViewModel.ResourceType.Vehicle:
                        string vehicleTypeStr = !string.IsNullOrWhiteSpace(baseResource.VehicleType)
                            ? baseResource.VehicleType
                            : Request.Form[$"ResourceList[{i}].VehicleType"].ToString();

                        string licensePlate = !string.IsNullOrWhiteSpace(baseResource.LicensePlate)
                            ? baseResource.LicensePlate
                            : Request.Form[$"ResourceList[{i}].LicensePlate"].ToString();

                        if (!string.IsNullOrWhiteSpace(vehicleTypeStr))
                        {
                            subCategoryValue = vehicleTypeStr;
                        }

                        if (!string.IsNullOrWhiteSpace(licensePlate))
                        {
                            extraInfo = $"[Reg.nr: {licensePlate}]";
                        }
                        break;

                    case ResourceViewModel.ResourceType.Tool:
                        string toolTypeStr = !string.IsNullOrWhiteSpace(baseResource.ToolType)
                            ? baseResource.ToolType
                            : Request.Form[$"ResourceList[{i}].ToolType"].ToString();

                        if (!string.IsNullOrWhiteSpace(toolTypeStr))
                        {
                            subCategoryValue = toolTypeStr;
                        }
                        break;

                    case ResourceViewModel.ResourceType.Clothing:
                        string sizeStr = !string.IsNullOrWhiteSpace(baseResource.Size)
                            ? baseResource.Size
                            : Request.Form[$"ResourceList[{i}].Size"].ToString();

                        if (!string.IsNullOrWhiteSpace(sizeStr))
                        {
                            subCategoryValue = sizeStr;
                        }
                        break;

                    case ResourceViewModel.ResourceType.Provision:
                        string provisionTypeStr = !string.IsNullOrWhiteSpace(baseResource.ProvisionType)
                            ? baseResource.ProvisionType
                            : Request.Form[$"ResourceList[{i}].ProvisionType"].ToString();

                        if (!string.IsNullOrWhiteSpace(provisionTypeStr))
                        {
                            subCategoryValue = provisionTypeStr;
                        }
                        break;

                    case ResourceViewModel.ResourceType.Materials:
                        string materialTypeStr = !string.IsNullOrWhiteSpace(baseResource.MaterialType)
                            ? baseResource.MaterialType
                            : Request.Form[$"ResourceList[{i}].MaterialType"].ToString();

                        if (!string.IsNullOrWhiteSpace(materialTypeStr))
                        {
                            subCategoryValue = materialTypeStr;
                        }
                        break;

                    default:
                        subCategoryValue = null;
                        break;
                }

                var resourceEntity = new Resource
                {
                    Name = baseResource.Name ?? "Unknown resource",
                    Description = string.IsNullOrWhiteSpace(extraInfo)
                        ? baseResource.Description ?? ""
                        : $"{baseResource.Description} {extraInfo}".Trim(),
                    Category = baseResource.Category.ToString(),
                    SubCategory = subCategoryValue,
                    IsAvailable = baseResource.Availability,
                    PhoneNumber = baseResource.PhoneNumber,
                    Quantity = baseResource.Quantity,
                    AvailableFrom = baseResource.AvailableFrom,
                    AvailableTo = baseResource.AvailableTo,
                    Coords = new Coordinates(model.Latitude.Value, model.Longitude.Value),
                    CreatedAt = NorwegianTime.Now
                };

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
            var allocations = (await _allocationRepo.GetAllAsync()).ToList();

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
                AvailableTo = r.AvailableTo,
                Allocated = allocations.Where(a => a.ResourceId == r.Id).Sum(a => a.Quantity),
            }).ToList();

            return View(resourceViewModels);
        }

        [HttpGet]
        [HttpGet]
        public async Task<IActionResult> Edit(int id)
        {
            var resource = await _repo.GetByIdAsync(id);
            if (resource == null) return NotFound();

            string description = resource.Description ?? "";
            string licensePlate = "";

            const string regTag = "[Reg.nr:";
            int regStart = description.IndexOf(regTag, StringComparison.Ordinal);
            if (regStart >= 0)
            {
                int valueStart = regStart + regTag.Length;
                int regEnd = description.IndexOf(']', valueStart);
                if (regEnd > valueStart)
                {
                    licensePlate = description.Substring(valueStart, regEnd - valueStart).Trim();
                    description = description.Remove(regStart, regEnd - regStart + 1).Trim();
                }
            }

            var category = Enum.TryParse<ResourceViewModel.ResourceType>(resource.Category, out var cat)
                ? cat
                : ResourceViewModel.ResourceType.Other;

            var subCategory = resource.SubCategory?.Trim();

            var viewModel = new ResourceViewModel
            {
                Id = resource.Id,
                Name = resource.Name,
                Description = description,
                PhoneNumber = resource.PhoneNumber,
                Quantity = resource.Quantity,
                Availability = resource.IsAvailable,
                Latitude = resource.Coords.Latitude,
                Longitude = resource.Coords.Longitude,
                Category = category,
                AvailableFrom = resource.AvailableFrom,
                AvailableTo = resource.AvailableTo,
                LicensePlate = licensePlate,

                VehicleType = category == ResourceViewModel.ResourceType.Vehicle ? subCategory : null,
                ToolType = category == ResourceViewModel.ResourceType.Tool ? subCategory : null,
                MaterialType = category == ResourceViewModel.ResourceType.Materials ? subCategory : null,
                ProvisionType = category == ResourceViewModel.ResourceType.Provision ? subCategory : null,
                Size = category == ResourceViewModel.ResourceType.Clothing ? subCategory : null
            };

            return View(viewModel);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, ResourceViewModel model)
        {
            if (id != model.Id) return BadRequest();

            ModelState.Remove(nameof(model.VehicleType));
            ModelState.Remove(nameof(model.LicensePlate));
            ModelState.Remove(nameof(model.ToolType));
            ModelState.Remove(nameof(model.MaterialType));
            ModelState.Remove(nameof(model.ProvisionType));
            ModelState.Remove(nameof(model.Size));

            if (!model.Latitude.HasValue || !model.Longitude.HasValue)
            {
                ModelState.AddModelError("", "Du må velge en posisjon i kartet.");
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

            var resourceEntity = await _repo.GetByIdAsync(id);
            if (resourceEntity == null) return NotFound();

            string extraInfo = "";
            string? subCategoryValue = null;

            switch (model.Category)
            {
                case ResourceViewModel.ResourceType.Vehicle:
                    if (!string.IsNullOrWhiteSpace(model.VehicleType))
                        subCategoryValue = model.VehicleType;
                    if (!string.IsNullOrWhiteSpace(model.LicensePlate))
                        extraInfo = $"[Reg.nr: {model.LicensePlate.Trim()}]";
                    break;

                case ResourceViewModel.ResourceType.Tool:
                    if (!string.IsNullOrWhiteSpace(model.ToolType))
                        subCategoryValue = model.ToolType;
                    break;

                case ResourceViewModel.ResourceType.Clothing:
                    if (!string.IsNullOrWhiteSpace(model.Size))
                        subCategoryValue = model.Size;
                    break;

                case ResourceViewModel.ResourceType.Provision:
                    if (!string.IsNullOrWhiteSpace(model.ProvisionType))
                        subCategoryValue = model.ProvisionType;
                    break;

                case ResourceViewModel.ResourceType.Materials:
                    if (!string.IsNullOrWhiteSpace(model.MaterialType))
                        subCategoryValue = model.MaterialType;
                    break;
            }

            string fullDescription = string.IsNullOrWhiteSpace(extraInfo)
                ? model.Description ?? ""
                : $"{model.Description} {extraInfo}".Trim();

            resourceEntity.Name = model.Name;
            resourceEntity.Description = fullDescription;
            resourceEntity.PhoneNumber = model.PhoneNumber;
            resourceEntity.Quantity = model.Quantity;
            resourceEntity.IsAvailable = model.Availability;
            resourceEntity.Category = model.Category.ToString();
            resourceEntity.SubCategory = subCategoryValue?.Trim();
            resourceEntity.Coords = coords;
            resourceEntity.AvailableFrom = model.AvailableFrom;
            resourceEntity.AvailableTo = model.AvailableTo;

            await _repo.UpdateAsync(resourceEntity);
            return RedirectToAction(nameof(Overview));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            await _repo.DeleteAsync(id);
            await _allocationRepo.DeleteByResourceAsync(id);
            return RedirectToAction(nameof(Overview));
        }

        [HttpGet]
        public async Task<IActionResult> GetInformasjon()
        {
            var ressurser = await _repo.GetAllAsync();

            var result = ressurser.Select(r => new ResourceViewModel
            {
                Id = r.Id,
                Name = r.Name,
                PhoneNumber = r.PhoneNumber,
                Description = r.Description,
                Quantity = r.Quantity,
                Availability = r.IsAvailable,
                Category = Enum.TryParse<ResourceViewModel.ResourceType>(r.Category, out var cat)
                    ? cat
                    : ResourceViewModel.ResourceType.Other,
                Latitude = r.Coords.Latitude,
                Longitude = r.Coords.Longitude
            });

            return Json(result);
        }
    }
}