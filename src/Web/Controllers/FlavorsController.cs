using IceCreamM12.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace IceCreamM12.Web.Controllers;

[Authorize(Roles = "Admin")]
public class FlavorsController : Controller
{
    private readonly IFlavorService _flavorService;

    public FlavorsController(IFlavorService flavorService)
    {
        _flavorService = flavorService;
    }

    public async Task<IActionResult> Index(CancellationToken cancellationToken)
    {
        var flavors = await _flavorService.GetAvailableFlavorsAsync(cancellationToken);
        return View(flavors);
    }
}
