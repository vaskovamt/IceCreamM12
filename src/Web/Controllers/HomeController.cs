using System.Diagnostics;
using IceCreamM12.Application.Interfaces;
using IceCreamM12.Web.Models;
using Microsoft.AspNetCore.Mvc;

namespace IceCreamM12.Web.Controllers;

public class HomeController : Controller
{
    private readonly IFlavorService _flavorService;

    public HomeController(IFlavorService flavorService)
    {
        _flavorService = flavorService;
    }

    public async Task<IActionResult> Index(CancellationToken cancellationToken)
    {
        var flavors = await _flavorService.GetAvailableFlavorsAsync(cancellationToken);
        return View(flavors);
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
