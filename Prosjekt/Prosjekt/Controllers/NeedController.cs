using Microsoft.AspNetCore.Mvc;
using Prosjekt.DataAccess.Repositories;
using Prosjekt.Models.Entities;
using Prosjekt.Models.ModelView;
using Prosjekt.Models.Authorization;
using Prosjekt.Models.Enums;
using Microsoft.AspNetCore.Authorization;


namespace Prosjekt.Controllers
{
    [Authorize(Roles = RoleNames.Admin)]
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
                        Priority = model.Importance.ToString(),
                        NeededBy = baseNeed.Deadline ?? model.Deadline,
                        Status = EntryStatus.New,
                        Coords = new Coordinates(model.Latitude.Value, model.Longitude.Value),
                        AreaGeoJson = model.AreaGeoJson,
                        AreaSquareMeters = model.AreaSquareMeters,
                        PerimeterMeters = model.PerimeterMeters,

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
                Status = n.Status,
                Latitude = n.Coords.Latitude,
                Longitude = n.Coords.Longitude,
                AreaGeoJson = n.AreaGeoJson,
                AreaSquareMeters = n.AreaSquareMeters,
                PerimeterMeters = n.PerimeterMeters
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
                Status = need.Status,
                Latitude = need.Coords.Latitude,
                Longitude = need.Coords.Longitude,
                AreaGeoJson = need.AreaGeoJson,
                AreaSquareMeters = need.AreaSquareMeters,
                PerimeterMeters = need.PerimeterMeters
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

            // Sjekk statusflyten FØR vi endrer noe på behovet
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
            existingNeed.NeededBy = model.Deadline;
            existingNeed.Status = model.Status;
            existingNeed.Coords = coords;
            existingNeed.AreaGeoJson = model.AreaGeoJson;
            existingNeed.AreaSquareMeters = model.AreaSquareMeters;
            existingNeed.PerimeterMeters = model.PerimeterMeters;

            await _repo.UpdateAsync(existingNeed);

            return RedirectToAction(nameof(Overview));
        }



        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            await _repo.DeleteAsync(id);
            await _allocationRepo.DeleteByNeedAsync(id);
            return RedirectToAction(nameof(Overview));
        }

        [AllowAnonymous]
        [HttpGet]
        public async Task<IActionResult> GetInformation()
        {
            if (User.Identity?.IsAuthenticated != true) return Challenge();

            var isAdmin = User.IsInRole(RoleNames.Admin);
            var needs = await _repo.GetAllAsync();

            var result = needs
                .Where(n => isAdmin || n.Status != EntryStatus.Resolved)
                .Select(n => new NeedViewModel
                {
                    Id = n.Id,
                    Name = n.Name,
                    Phone = isAdmin ? n.PhoneNumber : string.Empty,
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
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Allocate(int needId, int resourceId, int quantity)
        {
            var need = await _repo.GetByIdAsync(needId);
            var resource = await _resourceRepo.GetByIdAsync(resourceId);
            if (need == null || resource == null) return NotFound();

            // Hvor mye av ressursen er allerede tildelt andre behov?
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

            // Behovet går fra Ny/Vurderes til Tildelt
            if (need.Status == EntryStatus.New || need.Status == EntryStatus.UnderReview)
            {
                need.Status = EntryStatus.Assigned;
                await _repo.UpdateAsync(need);
            }

            return RedirectToAction(nameof(Match), new { id = needId });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Deallocate(int allocationId, int needId)
        {
            await _allocationRepo.DeleteAsync(allocationId);

            // Står behovet uten tildelinger, går det tilbake til Vurderes
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

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Resolve(int id)
        {
            var need = await _repo.GetByIdAsync(id);
            if (need == null) return NotFound();

            if (EntryStatusFlow.CanMove(need.Status, EntryStatus.Resolved))
            {
                need.Status = EntryStatus.Resolved;
                await _repo.UpdateAsync(need);
            }

            return RedirectToAction(nameof(Overview), new { id });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Unresolve(int id)
        {
            var need = await _repo.GetByIdAsync(id);
            if (need == null) return NotFound();

            if (need.Status != EntryStatus.Resolved) return RedirectToAction(nameof(Overview));

            // Tilbake til Tildelt hvis noe er tildelt, ellers Vurderes
            var allocations = await _allocationRepo.GetByNeedAsync(id);
            need.Status = allocations.Any() ? EntryStatus.Assigned : EntryStatus.UnderReview;

            await _repo.UpdateAsync(need);
            return RedirectToAction(nameof(Overview));
        }
    }
}