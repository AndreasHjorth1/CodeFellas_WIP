using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Prosjekt.DataAccess.Repositories;
using Prosjekt.Models.Authorization;
using Prosjekt.Models.Entities;
using Prosjekt.Models.Enums;
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
        private readonly IUserRepository _userRepo;
        private readonly INeedRepository _needRepo;
        private readonly INotificationRepository _notificationRepo;

        public ResourceController(IResourceRepository repo, IAllocationRepository allocationRepo, IUserRepository userRepo,
            INeedRepository needRepo, INotificationRepository notificationRepo)
        {
            _repo = repo;
            _allocationRepo = allocationRepo;
            _userRepo = userRepo;
            _needRepo = needRepo;
            _notificationRepo = notificationRepo;
        }

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

            var resourcesToCreate = model.ResourceList.Where(r => !string.IsNullOrWhiteSpace(r.Name)).ToList();

            /// <summary>
            /// Sjekkes før noe lagres, så en ugyldig rad ikke gir halvveis lagrede ressurser
            /// </summary>
            if (resourcesToCreate.Any(r => !r.AvailableFrom.HasValue || !r.AvailableTo.HasValue))
            {
                ModelState.AddModelError("", "Alle ressurser må ha tilgjengelig fra og til.");
                return View("Index", model);
            }

            var createdResources = new List<ResourceViewModel>();

            foreach (var baseResource in resourcesToCreate)
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
                    AvailableFrom = NorwegianTime.ToUtc(baseResource.AvailableFrom!.Value),
                    AvailableTo = NorwegianTime.ToUtc(baseResource.AvailableTo!.Value),
                    Coords = coords,
                    UserId = User.FindFirstValue(ClaimTypes.NameIdentifier)
                };

                await _repo.AddAsync(resourceEntity);

                createdResources.Add(baseResource with
                {
                    Id = resourceEntity.Id,
                    Latitude = model.Latitude,
                    Longitude = model.Longitude
                });
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

            /// <summary>
            /// Admin og koordinator har en egen side med tabell og kart, andre får sidene med sine egne ressurser
            /// </summary>
            var viewName = CanSeeAllResources() ? "AdminOverview" : "Overview";
            return View(viewName, resourceViewModels);
        }

        /// <summary>
        /// Admin og koordinator sine egne ressurser, vist med sidene som vanlige brukere får.
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
            var resources = resourceEntities.ToList();
            var allocations = (await _allocationRepo.GetAllAsync()).ToList();

            /// Eierens navn vises bare for admin og koordinator
            var ownerNames = CanSeeAllResources()
                ? await _userRepo.GetFullNamesAsync(resources.Select(r => r.UserId))
                : new Dictionary<string, string>();

            return resources.Select(r => new ResourceViewModel
            {
                Id = r.Id,
                UserId = r.UserId,
                OwnerName = r.UserId != null ? ownerNames.GetValueOrDefault(r.UserId) : null,
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
            if (resource == null)
            {
                return NotFound();
            }

            if (!CanEdit(resource))
            {
                return Forbid();
            }

            string description = resource.Description ?? "";
            string licensePlate = ExtractTag(ref description, "[Reg.nr:");
            string competenceTag = ExtractTag(ref description, "[Yrke:");

            var category = ParseCategory(resource.Category);
            var subCategory = resource.SubCategory?.Trim();

            var viewModel = new ResourceViewModel
            {
                Id = resource.Id,
                Name = resource.Name,
                Description = description,
                PhoneNumber = resource.PhoneNumber,
                Quantity = resource.Quantity,
                Allocated = await GetAllocatedAsync(resource.Id),
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
                Size = category == ResourceViewModel.ResourceType.Clothing ? subCategory : null,
                CompetenceType = category == ResourceViewModel.ResourceType.Competence
                    ? (string.IsNullOrEmpty(subCategory) ? competenceTag : subCategory)
                    : null
            };

            return View(viewModel);
        }

        /// <summary>
        /// Hvor mange av ressursen som er tildelt behov nå.
        /// </summary>
        private async Task<int> GetAllocatedAsync(int resourceId)
        {
            return (await _allocationRepo.GetAllAsync())
                .Where(a => a.ResourceId == resourceId)
                .Sum(a => a.Quantity);
        }

        /// <summary>
        /// Henter verdien i en merkelapp som «[Reg.nr: AB 12345]» og fjerner merkelappen fra beskrivelsen.
        /// Gir tom tekst hvis merkelappen ikke finnes.
        /// </summary>
        private static string ExtractTag(ref string description, string tag)
        {
            int start = description.IndexOf(tag, StringComparison.Ordinal);
            if (start < 0)
            {
                return "";
            }

            int valueStart = start + tag.Length;
            int end = description.IndexOf(']', valueStart);
            if (end <= valueStart)
            {
                return "";
            }

            var value = description.Substring(valueStart, end - valueStart).Trim();
            description = description.Remove(start, end - start + 1).Trim();
            return value;
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

            if (!model.AvailableFrom.HasValue || !model.AvailableTo.HasValue)
            {
                ModelState.AddModelError("", "Ressursen må ha tilgjengelig fra og til.");
                return View(model);
            }

            var resourceEntity = await _repo.GetByIdAsync(id);
            if (resourceEntity == null)
            {
                return NotFound();
            }

            if (!CanEdit(resourceEntity))
            {
                return Forbid();
            }

            /// Antallet kan ikke bli lavere enn det som allerede er tildelt behov
            var allocated = await GetAllocatedAsync(id);
            if (model.Quantity < allocated)
            {
                ModelState.AddModelError(nameof(model.Quantity),
                    $"{allocated} er tildelt behov. Antallet kan ikke være lavere før de er levert tilbake.");
                return View(model with { Allocated = allocated });
            }

            resourceEntity.Name = model.Name;
            resourceEntity.Description = GetFullDescription(model);
            resourceEntity.PhoneNumber = model.PhoneNumber;
            resourceEntity.Quantity = model.Quantity;
            /// Er alt tildelt, er ressursen ikke ledig, uansett hva som er krysset av
            resourceEntity.IsAvailable = model.Availability && model.Quantity - allocated > 0;
            resourceEntity.Category = model.Category.ToString();
            resourceEntity.SubCategory = GetSubCategory(model);
            resourceEntity.Coords = coords;
            resourceEntity.AvailableFrom = NorwegianTime.ToUtc(model.AvailableFrom.Value);
            resourceEntity.AvailableTo = NorwegianTime.ToUtc(model.AvailableTo.Value);

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
            if (resource == null)
            {
                return NotFound();
            }

            if (!CanDelete(resource)) 
            {
                return Forbid();
            }

            /// Må hentes før sletting, ellers vet vi ikke hvilke behov ressursen var tildelt
            var berorteBehov = (await _allocationRepo.GetAllAsync())
                .Where(a => a.ResourceId == id)
                .Select(a => a.NeedId)
                .Distinct()
                .ToList();

            await _repo.DeleteAsync(id);
            await _allocationRepo.DeleteByResourceAsync(id);

            await UpdateAffectedNeedsAsync(berorteBehov, resource.Name);

            return RedirectToAction(nameof(Overview));
        }

        /// <summary>
        /// Behov som mistet ressursen: står de uten tildelinger, går de tilbake til Vurderes,
        /// på samme måte som når en koordinator leverer tilbake den siste ressursen.
        /// Den som meldte behovet får beskjed.
        /// </summary>
        private async Task UpdateAffectedNeedsAsync(IEnumerable<int> needIds, string resourceName)
        {
            var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var remaining = (await _allocationRepo.GetAllAsync()).ToList();

            foreach (var needId in needIds)
            {
                var need = await _needRepo.GetByIdAsync(needId);
                if (need == null)
                {
                    continue;
                }

                var SetToUnderReview = need.Status == EntryStatus.Assigned && !remaining.Any(a => a.NeedId == needId);
                if (SetToUnderReview)
                {
                    need.Status = EntryStatus.UnderReview;
                    await _needRepo.UpdateAsync(need);
                }

                if (!string.IsNullOrEmpty(need.UserId) && need.UserId != currentUserId)
                {
                    var tekst = SetToUnderReview
                        ? $"Ressursen «{resourceName}» ble slettet. Behovet «{need.Name}» har ingen ressurser igjen og er satt til Vurderes."
                        : $"Ressursen «{resourceName}» ble slettet og er fjernet fra behovet «{need.Name}».";

                    await _notificationRepo.AddAsync(new Notification
                    {
                        UserId = need.UserId,
                        Message = tekst
                    });
                }
            }
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
                ResourceViewModel.ResourceType.Competence => model.CompetenceType,
                _ => null
            };

            return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
        }

        /// <summary>
        /// Skiltnummer og yrke lagres i beskrivelsen, fordi det ikke har egen kolonne.
        /// </summary>
        private static string GetFullDescription(ResourceViewModel model)
        {
            if (model.Category == ResourceViewModel.ResourceType.Vehicle && !string.IsNullOrWhiteSpace(model.LicensePlate))
            {
                return $"{model.Description} [Reg.nr: {model.LicensePlate.Trim()}]".Trim();
            }

            if (model.Category == ResourceViewModel.ResourceType.Competence && !string.IsNullOrWhiteSpace(model.CompetenceType))
            {
                return $"{model.Description} [Yrke: {model.CompetenceType.Trim()}]".Trim();
            }

            return model.Description ?? string.Empty;
        }
    }
}
