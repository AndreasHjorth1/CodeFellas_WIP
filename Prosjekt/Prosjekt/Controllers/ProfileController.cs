using Microsoft.AspNetCore.Mvc;
namespace Prosjekt.Controllers
{
    public class SettingsController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
    }
}