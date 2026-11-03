using Microsoft.AspNetCore.Mvc;

namespace Prosjekt.Controllers;

public class AccountController : Controller
{
    [HttpGet]
    public IActionResult Login()
    {
        return View();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult Login(string email, string password)
    {
        // Midlertidig demo-login uten autentisering.
        return RedirectToAction("FrontPage", "Home");
    }

    [HttpGet]
    public IActionResult Register()
    {
        return RedirectToAction(nameof(Login));
    }
}
