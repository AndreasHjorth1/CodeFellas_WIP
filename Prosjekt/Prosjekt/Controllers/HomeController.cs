using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Prosjekt.Models;
using Prosjekt.Models.ModelView;
using MySqlConnector;
namespace Prosjekt.Controllers;

public class HomeController : Controller
{
    /// <summary>
    /// Database connection string used to create connections to the target database.
    /// </summary>
    /// <remarks>Assigned during construction and immutable thereafter. Should contain provider-specific
    /// connection settings required by the data provider.</remarks>
    private readonly string _connectionString;

    public HomeController(IConfiguration configuration)
    {
        _connectionString = configuration.GetConnectionString("mysql")
            ?? configuration.GetConnectionString("mariadbcontainer")
            ?? "server=mariadbcontainer;port=3306;database=mysql;user=root;password=Gruppe12!";
    }

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