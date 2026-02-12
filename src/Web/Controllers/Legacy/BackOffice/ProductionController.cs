using System.Security.Claims;
using IceCreamM12.Application.Interfaces;
using IceCreamM12.Web.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace IceCreamM12.Web.Controllers;

[Authorize(Roles = "Owner,Worker")]
public class ProductionController : Controller
{
    private readonly IProductionService _productionService;

    public ProductionController(IProductionService productionService)
    {
        _productionService = productionService;
    }

    public IActionResult Index()
    {
        return View();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Produce(ProductionRequest request, CancellationToken cancellationToken)
    {
        await _productionService.ProduceAsync(
            request.ProductId,
            request.Quantity,
            User.FindFirstValue(ClaimTypes.NameIdentifier),
            cancellationToken);

        return RedirectToAction(nameof(Index));
    }
}
