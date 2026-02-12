using System.Security.Claims;
using IceCreamM12.Application.Interfaces;
using IceCreamM12.Web.Models.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace IceCreamM12.Web.Controllers;

[Authorize(Roles = "Worker,Owner")]
[Route("Worker/[action]")]
public class WorkerController : Controller
{
    private readonly IManagementService _managementService;
    private readonly IInventoryService _inventoryService;
    private readonly IOrderService _orderService;

    public WorkerController(IManagementService managementService, IInventoryService inventoryService, IOrderService orderService)
    {
        _managementService = managementService;
        _inventoryService = inventoryService;
        _orderService = orderService;
    }

    [HttpGet]
    public async Task<IActionResult> Dashboard(CancellationToken cancellationToken)
        => View(new WorkerDashboardViewModel { Data = await _managementService.GetWorkerDashboardAsync(cancellationToken) });

    [HttpGet]
    public async Task<IActionResult> Orders(CancellationToken cancellationToken)
        => View(new OrdersManagementViewModel { Orders = await _managementService.GetPendingOrdersAsync(cancellationToken), StatusFilter = "Pending" });

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
        => View(new InventoryManagementViewModel
        {
            InventoryItems = await _managementService.GetInventoryItemsAsync(cancellationToken),
            RecentAudits = await _managementService.GetRecentAuditsAsync(10, cancellationToken)
        });

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Load(InventoryManagementViewModel model, CancellationToken cancellationToken)
    {
        await ExecuteWithTempDataAsync(async () =>
            await _inventoryService.LoadInventoryAsync(model.Load.ProductId, model.Load.Quantity, model.Load.Reason,
                User.FindFirstValue(ClaimTypes.NameIdentifier), cancellationToken));
        return RedirectToAction(nameof(Inventory));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Scrap(InventoryManagementViewModel model, CancellationToken cancellationToken)
    {
        await ExecuteWithTempDataAsync(async () =>
            await _inventoryService.ScrapProductAsync(model.Scrap.ProductId, model.Scrap.Quantity, model.Scrap.Reason,
                User.FindFirstValue(ClaimTypes.NameIdentifier), cancellationToken));
        return RedirectToAction(nameof(Inventory));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Replace(InventoryManagementViewModel model, CancellationToken cancellationToken)
    {
        await ExecuteWithTempDataAsync(async () =>
            await _inventoryService.SwapProductAsync(model.Replace.FromProductId, model.Replace.ToProductId, model.Replace.Quantity, model.Replace.Reason,
                User.FindFirstValue(ClaimTypes.NameIdentifier), cancellationToken));
        return RedirectToAction(nameof(Inventory));
    }

    [HttpGet]
    public async Task<IActionResult> DailyCheck(CancellationToken cancellationToken)
    {
        var items = (await _managementService.GetInventoryItemsAsync(cancellationToken))
            .Select(i => new DailyCheckItemInputModel
            {
                ProductId = i.ProductId,
                ProductName = i.Product?.Name ?? $"Product #{i.ProductId}",
                SystemQuantity = i.QuantityOnHand,
                CountedQuantity = i.QuantityOnHand
            }).ToList();

        return View(new DailyCheckViewModel { Items = items });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DailyCheck(DailyCheckViewModel model, CancellationToken cancellationToken)
    {
        var input = model.Items.ToDictionary(i => i.ProductId, i => i.CountedQuantity);
        model.Results = await _managementService.ExecuteDailyCheckAsync(input, User.FindFirstValue(ClaimTypes.NameIdentifier), cancellationToken);
        TempData["Success"] = "Дневната проверка е записана.";
        return View(model);
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
