using System;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Prosjekt.DataAccess.Repositories;
using Prosjekt.Models.ModelView;
using Prosjekt.Utils;

namespace Prosjekt.Controllers
{
    /// <summary>
    /// Profilside. MIDLERTIDIG LØSNING til innlogging (ASP.NET Core Identity) er på plass:
    ///  - profilen lagres i en informasjonskapsel (cookie) i brukerens nettleser, ikke i databasen
    ///  - "Mine ressurser" finnes ved å matche mobilnummeret i profilen mot Resource.PhoneNumber,
    ///    fordi Resource ennå ikke har en eier (OwnerId).
    /// Når Identity kommer: bytt ReadProfile/WriteProfile mot brukerkontoen, og filtrer på OwnerId.
    /// </summary>
    public class ProfileController : Controller
    {
        public const string CookieName = "tf_profil";
        private const string SavedKey = "ProfileSaved";
        private const string MessageKey = "ProfileMessage";

        private readonly IResourceRepository _repo;

        public ProfileController(IResourceRepository repo)
        {
            _repo = repo;
        }

        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var saved = ReadProfile();
            var vm = await BuildPageAsync(saved ?? new ProfileFormModel(), saved);
            vm.Saved = TempData?[SavedKey] as string == "1";
            vm.Message = TempData?[MessageKey] as string;
            return View(vm);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Index([Bind(Prefix = "Profile")] ProfileFormModel profile)
        {
            var saved = ReadProfile();

            if (!ModelState.IsValid)
            {
                return View(await BuildPageAsync(profile, saved));
            }

            profile.Username = profile.Username.Trim();
            profile.PhoneNumber = PhoneNumberHelper.Format(profile.PhoneNumber);
            profile.Email = string.IsNullOrWhiteSpace(profile.Email) ? null : profile.Email.Trim();
            profile.Address = string.IsNullOrWhiteSpace(profile.Address) ? null : profile.Address.Trim();

            WriteProfile(profile);
            if (TempData != null) TempData[SavedKey] = "1";

            // Post/Redirect/Get: hindrer dobbel innsending ved oppdatering av siden
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Clear()
        {
            Response.Cookies.Delete(CookieName);
            return RedirectToAction(nameof(Index));
        }

        // Bytter en ressurs mellom tilgjengelig og utilgjengelig.
        // Midlertidig tilgangskontroll: ressursen må ha samme mobilnummer som profilen, slik som i "Mine ressurser".
        // TODO: når Identity og OwnerId er på plass, sjekk eier i stedet, og følg statusreglene i A-15
        //       (f.eks. hva som skjer med en ressurs som er tildelt et behov).
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ToggleAvailability(int id)
        {
            var saved = ReadProfile();
            if (saved == null)
            {
                return RedirectToAction(nameof(Index));
            }

            var resource = await _repo.GetByIdAsync(id);
            if (resource == null)
            {
                return NotFound();
            }

            if (!PhoneNumberHelper.AreEqual(resource.PhoneNumber, saved.PhoneNumber))
            {
                return StatusCode(StatusCodes.Status403Forbidden);
            }

            resource.IsAvailable = !resource.IsAvailable;
            await _repo.UpdateAsync(resource);

            if (TempData != null)
            {
                TempData[MessageKey] = resource.IsAvailable
                    ? $"{resource.Name} er nå tilgjengelig."
                    : $"{resource.Name} er nå utilgjengelig.";
            }

            return RedirectToAction(nameof(Index));
        }

        // ---------- hjelpemetoder ----------

        private async Task<ProfilePageViewModel> BuildPageAsync(ProfileFormModel formValues, ProfileFormModel? saved)
        {
            var vm = new ProfilePageViewModel
            {
                Profile = formValues,
                HasProfile = saved != null
            };

            // Ressursene hentes alltid med det LAGREDE nummeret, ikke det som står i et ugyldig skjema
            var phone = saved?.PhoneNumber;
            if (string.IsNullOrWhiteSpace(phone)) return vm;

            var all = await _repo.GetAllAsync();
            vm.Resources = all
                .Where(r => PhoneNumberHelper.AreEqual(r.PhoneNumber, phone))
                .OrderByDescending(r => r.CreatedAt)
                .Select(r => new ProfileResourceItem
                {
                    Id = r.Id,
                    Name = r.Name,
                    CategoryLabel = CategoryLabel(r.Category),
                    SubCategory = r.SubCategory,
                    Quantity = r.Quantity,
                    IsAvailable = r.IsAvailable,
                    AvailableFrom = r.AvailableFrom,
                    AvailableTo = r.AvailableTo,
                    CreatedAt = r.CreatedAt
                })
                .ToList();

            return vm;
        }

        private static string CategoryLabel(string category) =>
            Enum.TryParse<ResourceViewModel.ResourceType>(category, out var type)
                ? type switch
                {
                    ResourceViewModel.ResourceType.Provision => "Provisjon",
                    ResourceViewModel.ResourceType.Clothing => "Klær",
                    ResourceViewModel.ResourceType.Tool => "Verktøy",
                    ResourceViewModel.ResourceType.Vehicle => "Kjøretøy",
                    ResourceViewModel.ResourceType.Materials => "Materialer",
                    _ => "Annet"
                }
                : "Annet";

        private ProfileFormModel? ReadProfile()
        {
            if (!Request.Cookies.TryGetValue(CookieName, out var json) || string.IsNullOrWhiteSpace(json))
                return null;

            try
            {
                return JsonSerializer.Deserialize<ProfileFormModel>(json);
            }
            catch (JsonException)
            {
                return null; // ødelagt cookie: behandle som "ingen profil"
            }
        }

        private void WriteProfile(ProfileFormModel profile)
        {
            Response.Cookies.Append(CookieName, JsonSerializer.Serialize(profile), new CookieOptions
            {
                HttpOnly = true,
                Secure = Request.IsHttps,
                SameSite = SameSiteMode.Lax,
                IsEssential = true,
                Expires = DateTimeOffset.UtcNow.AddDays(30)
            });
        }
    }
}