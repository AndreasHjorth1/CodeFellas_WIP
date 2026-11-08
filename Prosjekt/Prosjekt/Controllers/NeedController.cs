using Microsoft.AspNetCore.Mvc;
using Prosjekt.DataAccess.Repositories;
using Prosjekt.Models.Entities;
using Prosjekt.Models.ModelView;
using Prosjekt.Models.Authorization;
using Prosjekt.Models.Enums;
using Microsoft.AspNetCore.Authorization;
using Prosjekt.Utils;
using System.Globalization;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using NetTopologySuite;
using NetTopologySuite.Features;
using NetTopologySuite.Geometries;
using NetTopologySuite.IO.Converters;
// NetTopologySuite har også en klasse som heter Coordinates. Her sier vi at det er vår egen som gjelder.
using Coordinates = Prosjekt.Models.Entities.Coordinates;


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
                    PerimeterMeters = model.PerimeterMeters
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

            var factory = NtsGeometryServices.Instance.CreateGeometryFactory(srid: 4326);
            var collection = new FeatureCollection();

            foreach (var n in needs)
            {
                // Punktet, eller punktet + det tegnede området (se BuildNeedGeometry lenger ned)
                var geometry = BuildNeedGeometry(n, factory);

                var props = new AttributesTable
                {
                    { "id", n.Id },
                    { "name", n.Name },
                    { "phone", canSeePhone ? n.PhoneNumber : string.Empty },
                    { "description", n.Description },
                    { "type", Enum.TryParse<NeedViewModel.NeedType>(n.Category, out var cat)
                        ? cat : NeedViewModel.NeedType.Other },
                    { "importance", Enum.TryParse<ImportanceLevel>(n.Priority, out var prio)
                        ? prio : ImportanceLevel.Planned },
                    { "status", n.Status },
                    { "areaSquareMeters", n.AreaSquareMeters }
                };

                collection.Add(new Feature(geometry, props));
            }

            return Json(collection);
        }

        /// <summary>
        /// Eksport av behov som CSV (kan åpnes i Excel). Enten bare åpne behov (ikke løste) eller alle.
        /// Knappene ligger på eksportsiden sammen med ressursene (Resource/Export).
        /// Klassen krever allerede admin eller koordinator, så det trengs ingen ekstra [Authorize] her.
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> ExportCsv(bool onlyOpen = true)
        {
            var needs = await RetrieveNeedsForExport(onlyOpen);

            var sb = new StringBuilder();
            sb.AppendLine("Id;Navn;Kategori;Viktighet;Status;Telefon;Beskrivelse;Frist;Registrert;Areal (m²);Breddegrad;Lengdegrad");

            foreach (var n in needs)
            {
                sb.AppendLine(string.Join(";",
                    n.Id,
                    Csv(n.Name),
                    TranslateNeedType(n.Category),
                    TranslatePriority(n.Priority),
                    TranslateStatus(n.Status),
                    Csv(n.PhoneNumber),
                    Csv(n.Description),
                    NorwegianTime.FromUtc(n.NeededBy)?.ToString("dd.MM.yyyy HH:mm") ?? "",
                    NorwegianTime.FromUtc(n.CreatedAt).ToString("dd.MM.yyyy HH:mm"),
                    n.AreaSquareMeters?.ToString("0", CultureInfo.InvariantCulture) ?? "",
                    n.Coords.Latitude.ToString(CultureInfo.InvariantCulture),
                    n.Coords.Longitude.ToString(CultureInfo.InvariantCulture)));
            }

            // BOM-en først i filen gjør at Excel viser æ, ø og å riktig
            var bytes = Encoding.UTF8.GetPreamble().Concat(Encoding.UTF8.GetBytes(sb.ToString())).ToArray();
            return File(bytes, "text/csv", $"{FileName(onlyOpen)}.csv");
        }

        /// <summary>
        /// Eksport av behov som GeoJSON. Filen kan åpnes direkte i kartprogrammer som QGIS, ArcGIS
        /// eller geojson.io. Behov med tegnet område får med både punktet og området.
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> ExportGeoJson(bool onlyOpen = true)
        {
            var needs = await RetrieveNeedsForExport(onlyOpen);

            var factory = NtsGeometryServices.Instance.CreateGeometryFactory(srid: 4326);
            var collection = new FeatureCollection();

            foreach (var n in needs)
            {
                // Egenskapene (properties) får norske navn, slik som i CSV-eksporten
                var props = new AttributesTable
                {
                    { "id", n.Id },
                    { "navn", n.Name },
                    { "kategori", TranslateNeedType(n.Category) },
                    { "viktighet", TranslatePriority(n.Priority) },
                    { "status", TranslateStatus(n.Status) },
                    { "telefon", n.PhoneNumber },
                    { "beskrivelse", n.Description },
                    { "frist", NorwegianTime.FromUtc(n.NeededBy) },
                    { "registrert", NorwegianTime.FromUtc(n.CreatedAt) },
                    { "arealKvm", n.AreaSquareMeters }
                };

                collection.Add(new Feature(BuildNeedGeometry(n, factory), props));
            }

            var bytes = JsonSerializer.SerializeToUtf8Bytes(collection, GeoJsonOptions);
            // application/geo+json er den offisielle filtypen for GeoJSON
            return File(bytes, "application/geo+json", $"{FileName(onlyOpen)}.geojson");
        }

        /// <summary>
        /// Lager geometrien til et behov: bare punktet, eller punktet + det tegnede området.
        /// Brukes både av kartet (GetInformation) og av GeoJSON-eksporten, så de alltid er like.
        /// </summary>
        private static Geometry BuildNeedGeometry(Need n, GeometryFactory factory)
        {
            // NTS: X = lengdegrad (longitude), Y = breddegrad (latitude)
            Geometry point = factory.CreatePoint(new Coordinate(n.Coords.Longitude, n.Coords.Latitude));

            // Ingen tegnet område: bare punktet
            if (string.IsNullOrEmpty(n.AreaGeoJson)) return point;

            try
            {
                // AreaGeoJson er tekst. Leaflet (layer.toGeoJSON()) lagrer en hel Feature,
                // men vi godtar også en ren geometri (f.eks. {"type":"Polygon",...}).
                // Derfor sjekker vi "type" først, og leser teksten på riktig måte.
                using var doc = JsonDocument.Parse(n.AreaGeoJson);
                var type = doc.RootElement.TryGetProperty("type", out var t) ? t.GetString() : null;

                // NB: Feature må leses som IFeature. Biblioteket lager sin egen Feature-type (StjFeature),
                // og ber vi om Feature direkte, krasjer det.
                var areaGeometry = type == "Feature"
                    ? doc.RootElement.Deserialize<IFeature>(GeoJsonOptions)?.Geometry
                    : doc.RootElement.Deserialize<Geometry>(GeoJsonOptions);

                if (areaGeometry != null)
                {
                    // GeometryCollection = flere geometrier i én. Punktet først, så området.
                    // site.js (fraGeoJson) regner med akkurat denne rekkefølgen.
                    return factory.CreateGeometryCollection(new[] { point, areaGeometry });
                }
            }
            catch (JsonException)
            {
                // Ugyldig område i databasen: bruk bare punktet
            }

            return point;
        }

        /// <summary>
        /// JSON-innstillinger med GeoJSON-støtte fra NetTopologySuite. Pene linjeskift, og æ, ø og å
        /// skrives som vanlig tekst. Brukes både til å lese AreaGeoJson og til å skrive GeoJSON-eksporten.
        /// </summary>
        private static readonly JsonSerializerOptions GeoJsonOptions = new()
        {
            WriteIndented = true,
            Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
            Converters = { new GeoJsonConverterFactory() }
        };

        /// <summary>
        /// Alle behov, eller bare de som ikke er løst ennå. Sortert etter Id.
        /// </summary>
        private async Task<List<Need>> RetrieveNeedsForExport(bool onlyOpen)
        {
            var all = await _repo.GetAllAsync();
            return all
                .Where(n => !onlyOpen || n.Status != EntryStatus.Resolved)
                .OrderBy(n => n.Id)
                .ToList();
        }

        /// <summary>
        /// Ulike filnavn så eksportene ikke blandes sammen med hverandre eller med ressursene
        /// </summary>
        private static string FileName(bool onlyOpen)
        {
            var name = onlyOpen ? "behov_apne" : "behov_alle";
            return $"{name}_{NorwegianTime.FromUtc(DateTime.UtcNow):yyyy-MM-dd}";
        }

        /// <summary>
        /// Category lagres som "Food", "Medicine" osv. Eksporten viser norsk navn (samme som i skjemaet).
        /// </summary>
        private static string TranslateNeedType(string category) => category switch
        {
            nameof(NeedViewModel.NeedType.Food) => "Mat/Vann",
            nameof(NeedViewModel.NeedType.Medicine) => "Medisiner",
            nameof(NeedViewModel.NeedType.Transport) => "Transport",
            nameof(NeedViewModel.NeedType.PowerGenerator) => "Strøm/aggregat",
            nameof(NeedViewModel.NeedType.SnowRemoval) => "Snørydding",
            nameof(NeedViewModel.NeedType.SandGravel) => "Sand/grus",
            nameof(NeedViewModel.NeedType.Machinery) => "Maskiner",
            nameof(NeedViewModel.NeedType.Evacuation) => "Evakuering",
            nameof(NeedViewModel.NeedType.Communication) => "Samband",
            nameof(NeedViewModel.NeedType.Personnel) => "Mannskap",
            _ => "Annet"
        };

        /// <summary>
        /// Priority lagres som "Acute" eller "Planned". Gamle verdier (f.eks. "Medium") vises som Planlagt,
        /// akkurat som på kartet.
        /// </summary>
        private static string TranslatePriority(string priority) =>
            priority == nameof(ImportanceLevel.Acute) ? "Akutt" : "Planlagt";

        private static string TranslateStatus(EntryStatus status) => status switch
        {
            EntryStatus.New => "Ny",
            EntryStatus.UnderReview => "Vurderes",
            EntryStatus.Assigned => "Tildelt",
            EntryStatus.Resolved => "Løst",
            _ => ""
        };

        /// <summary>
        /// Gjør tekst trygg å legge i en CSV-fil, så Excel ikke blander sammen kolonnene.
        /// Setter teksten i anførselstegn hvis den inneholder ; " eller linjeskift.
        /// </summary>
        private static string Csv(string? value)
        {
            if (string.IsNullOrEmpty(value)) return "";
            if (value.Contains(';') || value.Contains('"') || value.Contains('\n'))
                return "\"" + value.Replace("\"", "\"\"") + "\"";
            return value;
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
