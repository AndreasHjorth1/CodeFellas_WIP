using System.Diagnostics;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Prosjekt.Models.ModelView;

namespace Prosjekt.Controllers;

public class HomeController : Controller
{
    [Authorize]
    public IActionResult Index()
    {
        return View();
    }

    [HttpGet]
    public IActionResult FrontPage()
    {
        return View();
    }
    public IActionResult Privacy()
    {
        return View();
    }
    public IActionResult AboutUs()
    {
        return View();
    }
    public IActionResult UserInstruction()
    {
        return View();
    }
    public IActionResult CrisisInformation()
    {
        return View();
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }
}