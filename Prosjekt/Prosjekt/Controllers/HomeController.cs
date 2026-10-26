using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Identity;
using Prosjekt.DataAccess.Repositories;
using Prosjekt.Models;
using Prosjekt.Models.ModelView;

namespace Prosjekt.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly IGeoChangeRepository _geoChangeRepository;
        private readonly UserManager<IdentityUser> _userManager;

        // 1. Dependency Injection: Ta imot tjenestene automatisk
        public HomeController(
            ILogger<HomeController> logger,
            IGeoChangeRepository geoChangeRepository,
            UserManager<IdentityUser> userManager)
        {
            _logger = logger;
            _geoChangeRepository = geoChangeRepository;
            _userManager = userManager;
        }

        [HttpGet]
        public IActionResult Index()
        {
            // Hvis du senere vil vise posisjoner på forsiden, kan du hente dem via _geoChangeRepository her.
            return View();
        }

        [HttpGet]
        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}