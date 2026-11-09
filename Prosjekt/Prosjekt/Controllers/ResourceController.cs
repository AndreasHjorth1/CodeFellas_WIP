using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Prosjekt.DataAccess.Repositories;
using Prosjekt.Models.Authorization;
using Prosjekt.Models.Entities;
using Prosjekt.Models.ModelView;
using Prosjekt.Utils;
using System.Security.Claims;
using NetTopologySuite.Features;

namespace Prosjekt.Controllers
{
    /// <summary>
    /// Registrering, oversikt, endring, sletting og eksport av ressurser. Krever innlogging.
    /// </summary>
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

        /// <summary>
        /// Admin og koordinator ser og kan endre alle ressurser.
        /// </summary>
        private bool CanSeeAllResources()
        {
            return User.IsInRole(RoleNames.Admin) || User.IsInRole(RoleNames.Coordinator);
        }

        /// <summary>
        /// Admin, koordinator og eieren kan endre ressursen.
        /// </summary>
        private bool CanEdit(Resource resource)
        {
            return CanSeeAllResources()
                || resource.UserId == User.FindFirstValue(ClaimTypes.NameIdentifier);
        }

        /// <summary>
        /// Admin kan slette alle ressurser, andre (også koordinator) bare sine egne.
        /// </summary>
        private bool CanDelete(Resource resource)
        {
            if (User.IsInRole(RoleNames.Admin)) return true;

            return resource.UserId == User.FindFirstValue(ClaimTypes.NameIdentifier);
        }

        [HttpGet]
        public IActionResult Index()
        {
            return View(new ResourceCreatorViewModel());
        }

        /// <summary>
        /// Oppretter alle ressursene i listen, med samme posisjon fra kartet.
        /// Skiltnummer lagres i beskrivelsen, fordi det ikke har egen kolonne.
        /// Bekreftelsessiden viser bare den første ressursen.
        /// </summary>
        [HttpPost]
        public async Task<IActionResult> Create(ResourceCreatorViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View("Index", model);
            }

            if (model.ResourceList == null || !model.ResourceList.Any(r => !string.IsNullOrWhiteSpace(r.Name)))
            {
                ModelState.AddModelError("", "Du må legge til minst én ressurs.");
                return View("Index", model);
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

            var createdResources = new List<ResourceViewModel>();

            foreach (var baseResource in model.ResourceList.Where(r => !string.IsNullOrWhiteSpace(r.Name)))
            {
                var resourceEntity = new Resource
                {
                    Name = baseResource.Name!,
                    Description = GetFullDescription(baseResource),
                    Category = baseResource.Category.ToString(),
                    SubCategory = GetSubCategory(baseResource),
                    IsAvailable = baseResource.Availability,
                    PhoneNumber = baseResource.PhoneNumber,
                    Quantity = baseResource.Quantity,
                    AvailableFrom = NorwegianTime.ToUtc(baseResource.AvailableFrom),
                    AvailableTo = NorwegianTime.ToUtc(baseResource.AvailableTo),
                    Coords = coords,
                    UserId = User.FindFirstValue(ClaimTypes.NameIdentifier)
                };

                await _repo.AddAsync(resourceEntity);

                baseResource.Id = resourceEntity.Id;
                baseResource.Latitude = model.Latitude;
                baseResource.Longitude = model.Longitude;
                createdResources.Add(baseResource);
            }

            return View("Create", createdResources);
        }

        /// <summary>
        /// Admin og koordinator ser alle ressurser, andre bare sine egne.
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> Overview()
        {
            var resourceViewModels = await ToOverviewViewModelsAsync(await GetVisibleResourcesAsync());

            /// Admin og koordinator har en egen side med tabell og kart, andre får kortene med sine egne ressurser
            var viewName = CanSeeAllResources() ? "AdminOverview" : "Overview";
            return View(viewName, resourceViewModels);
        }

        /// <summary>
        /// Admin og koordinator sine egne ressurser, vist med kortene som vanlige brukere får.
        /// </summary>
        [HttpGet]
        [Authorize(Roles = RoleNames.AdminOrCoordinator)]
        public async Task<IActionResult> MyResources()
        {
            var ownResources = await _repo.GetByUserIdAsync(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
            return View("Overview", await ToOverviewViewModelsAsync(ownResources));
        }

        private async Task<List<ResourceViewModel>> ToOverviewViewModelsAsync(IEnumerable<Resource> resourceEntities)
        {
            var allocations = (await _allocationRepo.GetAllAsync()).ToList();

            return resourceEntities.Select(r => new ResourceViewModel
            {
                Id = r.Id,
                UserId = r.UserId,
                Name = r.Name,
                Description = r.Description,
                PhoneNumber = r.PhoneNumber,
                Quantity = r.Quantity,
                Availability = r.IsAvailable,
                Latitude = r.Coords.Latitude,
                Longitude = r.Coords.Longitude,
                Category = ParseCategory(r.Category),
                AvailableFrom = NorwegianTime.FromUtc(r.AvailableFrom),
                AvailableTo = NorwegianTime.FromUtc(r.AvailableTo),
                Allocated = allocations.Where(a => a.ResourceId == r.Id).Sum(a => a.Quantity),
            }).ToList();
        }

        /// <summary>
        /// Viser redigeringsskjemaet. Skiltnummeret skilles ut fra beskrivelsen og vises i eget felt.
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> Edit(int id)
        {
            var resource = await _repo.GetByIdAsync(id);
            if (resource == null) return NotFound();
            if (!CanEdit(resource)) return Forbid();

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

            var category = ParseCategory(resource.Category);
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
                AvailableFrom = NorwegianTime.FromUtc(resource.AvailableFrom),
                AvailableTo = NorwegianTime.FromUtc(resource.AvailableTo),
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
        public async Task<IActionResult> Edit(int id, ResourceViewModel model)
        {
            if (id != model.Id) return BadRequest();

            if (!ModelState.IsValid)
            {
                return View(model);
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

            var resourceEntity = await _repo.GetByIdAsync(id);
            if (resourceEntity == null) return NotFound();
            if (!CanEdit(resourceEntity)) return Forbid();

            resourceEntity.Name = model.Name;
            resourceEntity.Description = GetFullDescription(model);
            resourceEntity.PhoneNumber = model.PhoneNumber;
            resourceEntity.Quantity = model.Quantity;
            resourceEntity.IsAvailable = model.Availability;
            resourceEntity.Category = model.Category.ToString();
            resourceEntity.SubCategory = GetSubCategory(model);
            resourceEntity.Coords = coords;
            resourceEntity.AvailableFrom = NorwegianTime.ToUtc(model.AvailableFrom);
            resourceEntity.AvailableTo = NorwegianTime.ToUtc(model.AvailableTo);

            await _repo.UpdateAsync(resourceEntity);
            return RedirectToAction(nameof(Overview));
        }

        /// <summary>
        /// Sletter ressursen og tildelingene, siden de ikke slettes automatisk.
        /// </summary>
        [HttpPost]
        public async Task<IActionResult> Delete(int id)
        {
            var resource = await _repo.GetByIdAsync(id);
            if (resource == null) return NotFound();
            if (!CanDelete(resource)) return Forbid();

            await _repo.DeleteAsync(id);
            await _allocationRepo.DeleteByResourceAsync(id);
            return RedirectToAction(nameof(Overview));
        }

        /// <summary>
        /// Data til kartet på Home/Index. Admin og koordinator får alle ressurser, andre bare sine egne.
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> GetInformation()
        {
            var resources = await GetVisibleResourcesAsync();

            var collection = new FeatureCollection();
            foreach (var r in resources)
            {
                collection.Add(new Feature(GeoJson.Point(r.Coords.Latitude, r.Coords.Longitude), new AttributesTable
                {
                    { "id", r.Id },
                    { "name", r.Name },
                    { "phoneNumber", r.PhoneNumber },
                    { "description", r.Description },
                    { "quantity", r.Quantity },
                    { "availability", r.IsAvailable },
                    { "category", ParseCategory(r.Category) }
                }));
            }

            return Json(collection);
        }

        /// <summary>
        /// Admin og koordinator får alle ressurser, andre bare sine egne.
        /// </summary>
        private Task<IEnumerable<Resource>> GetVisibleResourcesAsync()
        {
            return CanSeeAllResources()
                ? _repo.GetAllAsync()
                : _repo.GetByUserIdAsync(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        }

        private static ResourceViewModel.ResourceType ParseCategory(string category)
        {
            return Enum.TryParse<ResourceViewModel.ResourceType>(category, out var parsed)
                ? parsed
                : ResourceViewModel.ResourceType.Other;
        }

        /// <summary>
        /// Underkategorien ligger i ulike felt avhengig av kategorien.
        /// </summary>
        private static string? GetSubCategory(ResourceViewModel model)
        {
            var value = model.Category switch
            {
                ResourceViewModel.ResourceType.Vehicle => model.VehicleType,
                ResourceViewModel.ResourceType.Tool => model.ToolType,
                ResourceViewModel.ResourceType.Clothing => model.Size,
                ResourceViewModel.ResourceType.Provision => model.ProvisionType,
                ResourceViewModel.ResourceType.Materials => model.MaterialType,
                _ => null
            };

            return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
        }

        /// <summary>
        /// Skiltnummer lagres i beskrivelsen, fordi det ikke har egen kolonne.
        /// </summary>
        private static string GetFullDescription(ResourceViewModel model)
        {
            return model.Category == ResourceViewModel.ResourceType.Vehicle && !string.IsNullOrWhiteSpace(model.LicensePlate)
                ? $"{model.Description} [Reg.nr: {model.LicensePlate.Trim()}]".Trim()
                : model.Description ?? "";
        }
    }
}
