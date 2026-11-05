using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Prosjekt.DataAccess.Repositories;
using Prosjekt.Models.Authorization;
using Prosjekt.Models.Entities;
using Prosjekt.Models.ModelView;
using Prosjekt.Utils;
using System.Security.Claims;
using System.Globalization;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;

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
        /// Bare admin og eieren av ressursen kan endre eller slette den.
        /// </summary>
        private bool CanModify(Resource resource)
        {
            return User.IsInRole(RoleNames.Admin)
                || resource.UserId == User.FindFirstValue(ClaimTypes.NameIdentifier);
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

            ResourceViewModel? createdResourceModel = null;

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

                if (createdResourceModel == null)
                {
                    createdResourceModel = baseResource;
                    createdResourceModel.Id = resourceEntity.Id;
                    createdResourceModel.Latitude = model.Latitude;
                    createdResourceModel.Longitude = model.Longitude;
                }
            }

            return View("Create", createdResourceModel);
        }

        /// <summary>
        /// Admin ser alle ressurser, andre bare sine egne.
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> Overview()
        {
            var resourceEntities = await GetVisibleResourcesAsync();
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
                Category = ParseCategory(r.Category),
                AvailableFrom = NorwegianTime.FromUtc(r.AvailableFrom),
                AvailableTo = NorwegianTime.FromUtc(r.AvailableTo),
                Allocated = allocations.Where(a => a.ResourceId == r.Id).Sum(a => a.Quantity),
            }).ToList();

            return View(resourceViewModels);
        }

        /// <summary>
        /// Viser redigeringsskjemaet. Skiltnummeret skilles ut fra beskrivelsen og vises i eget felt.
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> Edit(int id)
        {
            var resource = await _repo.GetByIdAsync(id);
            if (resource == null) return NotFound();
            if (!CanModify(resource)) return Forbid();

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
            if (!CanModify(resourceEntity)) return Forbid();

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
            if (!CanModify(resource)) return Forbid();

            await _repo.DeleteAsync(id);
            await _allocationRepo.DeleteByResourceAsync(id);
            return RedirectToAction(nameof(Overview));
        }

        /// <summary>
        /// Data til kartet på Home/Index. Admin får alle ressurser, andre bare sine egne.
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> GetInformation()
        {
            var resources = await GetVisibleResourcesAsync();

            var result = resources.Select(r => new ResourceViewModel
            {
                Id = r.Id,
                Name = r.Name,
                PhoneNumber = r.PhoneNumber,
                Description = r.Description,
                Quantity = r.Quantity,
                Availability = r.IsAvailable,
                Category = ParseCategory(r.Category),
                Latitude = r.Coords.Latitude,
                Longitude = r.Coords.Longitude
            });

            return Json(result);
        }

        /// <summary>
        /// Eksport av ressurser, enten bare tilgjengelige eller alle registrerte. Ment for offentlige aktører.
        /// </summary>
        [Authorize(Roles = RoleNames.Admin)]
        [HttpGet]
        public IActionResult Export()
        {
            return View();
        }

        [Authorize(Roles = RoleNames.Admin)]
        [HttpGet]
        public async Task<IActionResult> ExportCsv(bool bareTilgjengelige = true)
        {
            var ressurser = await HentRessurserForEksport(bareTilgjengelige);

            var sb = new StringBuilder();
            sb.AppendLine("Id;Navn;Kategori;Antall;Tilgjengelig;Telefon;Beskrivelse;Tilgjengelig fra;Tilgjengelig til;Breddegrad;Lengdegrad");

            foreach (var r in ressurser)
            {
                sb.AppendLine(string.Join(";",
                    r.Id,
                    Csv(r.Name),
                    OversettKategori(r.Category),
                    r.Quantity,
                    r.IsAvailable ? "Ja" : "Nei",
                    Csv(r.PhoneNumber),
                    Csv(r.Description),
                    NorwegianTime.FromUtc(r.AvailableFrom)?.ToString("dd.MM.yyyy HH:mm") ?? "",
                    NorwegianTime.FromUtc(r.AvailableTo)?.ToString("dd.MM.yyyy HH:mm") ?? "",
                    r.Coords.Latitude.ToString(CultureInfo.InvariantCulture),
                    r.Coords.Longitude.ToString(CultureInfo.InvariantCulture)));
            }

            var bytes = Encoding.UTF8.GetPreamble().Concat(Encoding.UTF8.GetBytes(sb.ToString())).ToArray();
            return File(bytes, "text/csv", $"{Filnavn(bareTilgjengelige)}.csv");
        }

        [Authorize(Roles = RoleNames.Admin)]
        [HttpGet]
        public async Task<IActionResult> ExportJson(bool bareTilgjengelige = true)
        {
            var ressurser = await HentRessurserForEksport(bareTilgjengelige);

            var data = ressurser.Select(r => new
            {
                r.Id,
                Navn = r.Name,
                Kategori = OversettKategori(r.Category),
                Antall = r.Quantity,
                Tilgjengelig = r.IsAvailable,
                Telefon = r.PhoneNumber,
                Beskrivelse = r.Description,
                TilgjengeligFra = NorwegianTime.FromUtc(r.AvailableFrom),
                TilgjengeligTil = NorwegianTime.FromUtc(r.AvailableTo),
                Breddegrad = r.Coords.Latitude,
                Lengdegrad = r.Coords.Longitude
            });

            var options = new JsonSerializerOptions
            {
                WriteIndented = true,
                Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
            };

            var bytes = JsonSerializer.SerializeToUtf8Bytes(data, options);
            return File(bytes, "application/json", $"{Filnavn(bareTilgjengelige)}.json");
        }

        /// <summary>
        /// Admin får alle ressurser, andre bare sine egne.
        /// </summary>
        private Task<IEnumerable<Resource>> GetVisibleResourcesAsync()
        {
            return User.IsInRole(RoleNames.Admin)
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

        /// <summary>
        /// Alle registrerte ressurser, eller bare de som er tilgjengelige. Sortert etter Id.
        /// </summary>
        private async Task<List<Resource>> HentRessurserForEksport(bool bareTilgjengelige)
        {
            var alle = await _repo.GetAllAsync();
            return alle
                .Where(r => !bareTilgjengelige || r.IsAvailable)
                .OrderBy(r => r.Id)
                .ToList();
        }

        /// <summary>
        /// Ulike filnavn så de to eksportene ikke blandes sammen
        /// </summary>
        private static string Filnavn(bool bareTilgjengelige)
        {
            var navn = bareTilgjengelige ? "ressurser_tilgjengelige" : "ressurser_alle";
            return $"{navn}_{NorwegianTime.FromUtc(DateTime.UtcNow):yyyy-MM-dd}";
        }

        /// <summary>
        /// Category lagres som "Vehicle", "Tool" osv. Eksporten viser norsk navn.
        /// </summary>
        private static string OversettKategori(string category) => category switch
        {
            nameof(ResourceViewModel.ResourceType.Provision) => "Provisjon",
            nameof(ResourceViewModel.ResourceType.Clothing) => "Klær",
            nameof(ResourceViewModel.ResourceType.Tool) => "Verktøy",
            nameof(ResourceViewModel.ResourceType.Vehicle) => "Kjøretøy",
            nameof(ResourceViewModel.ResourceType.Materials) => "Materialer",
            _ => "Annet"
        };

        /// <summary>
        /// Setter tekst i anførselstegn hvis den inneholder ; " eller linjeskift
        /// </summary>
        private static string Csv(string? value)
        {
            if (string.IsNullOrEmpty(value)) return "";
            if (value.Contains(';') || value.Contains('"') || value.Contains('\n'))
                return "\"" + value.Replace("\"", "\"\"") + "\"";
            return value;
        }
    }
}
