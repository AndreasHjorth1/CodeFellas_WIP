using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Prosjekt.DataAccess.Repositories;
using Prosjekt.Models.Authorization;
using Prosjekt.Models.Entities;
using Prosjekt.Models.Enums;
using Prosjekt.Models.ModelView;
using Prosjekt.Utils;
using System.Globalization;
using System.Text;
using NetTopologySuite.Features;

namespace Prosjekt.Controllers
{
    /// <summary>
    /// Eksport av ressurser og behov som CSV og GeoJSON. Ment for offentlige aktører.
    /// </summary>
    [Authorize(Roles = RoleNames.AdminOrCoordinator)]
    public class ExportController : Controller
    {
        private readonly IResourceRepository _resourceRepo;
        private readonly INeedRepository _needRepo;

        public ExportController(IResourceRepository resourceRepo, INeedRepository needRepo)
        {
            _resourceRepo = resourceRepo;
            _needRepo = needRepo;
        }

        [HttpGet]
        public IActionResult Index()
        {
            return View();
        }

        [HttpGet]
        public async Task<IActionResult> ResourcesCsv(bool onlyAvailable = true)
        {
            var resources = await RetrieveResources(onlyAvailable);

            var sb = new StringBuilder();
            sb.AppendLine("Id;Navn;Kategori;Antall;Tilgjengelig;Telefon;Beskrivelse;Tilgjengelig fra;Tilgjengelig til;Breddegrad;Lengdegrad");

            foreach (var r in resources)
            {
                sb.AppendLine(string.Join(";",
                    r.Id,
                    Csv.Text(r.Name),
                    TranslateCategory(r.Category),
                    r.Quantity,
                    r.IsAvailable ? "Ja" : "Nei",
                    Csv.Text(r.PhoneNumber),
                    Csv.Text(r.Description),
                    Csv.Date(r.AvailableFrom),
                    Csv.Date(r.AvailableTo),
                    r.Coords.Latitude.ToString(CultureInfo.InvariantCulture),
                    r.Coords.Longitude.ToString(CultureInfo.InvariantCulture)));
            }

            return File(Csv.ToBytes(sb), Csv.ContentType, $"{ResourceFileName(onlyAvailable)}.csv");
        }

        [HttpGet]
        public async Task<IActionResult> ResourcesGeoJson(bool onlyAvailable = true)
        {
            var resources = await RetrieveResources(onlyAvailable);

            var collection = new FeatureCollection();
            foreach (var r in resources)
            {
                collection.Add(new Feature(GeoJson.Point(r.Coords.Latitude, r.Coords.Longitude), new AttributesTable
                {
                    { "id", r.Id },
                    { "navn", r.Name },
                    { "kategori", TranslateCategory(r.Category) },
                    { "antall", r.Quantity },
                    { "tilgjengelig", r.IsAvailable },
                    { "telefon", r.PhoneNumber },
                    { "beskrivelse", r.Description },
                    { "tilgjengeligFra", GeoJson.Utc(r.AvailableFrom) },
                    { "tilgjengeligTil", GeoJson.Utc(r.AvailableTo) }
                }));
            }

            return File(GeoJson.ToBytes(collection), GeoJson.ContentType, $"{ResourceFileName(onlyAvailable)}.geojson");
        }

        [HttpGet]
        public async Task<IActionResult> NeedsCsv(bool onlyOpen = true)
        {
            var needs = await RetrieveNeeds(onlyOpen);

            var sb = new StringBuilder();
            sb.AppendLine("Id;Navn;Kategori;Viktighet;Status;Telefon;Beskrivelse;Frist;Registrert;Areal (m²);Breddegrad;Lengdegrad");

            foreach (var n in needs)
            {
                sb.AppendLine(string.Join(";",
                    n.Id,
                    Csv.Text(n.Name),
                    TranslateNeedType(n.Category),
                    TranslatePriority(n.Priority),
                    TranslateStatus(n.Status),
                    Csv.Text(n.PhoneNumber),
                    Csv.Text(n.Description),
                    Csv.Date(n.NeededBy),
                    Csv.Date(n.CreatedAt),
                    n.AreaSquareMeters?.ToString("0", CultureInfo.InvariantCulture) ?? "",
                    n.Coords.Latitude.ToString(CultureInfo.InvariantCulture),
                    n.Coords.Longitude.ToString(CultureInfo.InvariantCulture)));
            }

            return File(Csv.ToBytes(sb), Csv.ContentType, $"{NeedFileName(onlyOpen)}.csv");
        }

        /// <summary>
        /// Behov med tegnet område får området som geometri. Posisjonen ligger uansett i egenskapene.
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> NeedsGeoJson(bool onlyOpen = true)
        {
            var needs = await RetrieveNeeds(onlyOpen);

            var collection = new FeatureCollection();
            foreach (var n in needs)
            {
                var geometry = GeoJson.AreaOrPoint(n.AreaGeoJson, n.Coords.Latitude, n.Coords.Longitude);
                collection.Add(new Feature(geometry, new AttributesTable
                {
                    { "id", n.Id },
                    { "navn", n.Name },
                    { "kategori", TranslateNeedType(n.Category) },
                    { "viktighet", TranslatePriority(n.Priority) },
                    { "status", TranslateStatus(n.Status) },
                    { "telefon", n.PhoneNumber },
                    { "beskrivelse", n.Description },
                    { "frist", GeoJson.Utc(n.NeededBy) },
                    { "registrert", GeoJson.Utc(n.CreatedAt) },
                    { "arealKvm", n.AreaSquareMeters },
                    { "breddegrad", n.Coords.Latitude },
                    { "lengdegrad", n.Coords.Longitude }
                }));
            }

            return File(GeoJson.ToBytes(collection), GeoJson.ContentType, $"{NeedFileName(onlyOpen)}.geojson");
        }

        /// <summary>
        /// Alle ressurser, eller bare de som er tilgjengelige. Sortert etter Id.
        /// </summary>
        private async Task<List<Resource>> RetrieveResources(bool onlyAvailable)
        {
            var all = await _resourceRepo.GetAllAsync();
            return all
                .Where(r => !onlyAvailable || r.IsAvailable)
                .OrderBy(r => r.Id)
                .ToList();
        }

        /// <summary>
        /// Alle behov, eller bare de som ikke er løst ennå. Sortert etter Id.
        /// </summary>
        private async Task<List<Need>> RetrieveNeeds(bool onlyOpen)
        {
            var all = await _needRepo.GetAllAsync();
            return all
                .Where(n => !onlyOpen || n.Status != EntryStatus.Resolved)
                .OrderBy(n => n.Id)
                .ToList();
        }

        private static string ResourceFileName(bool onlyAvailable) =>
            WithDate(onlyAvailable ? "ressurser_tilgjengelige" : "ressurser_alle");

        private static string NeedFileName(bool onlyOpen) =>
            WithDate(onlyOpen ? "behov_apne" : "behov_alle");

        private static string WithDate(string name) =>
            $"{name}_{NorwegianTime.FromUtc(DateTime.UtcNow):yyyy-MM-dd}";

        /// <summary>
        /// Category lagres som "Vehicle", "Tool" osv. Eksporten viser norsk navn.
        /// </summary>
        private static string TranslateCategory(string category) => category switch
        {
            nameof(ResourceViewModel.ResourceType.Provision) => "Provisjon",
            nameof(ResourceViewModel.ResourceType.Clothing) => "Klær",
            nameof(ResourceViewModel.ResourceType.Tool) => "Verktøy",
            nameof(ResourceViewModel.ResourceType.Vehicle) => "Kjøretøy",
            nameof(ResourceViewModel.ResourceType.Materials) => "Materialer",
            _ => "Annet"
        };

        /// <summary>
        /// Category lagres som "Food", "Medicine" osv. Eksporten viser samme norske navn som skjemaet.
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
        /// Gamle verdier som "Medium" vises som Planlagt, slik som på kartet.
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
    }
}
