using System;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
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

                // BRUKER NÅ DEN NYE MODELL-EGENSKAPEN DIREKTE I STEDET FOR REQUEST.FORM
                string extraInfo = "";
                if (baseResource.Category == ResourceViewModel.ResourceType.Vehicle && !string.IsNullOrWhiteSpace(baseResource.LicensePlate))
                {
                    extraInfo = $"[Reg.nr: {baseResource.LicensePlate}]";
                }
                else if (baseResource.Category == ResourceViewModel.ResourceType.Clothing && !string.IsNullOrWhiteSpace(baseResource.Size))
                {
                    extraInfo = $"[Size: {baseResource.Size}]";
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

            string description = resource.Description ?? "";
            string licensePlate = "";
            string size = "";

            // Henter ut [Reg.nr: ...] fra beskrivelsen
            if (description.Contains("[Reg.nr:"))
            {
                var startIndex = description.IndexOf("[Reg.nr:") + 8;
                var endIndex = description.IndexOf("]", startIndex);
                if (endIndex > startIndex)
                {
                    licensePlate = description.Substring(startIndex, endIndex - startIndex).Trim();
                    description = description.Remove(description.IndexOf("[Reg.nr:"), (endIndex - description.IndexOf("[Reg.nr:")) + 1).Trim();
                }
            }

            // Henter ut [Size: ...] fra beskrivelsen
            if (description.Contains("[Size:"))
            {
                var startIndex = description.IndexOf("[Size:") + 6;
                var endIndex = description.IndexOf("]", startIndex);
                if (endIndex > startIndex)
                {
                    size = description.Substring(startIndex, endIndex - startIndex).Trim();
                    description = description.Remove(description.IndexOf("[Size:"), (endIndex - description.IndexOf("[Size:")) + 1).Trim();
                }
            }

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
                Category = Enum.TryParse<ResourceViewModel.ResourceType>(resource.Category, out var cat)
                    ? cat
                    : ResourceViewModel.ResourceType.Other,
                AvailableFrom = resource.AvailableFrom,
                AvailableTo = resource.AvailableTo,
                LicensePlate = licensePlate,
                Size = size
            };

            return View(viewModel);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, ResourceViewModel model)
        {
            if (id != model.Id) return BadRequest();

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

            if (ModelState.IsValid)
            {
                var resourceEntity = await _repo.GetByIdAsync(id);
                if (resourceEntity == null) return NotFound();

                // BRUKER NÅ MODELLEN DIREKTE VED LAGRING FRA EDIT
                string extraInfo = "";
                if (model.Category == ResourceViewModel.ResourceType.Vehicle && !string.IsNullOrWhiteSpace(model.LicensePlate))
                {
                    extraInfo = $"[Reg.nr: {model.LicensePlate}]";
                }
                else if (model.Category == ResourceViewModel.ResourceType.Clothing && !string.IsNullOrWhiteSpace(model.Size))
                {
                    extraInfo = $"[Size: {model.Size}]";
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

        // Eksport av alle tilgjengelige ressurser. Ment for offentlige aktører.
        // TODO: [Authorize(Roles = "Offentlig")] på Export, ExportCsv og ExportJson når innlogging er på plass.
        [HttpGet]
        public IActionResult Export()
        {
            return View();
        }

        [HttpGet]
        public async Task<IActionResult> ExportCsv()
        {
            var ressurser = await HentTilgjengeligeRessurser();

            var sb = new StringBuilder();
            sb.AppendLine("Id;Navn;Kategori;Antall;Telefon;Beskrivelse;Tilgjengelig fra;Tilgjengelig til;Breddegrad;Lengdegrad");

            foreach (var r in ressurser)
            {
                sb.AppendLine(string.Join(";",
                    r.Id,
                    Csv(r.Name),
                    OversettKategori(r.Category),
                    r.Quantity,
                    Csv(r.PhoneNumber),
                    Csv(r.Description),
                    r.AvailableFrom?.ToString("dd.MM.yyyy HH:mm") ?? "",
                    r.AvailableTo?.ToString("dd.MM.yyyy HH:mm") ?? "",
                    r.Coords.Latitude.ToString(CultureInfo.InvariantCulture),
                    r.Coords.Longitude.ToString(CultureInfo.InvariantCulture)));
            }

            // BOM gjør at Excel viser æ, ø og å riktig
            var bytes = Encoding.UTF8.GetPreamble().Concat(Encoding.UTF8.GetBytes(sb.ToString())).ToArray();
            return File(bytes, "text/csv", $"ressurser_{DateTime.Now:yyyy-MM-dd}.csv");
        }

        [HttpGet]
        public async Task<IActionResult> ExportJson()
        {
            var ressurser = await HentTilgjengeligeRessurser();

            var data = ressurser.Select(r => new
            {
                r.Id,
                Navn = r.Name,
                Kategori = OversettKategori(r.Category),
                Antall = r.Quantity,
                Telefon = r.PhoneNumber,
                Beskrivelse = r.Description,
                TilgjengeligFra = r.AvailableFrom,
                TilgjengeligTil = r.AvailableTo,
                Breddegrad = r.Coords.Latitude,
                Lengdegrad = r.Coords.Longitude
            });

            var options = new JsonSerializerOptions
            {
                WriteIndented = true,
                Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping // æøå som tekst, ikke ø
            };

            var bytes = JsonSerializer.SerializeToUtf8Bytes(data, options);
            return File(bytes, "application/json", $"ressurser_{DateTime.Now:yyyy-MM-dd}.json");
        }

        // Kun tilgjengelige ressurser, sortert etter Id
        private async Task<List<Resource>> HentTilgjengeligeRessurser()
        {
            var alle = await _repo.GetAllAsync();
            return alle
                .Where(r => r.IsAvailable)
                .OrderBy(r => r.Id)
                .ToList();
        }

        // Category lagres som "Vehicle", "Tool" osv. Eksporten viser norsk navn.
        private static string OversettKategori(string category) => category switch
        {
            nameof(ResourceViewModel.ResourceType.Provision) => "Provisjon",
            nameof(ResourceViewModel.ResourceType.Clothing) => "Klær",
            nameof(ResourceViewModel.ResourceType.Tool) => "Verktøy",
            nameof(ResourceViewModel.ResourceType.Vehicle) => "Kjøretøy",
            nameof(ResourceViewModel.ResourceType.Materials) => "Materialer",
            _ => "Annet"
        };

        // Setter tekst i anførselstegn hvis den inneholder ; " eller linjeskift
        private static string Csv(string? value)
        {
            if (string.IsNullOrEmpty(value)) return "";
            if (value.Contains(';') || value.Contains('"') || value.Contains('\n'))
                return "\"" + value.Replace("\"", "\"\"") + "\"";
            return value;
        }
    }
}