using System.Security.Claims;
using IceCreamM12.Application.Interfaces;
using IceCreamM12.Domain.Entities;
using IceCreamM12.Web.Models.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace IceCreamM12.Web.Controllers.Admin;

[Authorize(Roles = "Owner")]
[Route("Owner/[action]/{id?}")]
public class OwnerController : Controller
{
    private readonly IManagementService _managementService;
    private readonly IInventoryService _inventoryService;
    private readonly IOrderService _orderService;

    public OwnerController(IManagementService managementService, IInventoryService inventoryService, IOrderService orderService)
    {
        _managementService = managementService;
        _inventoryService = inventoryService;
        _orderService = orderService;
    }

    [HttpGet]
    public async Task<IActionResult> Dashboard(CancellationToken cancellationToken)
        => View(new OwnerDashboardViewModel { Data = await _managementService.GetOwnerDashboardAsync(cancellationToken) });

    [HttpGet]
    public async Task<IActionResult> Orders(string? status, CancellationToken cancellationToken)
        => View(new OrdersManagementViewModel
        {
            StatusFilter = status,
            Orders = await _managementService.GetOrdersAsync(status, cancellationToken)
        });

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ApproveOrder(int orderId, CancellationToken cancellationToken)
    {
        await ExecuteWithTempDataAsync(async () =>
            await _orderService.ApproveOrderAsync(orderId, User.FindFirstValue(ClaimTypes.NameIdentifier), cancellationToken));

        return RedirectToAction(nameof(Orders));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> RejectOrder(int orderId, string rejectionReason, CancellationToken cancellationToken)
    {
        await ExecuteWithTempDataAsync(async () =>
            await _orderService.RejectOrderAsync(orderId, rejectionReason, User.FindFirstValue(ClaimTypes.NameIdentifier), cancellationToken));

        return RedirectToAction(nameof(Orders));
    }

    [HttpGet]
    public async Task<IActionResult> Inventory(CancellationToken cancellationToken)
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
            model = await BuildInventoryViewModelAsync(cancellationToken);
            return View(nameof(Inventory), model);
        }

        await ExecuteWithTempDataAsync(async () =>
            await _inventoryService.LoadInventoryAsync(
                model.Load.ItemType == InventoryEntityType.Product ? model.Load.ProductId : null,
                model.Load.ItemType == InventoryEntityType.Ingredient ? model.Load.IngredientId : null,
                model.Load.Quantity,
                model.Load.Reason,
                User.FindFirstValue(ClaimTypes.NameIdentifier),
                cancellationToken));

        return RedirectToAction(nameof(Inventory));
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
            model = await BuildInventoryViewModelAsync(cancellationToken);
            return View(nameof(Inventory), model);
        }

        await ExecuteWithTempDataAsync(async () =>
            await _inventoryService.ScrapProductAsync(
                model.Scrap.ItemType == InventoryEntityType.Product ? model.Scrap.ProductId : null,
                model.Scrap.ItemType == InventoryEntityType.Ingredient ? model.Scrap.IngredientId : null,
                model.Scrap.Quantity,
                model.Scrap.Reason,
                User.FindFirstValue(ClaimTypes.NameIdentifier),
                cancellationToken));

        return RedirectToAction(nameof(Inventory));
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
            model = await BuildInventoryViewModelAsync(cancellationToken);
            return View(nameof(Inventory), model);
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

        return RedirectToAction(nameof(Inventory));
    }


    [HttpGet]
    public async Task<IActionResult> Users(CancellationToken cancellationToken)
        => View(new UserManagementViewModel
        {
            Users = await _managementService.GetUsersWithRolesAsync(cancellationToken)
        });

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> PromoteToWorker(string userId, CancellationToken cancellationToken)
    {
        await ExecuteWithTempDataAsync(async () =>
            await _managementService.PromoteToWorkerAsync(userId, cancellationToken));

        return RedirectToAction(nameof(Users));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DemoteToClient(string userId, CancellationToken cancellationToken)
    {
        await ExecuteWithTempDataAsync(async () =>
            await _managementService.DemoteToClientAsync(userId, cancellationToken));

        return RedirectToAction(nameof(Users));
    }

    [HttpGet]
    public async Task<IActionResult> Products(CancellationToken cancellationToken)
        => View("Products/Index", await _managementService.GetProductsAsync(cancellationToken));

    [HttpGet]
    public async Task<IActionResult> ProductDetails(int id, CancellationToken cancellationToken)
    {
        var product = await _managementService.GetProductByIdAsync(id, cancellationToken);
        return product is null ? NotFound() : View("Products/Details", product);
    }

    [HttpGet]
    public async Task<IActionResult> CreateProduct(CancellationToken cancellationToken)
    {
        await LoadCategoriesAsync(cancellationToken);
        return View("Products/Create", new Product());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateProduct(Product product, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            await LoadCategoriesAsync(cancellationToken);
            return View("Products/Create", product);
        }

        await _managementService.CreateProductAsync(product, cancellationToken);
        TempData["Success"] = "Продуктът е създаден успешно.";
        return RedirectToAction(nameof(Products));
    }

    [HttpGet]
    public async Task<IActionResult> EditProduct(int id, CancellationToken cancellationToken)
    {
        var product = await _managementService.GetProductByIdAsync(id, cancellationToken);
        if (product is null) return NotFound();

        await LoadCategoriesAsync(cancellationToken);
        return View("Products/Edit", product);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> EditProduct(Product product, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            await LoadCategoriesAsync(cancellationToken);
            return View("Products/Edit", product);
        }

        await _managementService.UpdateProductAsync(product, cancellationToken);
        TempData["Success"] = "Продуктът е обновен.";
        return RedirectToAction(nameof(Products));
    }

    [HttpGet]
    public async Task<IActionResult> DeleteProduct(int id, CancellationToken cancellationToken)
    {
        var product = await _managementService.GetProductByIdAsync(id, cancellationToken);
        return product is null ? NotFound() : View("Products/Delete", product);
    }

    [HttpPost, ActionName("DeleteProduct")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteProductConfirmed(int id, CancellationToken cancellationToken)
    {
        await _managementService.DeleteProductAsync(id, cancellationToken);
        TempData["Success"] = "Продуктът е изтрит.";
        return RedirectToAction(nameof(Products));
    }

    [HttpGet]
    public async Task<IActionResult> Ingredients(CancellationToken cancellationToken)
        => View("Ingredients/Index", await _managementService.GetIngredientsAsync(cancellationToken));

    [HttpGet]
    public IActionResult CreateIngredient() => View("Ingredients/Create", new Ingredient());

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateIngredient(Ingredient ingredient, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid) return View("Ingredients/Create", ingredient);
        await _managementService.CreateIngredientAsync(ingredient, cancellationToken);
        TempData["Success"] = "Суровината е създадена.";
        return RedirectToAction(nameof(Ingredients));
    }

    [HttpGet]
    public async Task<IActionResult> IngredientDetails(int id, CancellationToken cancellationToken)
    {
        var ingredient = await _managementService.GetIngredientByIdAsync(id, cancellationToken);
        return ingredient is null ? NotFound() : View("Ingredients/Details", ingredient);
    }

    [HttpGet]
    public async Task<IActionResult> EditIngredient(int id, CancellationToken cancellationToken)
    {
        var ingredient = await _managementService.GetIngredientByIdAsync(id, cancellationToken);
        return ingredient is null ? NotFound() : View("Ingredients/Edit", ingredient);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> EditIngredient(Ingredient ingredient, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid) return View("Ingredients/Edit", ingredient);
        await _managementService.UpdateIngredientAsync(ingredient, cancellationToken);
        TempData["Success"] = "Суровината е обновена.";
        return RedirectToAction(nameof(Ingredients));
    }

    [HttpGet]
    public async Task<IActionResult> DeleteIngredient(int id, CancellationToken cancellationToken)
    {
        var ingredient = await _managementService.GetIngredientByIdAsync(id, cancellationToken);
        return ingredient is null ? NotFound() : View("Ingredients/Delete", ingredient);
    }

    [HttpPost, ActionName("DeleteIngredient")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteIngredientConfirmed(int id, CancellationToken cancellationToken)
    {
        await _managementService.DeleteIngredientAsync(id, cancellationToken);
        TempData["Success"] = "Суровината е изтрита.";
        return RedirectToAction(nameof(Ingredients));
    }


    private static bool HasSelectedItem(InventoryOperationInputModel operation)
        => operation.ItemType == InventoryEntityType.Product ? operation.ProductId.HasValue : operation.IngredientId.HasValue;

    private static bool HasSelectedFromItem(InventoryReplaceInputModel operation)
        => operation.FromItemType == InventoryEntityType.Product ? operation.FromProductId.HasValue : operation.FromIngredientId.HasValue;

    private static bool HasSelectedToItem(InventoryReplaceInputModel operation)
        => operation.ToItemType == InventoryEntityType.Product ? operation.ToProductId.HasValue : operation.ToIngredientId.HasValue;

    private async Task<InventoryManagementViewModel> BuildInventoryViewModelAsync(CancellationToken cancellationToken)
        => new()
        {
            InventoryItems = await _managementService.GetInventoryItemsAsync(cancellationToken),
            Ingredients = await _managementService.GetIngredientsAsync(cancellationToken),
            RecentAudits = await _managementService.GetRecentAuditsAsync(15, cancellationToken)
        };

    private async Task LoadCategoriesAsync(CancellationToken cancellationToken)
    {
        var categories = await _managementService.GetCategoriesAsync(cancellationToken);
        ViewBag.CategoryOptions = categories.Select(c => new SelectListItem(c.Name, c.Id.ToString()));
    }

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
