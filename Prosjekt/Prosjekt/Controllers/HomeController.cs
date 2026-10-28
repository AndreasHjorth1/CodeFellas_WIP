using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Prosjekt.Models.ModelView;
using DotNetEnv;

namespace Prosjekt.Controllers;

public class HomeController : Controller
{
    private readonly string _connectionString;

    public HomeController(IConfiguration configuration)
    {
        // Leser .env-filen automatisk uansett om den kjøres lokalt eller i tester
        DotNetEnv.Env.TraversePath().Load();

        _connectionString = Environment.GetEnvironmentVariable("DB_CONNECTION_STRING")
            ?? configuration.GetConnectionString("mysql")
            ?? configuration.GetConnectionString("mariadbcontainer")
            ?? throw new InvalidOperationException("Missing database connection string. Make sure .env file exists.");
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