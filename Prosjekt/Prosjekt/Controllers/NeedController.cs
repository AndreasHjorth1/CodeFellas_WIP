using Microsoft.AspNetCore.Mvc;
using Prosjekt.DataAccess.Repositories;
using Prosjekt.Models.Entities;
using Prosjekt.Models.ModelView;
using Prosjekt.Models.Authorization;
using Prosjekt.Models.Enums;
using Microsoft.AspNetCore.Authorization;
using Prosjekt.Utils;
using System.Security.Claims;

namespace Prosjekt.Controllers
{
    /// <summary>
    /// Registrering, oversikt, endring og tildeling av behov. Bare for admin og koordinator, unntatt GetInformation.
    /// </summary>
    [Authorize(Roles = RoleNames.AdminOrCoordinator)]
    public class NeedController : Controller
    {
        private readonly INeedRepository _repo;
        private readonly IResourceRepository _resourceRepo;
        private readonly IAllocationRepository _allocationRepo;

        public NeedController(INeedRepository repo, IResourceRepository resourceRepo, IAllocationRepository allocationRepo)
        {
            _repo = repo;
            _resourceRepo = resourceRepo;
            _allocationRepo = allocationRepo;
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

        /// <summary>
        /// Oppretter alle behovene i listen. Posisjon, viktighet og frist er felles, og tomme rader hoppes over.
        /// </summary>
        [HttpPost]
        public async Task<IActionResult> Create(NeedCreatorViewModel model)
        {
            model.NeedList ??= new List<NeedViewModel> { new NeedViewModel() };

            /// <summary>
            /// Radene sender ikke egen tidsfrist, bare den felles fristen.
            /// Derfor fjerner vi valideringen på Deadline for hver rad, ellers vil det alltid feile.
            /// Dette må skje før ModelState.IsValid sjekkes.
            /// </summary>

            for (int i = 0; i < model.NeedList.Count; i++)
            {
                ModelState.Remove($"NeedList[{i}].Deadline");
            }

            if (!ModelState.IsValid)
            {
                return View("Index", model);
            }

            if (!model.NeedList.Any(n => !string.IsNullOrWhiteSpace(n.Name)))
            {
                ModelState.AddModelError("", "Du må fylle ut minst ett behov.");
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

            foreach (var baseNeed in model.NeedList.Where(n => !string.IsNullOrWhiteSpace(n.Name)))
            {
                var needEntity = new Need
                {
                    Name = baseNeed.Name,
                    Description = baseNeed.Description ?? string.Empty,
                    Category = baseNeed.Type.ToString(),
                    PhoneNumber = baseNeed.Phone ?? string.Empty,
                    Priority = model.Importance.ToString(),
                    NeededBy = NorwegianTime.ToUtc(baseNeed.Deadline ?? model.Deadline),
                    Coords = coords,
                    AreaGeoJson = model.AreaGeoJson,
                    AreaSquareMeters = model.AreaSquareMeters,
                    PerimeterMeters = model.PerimeterMeters,
                    UserId = User.FindFirstValue(ClaimTypes.NameIdentifier)
                };

                await _repo.AddAsync(needEntity);
            }

            return View("Create", model);
        }

        [HttpGet]
        public async Task<IActionResult> Overview()
        {
            var needEntities = await _repo.GetAllAsync();
            return View(needEntities.Select(ToViewModel).ToList());
        }

        [HttpGet]
        public async Task<IActionResult> Edit(int id)
        {
            var need = await _repo.GetByIdAsync(id);
            if (need == null) return NotFound();

            return View(ToViewModel(need));
        }

        [HttpPost]
        public async Task<IActionResult> Edit(int id, NeedViewModel model)
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

            var existingNeed = await _repo.GetByIdAsync(id);
            if (existingNeed == null) return NotFound();

            if (!EntryStatusFlow.CanMove(existingNeed.Status, model.Status))
            {
                ModelState.AddModelError(nameof(model.Status),
                    "Status kan bare flyttes ett steg om gangen.");
                return View(model);
            }

            existingNeed.Name = model.Name;
            existingNeed.Description = model.Description;
            existingNeed.PhoneNumber = model.Phone;
            existingNeed.Category = model.Type.ToString();
            existingNeed.Priority = model.Importance.ToString();
            existingNeed.NeededBy = NorwegianTime.ToUtc(model.Deadline);
            existingNeed.Status = model.Status;
            existingNeed.Coords = coords;
            existingNeed.AreaGeoJson = model.AreaGeoJson;
            existingNeed.AreaSquareMeters = model.AreaSquareMeters;
            existingNeed.PerimeterMeters = model.PerimeterMeters;

            await _repo.UpdateAsync(existingNeed);

            return RedirectToAction(nameof(Overview));
        }

        [HttpPost]
        public async Task<IActionResult> Delete(int id)
        {
            await _repo.DeleteAsync(id);
            await _allocationRepo.DeleteByNeedAsync(id);
            return RedirectToAction(nameof(Overview));
        }

        /// <summary>
        /// Data til kartet. [AllowAnonymous] opphever rollekravet, så alle innloggede kan se behovene.
        /// Den som ikke er logget inn, sendes til innlogging. Bare admin og koordinator får telefonnumrene.
        /// </summary>
        [AllowAnonymous]
        [HttpGet]
        public async Task<IActionResult> GetInformation()
        {
            if (User.Identity?.IsAuthenticated != true) return Challenge();

            var canSeePhone = User.IsInRole(RoleNames.Admin) || User.IsInRole(RoleNames.Coordinator);
            var needs = await _repo.GetAllAsync();

            var result = needs
                .Select(n => new NeedViewModel
                {
                    Id = n.Id,
                    Name = n.Name,
                    Phone = canSeePhone ? n.PhoneNumber : string.Empty,
                    Description = n.Description,
                    Type = Enum.TryParse<NeedViewModel.NeedType>(n.Category, out var cat)
                        ? cat
                        : NeedViewModel.NeedType.Other,
                    Importance = Enum.TryParse<ImportanceLevel>(n.Priority, out var prio)
                        ? prio
                        : ImportanceLevel.Planned,
                    Status = n.Status,
                    Latitude = n.Coords.Latitude,
                    Longitude = n.Coords.Longitude,
                    AreaGeoJson = n.AreaGeoJson,
                    AreaSquareMeters = n.AreaSquareMeters
                });

            return Json(result);
        }

        /// <summary>
        /// Finner tilgjengelige ressurser i riktig kategori, med den nærmeste først.
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> Match(int id)
        {
            var need = await _repo.GetByIdAsync(id);
            if (need == null) return NotFound();

            var needType = Enum.TryParse<NeedViewModel.NeedType>(need.Category, out var nt)
                ? nt : NeedViewModel.NeedType.Other;

            var wanted = CategoryMatching.MatchingResources(needType);

            var all = await _resourceRepo.GetAllAsync();
            var allocations = (await _allocationRepo.GetAllAsync()).ToList();

            var matches = all
                .Where(r => r.IsAvailable)
                .Where(r => Enum.TryParse<ResourceViewModel.ResourceType>(r.Category, out var rt)
                            && wanted.Contains(rt))
                .Select(r => new ResourceMatchViewModel
                {
                    Id = r.Id,
                    Name = r.Name,
                    Description = r.Description,
                    Category = r.Category,
                    Quantity = r.Quantity,
                    PhoneNumber = r.PhoneNumber,
                    Latitude = r.Coords.Latitude,
                    Longitude = r.Coords.Longitude,
                    DistanceMeters = Distance.InMeters(
                        need.Coords.Latitude, need.Coords.Longitude,
                        r.Coords.Latitude, r.Coords.Longitude),
                    Allocated = allocations.Where(a => a.ResourceId == r.Id).Sum(a => a.Quantity),
                    AllocatedToThisNeed = allocations.Where(a => a.ResourceId == r.Id && a.NeedId == id).Sum(a => a.Quantity)
                })
                .OrderBy(r => r.DistanceMeters)
                .ToList();

            var resourceNames = all.ToDictionary(r => r.Id, r => r.Name);

            ViewBag.Allocations = allocations
                .Where(a => a.NeedId == id)
                .Select(a => new AllocationViewModel
                {
                    Id = a.Id,
                    ResourceName = resourceNames.TryGetValue(a.ResourceId, out var n) ? n : "(ukjent ressurs)",
                    Quantity = a.Quantity
                })
                .ToList();

            ViewBag.NeedId = id;
            ViewBag.NeedName = need.Name;
            ViewBag.NeedLat = need.Coords.Latitude;
            ViewBag.NeedLng = need.Coords.Longitude;

            return View(matches);
        }

        [HttpPost]
        public async Task<IActionResult> Allocate(int needId, int resourceId, int quantity)
        {
            var need = await _repo.GetByIdAsync(needId);
            var resource = await _resourceRepo.GetByIdAsync(resourceId);
            if (need == null || resource == null) return NotFound();

            if (!resource.IsAvailable)
            {
                TempData["Error"] = $"{resource.Name} er ikke tilgjengelig.";
                return RedirectToAction(nameof(Match), new { id = needId });
            }

            /// <summary>
            /// Hvor mye av ressursen er allerede tildelt andre behov?
            /// </summary>
            var alle = await _allocationRepo.GetAllAsync();
            var taken = alle.Where(a => a.ResourceId == resourceId).Sum(a => a.Quantity);
            var available = resource.Quantity - taken;

            if (quantity < 1 || quantity > available)
            {
                TempData["Error"] = $"Kan ikke tildele {quantity} - bare {available} er ledig.";
                return RedirectToAction(nameof(Match), new { id = needId });
            }

            await _allocationRepo.AddAsync(new Allocation
            {
                NeedId = needId,
                ResourceId = resourceId,
                Quantity = quantity
            });

            if (need.Status == EntryStatus.New || need.Status == EntryStatus.UnderReview)
            {
                need.Status = EntryStatus.Assigned;
                await _repo.UpdateAsync(need);
            }

            return RedirectToAction(nameof(Match), new { id = needId });
        }

        [HttpPost]
        public async Task<IActionResult> Deallocate(int allocationId, int needId)
        {
            var allocations = await _allocationRepo.GetByNeedAsync(needId);
            if (!allocations.Any(a => a.Id == allocationId)) return NotFound();

            await _allocationRepo.DeleteAsync(allocationId);

            /// <summary>
            /// Står behovet uten tildelinger, går det tilbake til Vurderes
            /// </summary>
            var igjen = await _allocationRepo.GetByNeedAsync(needId);
            if (!igjen.Any())
            {
                var need = await _repo.GetByIdAsync(needId);
                if (need != null && need.Status == EntryStatus.Assigned)
                {
                    need.Status = EntryStatus.UnderReview;
                    await _repo.UpdateAsync(need);
                }
            }

            return RedirectToAction(nameof(Match), new { id = needId });
        }

        /// <summary>
        /// Setter behovet til Løst. Bare Tildelt kan gå videre dit; fra Ny og Vurderes skjer det ingenting.
        /// </summary>
        [HttpPost]
        public async Task<IActionResult> Resolve(int id)
        {
            var need = await _repo.GetByIdAsync(id);
            if (need == null) return NotFound();

            if (EntryStatusFlow.CanMove(need.Status, EntryStatus.Resolved))
            {
                need.Status = EntryStatus.Resolved;
                await _repo.UpdateAsync(need);
            }

            return RedirectToAction(nameof(Overview));
        }

        [HttpPost]
        public async Task<IActionResult> Unresolve(int id)
        {
            var need = await _repo.GetByIdAsync(id);
            if (need == null) return NotFound();

            if (need.Status != EntryStatus.Resolved) return RedirectToAction(nameof(Overview));

            var allocations = await _allocationRepo.GetByNeedAsync(id);
            need.Status = allocations.Any() ? EntryStatus.Assigned : EntryStatus.UnderReview;

            await _repo.UpdateAsync(need);
            return RedirectToAction(nameof(Overview));
        }

        private static NeedViewModel ToViewModel(Need n) => new()
        {
            Id = n.Id,
            Name = n.Name,
            Description = n.Description,
            Phone = n.PhoneNumber,
            Type = Enum.TryParse<NeedViewModel.NeedType>(n.Category, out var cat) ? cat : NeedViewModel.NeedType.Other,
            Importance = Enum.TryParse<ImportanceLevel>(n.Priority, out var prio) ? prio : default,
            Deadline = NorwegianTime.FromUtc(n.NeededBy),
            Status = n.Status,
            Latitude = n.Coords.Latitude,
            Longitude = n.Coords.Longitude,
            AreaGeoJson = n.AreaGeoJson,
            AreaSquareMeters = n.AreaSquareMeters,
            PerimeterMeters = n.PerimeterMeters
        };
    }
}
