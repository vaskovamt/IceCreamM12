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
        if (!TryValidateModel(model.Load, nameof(model.Load)) || !HasSelectedItem(model.Load))
        {
            if (!HasSelectedItem(model.Load))
            {
                ModelState.AddModelError(nameof(model.Load.ProductId), "Изберете продукт или суровина.");
            }

            return View(nameof(Index), await MergeWithInventoryDataAsync(model, cancellationToken));
        }

        await ExecuteWithTempDataAsync(async () =>
            await _inventoryService.LoadInventoryAsync(
                model.Load.ItemType == InventoryEntityType.Product ? model.Load.ProductId : null,
                model.Load.ItemType == InventoryEntityType.Ingredient ? model.Load.IngredientId : null,
                model.Load.Quantity,
                model.Load.Reason,
                User.FindFirstValue(ClaimTypes.NameIdentifier),
                cancellationToken));

        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Scrap(InventoryManagementViewModel model, CancellationToken cancellationToken)
    {
        if (!TryValidateModel(model.Scrap, nameof(model.Scrap)) || !HasSelectedItem(model.Scrap))
        {
            if (!HasSelectedItem(model.Scrap))
            {
                ModelState.AddModelError(nameof(model.Scrap.ProductId), "Изберете продукт или суровина.");
            }

            return View(nameof(Index), await MergeWithInventoryDataAsync(model, cancellationToken));
        }

        await ExecuteWithTempDataAsync(async () =>
            await _inventoryService.ScrapProductAsync(
                model.Scrap.ItemType == InventoryEntityType.Product ? model.Scrap.ProductId : null,
                model.Scrap.ItemType == InventoryEntityType.Ingredient ? model.Scrap.IngredientId : null,
                model.Scrap.Quantity,
                model.Scrap.Reason,
                User.FindFirstValue(ClaimTypes.NameIdentifier),
                cancellationToken));

        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Replace(InventoryManagementViewModel model, CancellationToken cancellationToken)
    {
        if (!TryValidateModel(model.Replace, nameof(model.Replace)) || !HasSelectedFromItem(model.Replace) || !HasSelectedToItem(model.Replace))
        {
            if (!HasSelectedFromItem(model.Replace))
            {
                ModelState.AddModelError(nameof(model.Replace.FromProductId), "Изберете източник за замяна.");
            }

            if (!HasSelectedToItem(model.Replace))
            {
                ModelState.AddModelError(nameof(model.Replace.ToProductId), "Изберете цел за замяна.");
            }

            return View(nameof(Index), await MergeWithInventoryDataAsync(model, cancellationToken));
        }

        await ExecuteWithTempDataAsync(async () =>
            await _inventoryService.SwapProductAsync(
                model.Replace.FromItemType == InventoryEntityType.Product ? model.Replace.FromProductId : null,
                model.Replace.FromItemType == InventoryEntityType.Ingredient ? model.Replace.FromIngredientId : null,
                model.Replace.ToItemType == InventoryEntityType.Product ? model.Replace.ToProductId : null,
                model.Replace.ToItemType == InventoryEntityType.Ingredient ? model.Replace.ToIngredientId : null,
                model.Replace.Quantity,
                model.Replace.Reason,
                User.FindFirstValue(ClaimTypes.NameIdentifier),
                cancellationToken));

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
