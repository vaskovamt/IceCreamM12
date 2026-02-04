using System.Security.Claims;
using IceCreamM12.Application.Interfaces;
using IceCreamM12.Web.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace IceCreamM12.Web.Controllers;

[Authorize(Roles = "Owner,Worker")]
public class ProductController : Controller
{
    private readonly IProductService _productService;

    public ProductController(IProductService productService)
    {
        _productService = productService;
    }

    public IActionResult Index()
    {
        return View();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Scrap(ProductScrapRequest request, CancellationToken cancellationToken)
    {
        await _productService.ScrapProductAsync(
            request.ProductId,
            request.Quantity,
            request.Reason,
            User.FindFirstValue(ClaimTypes.NameIdentifier),
            cancellationToken);

        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Replace(ProductReplaceRequest request, CancellationToken cancellationToken)
    {
        await _productService.ReplaceProductAsync(
            request.OriginalProductId,
            request.ReplacementProductId,
            request.Quantity,
            request.Reason,
            User.FindFirstValue(ClaimTypes.NameIdentifier),
            cancellationToken);

        return RedirectToAction(nameof(Index));
    }
}
