using System.Security.Claims;
using IceCreamM12.Application.Interfaces;
using IceCreamM12.Web.Models.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace IceCreamM12.Web.Controllers;

[Authorize(Roles = "Owner,Worker")]
public class InventoryController : Controller
{
    private readonly IInventoryService _inventoryService;
    private readonly IManagementService _managementService;

    public InventoryController(IInventoryService inventoryService, IManagementService managementService)
    {
        _inventoryService = inventoryService;
        _managementService = managementService;
    }

    [HttpGet]
    public async Task<IActionResult> Index(CancellationToken cancellationToken)
        => View(await BuildInventoryViewModelAsync(cancellationToken));

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Load(InventoryManagementViewModel model, CancellationToken cancellationToken)
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
    public async Task<IActionResult> Scrap(InventoryManagementViewModel model, CancellationToken cancellationToken)
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
    public async Task<IActionResult> Replace(InventoryManagementViewModel model, CancellationToken cancellationToken)
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

    private async Task<InventoryManagementViewModel> BuildInventoryViewModelAsync(CancellationToken cancellationToken)
        => new()
        {
            InventoryItems = await _managementService.GetInventoryItemsAsync(cancellationToken),
            Ingredients = await _managementService.GetIngredientsAsync(cancellationToken),
            RecentAudits = await _managementService.GetRecentAuditsAsync(10, cancellationToken)
        };

    private async Task<InventoryManagementViewModel> MergeWithInventoryDataAsync(InventoryManagementViewModel model, CancellationToken cancellationToken)
    {
        var populated = await BuildInventoryViewModelAsync(cancellationToken);
        populated.Load = model.Load;
        populated.Scrap = model.Scrap;
        populated.Replace = model.Replace;
        return populated;
    }

    private static bool HasSelectedItem(InventoryOperationInputModel operation)
        => operation.ItemType == InventoryEntityType.Product ? operation.ProductId.HasValue : operation.IngredientId.HasValue;

    private static bool HasSelectedFromItem(InventoryReplaceInputModel operation)
        => operation.FromItemType == InventoryEntityType.Product ? operation.FromProductId.HasValue : operation.FromIngredientId.HasValue;

    private static bool HasSelectedToItem(InventoryReplaceInputModel operation)
        => operation.ToItemType == InventoryEntityType.Product ? operation.ToProductId.HasValue : operation.ToIngredientId.HasValue;

    private async Task ExecuteWithTempDataAsync(Func<Task> action)
    {
        try
        {
            await action();
            TempData["Success"] = "Операцията е успешна.";
        }
        catch (Exception ex)
        {
            TempData["Error"] = ex.Message;
        }
    }
}
