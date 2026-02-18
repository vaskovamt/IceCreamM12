using System.Security.Claims;
using IceCreamM12.Application.Interfaces;
using IceCreamM12.Web.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace IceCreamM12.Web.Controllers;

[Authorize(Roles = "Owner,Worker")]
public class InventoryController : Controller
{
    private readonly IInventoryService _inventoryService;

    public InventoryController(IInventoryService inventoryService)
    {
        _inventoryService = inventoryService;
    }

    public IActionResult Index()
    {
        return View();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Load(InventoryLoadRequest request, CancellationToken cancellationToken)
    {
        await _inventoryService.LoadInventoryAsync(
            request.ProductId,
            null,
            request.Quantity,
            request.Reason,
            User.FindFirstValue(ClaimTypes.NameIdentifier),
            cancellationToken);

        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Scrap(InventoryScrapRequest request, CancellationToken cancellationToken)
    {
        await _inventoryService.ScrapProductAsync(
            request.ProductId,
            null,
            request.Quantity,
            request.Reason,
            User.FindFirstValue(ClaimTypes.NameIdentifier),
            cancellationToken);

        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Swap(InventorySwapRequest request, CancellationToken cancellationToken)
    {
        await _inventoryService.SwapProductAsync(
            request.FromProductId,
            null,
            request.ToProductId,
            null,
            request.Quantity,
            request.Reason,
            User.FindFirstValue(ClaimTypes.NameIdentifier),
            cancellationToken);

        return RedirectToAction(nameof(Index));
    }
}
