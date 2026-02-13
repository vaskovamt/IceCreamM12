using System.Diagnostics;
using IceCreamM12.Application.Interfaces;
using IceCreamM12.Web.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace IceCreamM12.Web.Controllers;

[Authorize(Roles = "Owner,Worker,Client")]
public class HomeController : Controller
{
    private readonly IManagementService _managementService;

    public HomeController(IManagementService managementService)
    {
        _managementService = managementService;
    }

    [AllowAnonymous]
    public IActionResult Index()
    {
        return View();
    }

    [AllowAnonymous]
    public IActionResult Privacy()
    {
        return View();
    }

    [AllowAnonymous]
    public IActionResult About()
    {
        return View();
    }

    [AllowAnonymous]
    public async Task<IActionResult> Products(CancellationToken cancellationToken)
    {
        var products = await _managementService.GetProductsAsync(cancellationToken);
        return View(products);
    }

    [AllowAnonymous]
    public IActionResult Contact()
    {
        return View();
    }

    [AllowAnonymous]
    public IActionResult LoginRegister()
    {
        return View();
    }

    [AllowAnonymous]
    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }
}
