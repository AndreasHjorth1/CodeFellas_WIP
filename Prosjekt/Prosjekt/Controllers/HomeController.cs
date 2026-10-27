using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Prosjekt.Models;
using Prosjekt.Models.ModelView;
using MySqlConnector;
namespace Prosjekt.Controllers;

public class HomeController : Controller
{
    /// <summary>
    /// Displays the default view for the controller.
    /// </summary>
    /// <returns>An IActionResult that renders the view.</returns>

    public IActionResult Index()
    {
        return View();
    }
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