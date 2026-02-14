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

    [HttpGet]
    public async Task<IActionResult> MyOrders(CancellationToken cancellationToken)
    {
        var email = User.Identity?.Name;
        if (string.IsNullOrWhiteSpace(email)) return Challenge();

        var orders = await _orderService.GetOrdersByCustomerEmailAsync(email, cancellationToken);
        return View(new MyOrdersViewModel { Orders = orders });
    }

    [HttpGet]
    public async Task<IActionResult> NewOrder(CancellationToken cancellationToken)
    {
        var model = new NewOrderViewModel
        {
            CustomerEmail = User.Identity?.Name,
            AvailableProducts = await _orderService.GetAvailableProductsAsync(cancellationToken)
        };

        PopulateOrderItems(model);
        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> NewOrder(NewOrderViewModel model, CancellationToken cancellationToken)
    {
        model.AvailableProducts = await _orderService.GetAvailableProductsAsync(cancellationToken);
        PopulateOrderItems(model);

        ValidateOrderInput(model);
        if (!ModelState.IsValid)
        {
            if (model.Items.Count == 0)
            {
                model.Items.Add(new NewOrderItemViewModel());
            }

            return View(model);
        }

        try
        {
            var customerEmail = string.IsNullOrWhiteSpace(model.CustomerEmail)
                ? User.Identity?.Name ?? string.Empty
                : model.CustomerEmail.Trim();

            var customerName = string.IsNullOrWhiteSpace(model.CustomerName)
                ? customerEmail
                : model.CustomerName.Trim();

            var orderItems = model.Items
                .Where(i => i.Quantity > 0)
                .GroupBy(i => i.ProductId)
                .Select(group => new OrderProductRequest(group.Key, group.Sum(item => item.Quantity)))
                .ToList();

            var order = await _orderService.CreatePendingOrderAsync(
                orderItems,
                customerName,
                customerEmail,
                model.CompanyEik.Trim(),
                model.InvoiceAddress.Trim(),
                model.PaymentMethod,
                model.VatNumber,
                model.ContactPhone,
                cancellationToken);

            await _orderService.ApproveOrderAsync(order.Id, User.FindFirstValue(ClaimTypes.NameIdentifier), cancellationToken);

            TempData["Success"] = $"Поръчката {order.OrderNumber} е създадена и одобрена.";
            return RedirectToAction(nameof(MyOrders));
        }
        catch (Exception ex)
        {
            ModelState.AddModelError(string.Empty, ex.Message);
            return View(model);
        }
    }

    [HttpGet("Worker/EditOrder/{orderId:int}")]
    public async Task<IActionResult> EditOrder(int orderId, CancellationToken cancellationToken)
    {
        var order = await _orderService.GetOrderByIdAsync(orderId, cancellationToken);
        if (order is null)
        {
            TempData["Error"] = "Поръчката не е намерена.";
            return RedirectToAction(nameof(MyOrders));
        }

        if (!CanManageOrder(order))
        {
            return Forbid();
        }

        var model = new NewOrderViewModel
        {
            CustomerName = order.CustomerName,
            CustomerEmail = order.CustomerEmail,
            CompanyEik = order.CompanyEik,
            InvoiceAddress = order.InvoiceAddress,
            PaymentMethod = order.PaymentMethod,
            VatNumber = order.VatNumber,
            ContactPhone = order.ContactPhone,
            AvailableProducts = await _orderService.GetAvailableProductsAsync(cancellationToken),
            Items = order.Items.Select(item => new NewOrderItemViewModel
            {
                ProductId = item.ProductId,
                Quantity = item.Quantity
            }).ToList()
        };

        PopulateOrderItems(model);
        ViewData["OrderId"] = orderId;
        ViewData["OrderNumber"] = order.OrderNumber;
        return View(model);
    }

    [HttpPost("Worker/EditOrder/{orderId:int}")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> EditOrder(int orderId, NewOrderViewModel model, CancellationToken cancellationToken)
    {
        var existingOrder = await _orderService.GetOrderByIdAsync(orderId, cancellationToken);
        if (existingOrder is null)
        {
            TempData["Error"] = "Поръчката не е намерена.";
            return RedirectToAction(nameof(MyOrders));
        }

        if (!CanManageOrder(existingOrder))
        {
            return Forbid();
        }

        model.AvailableProducts = await _orderService.GetAvailableProductsAsync(cancellationToken);
        PopulateOrderItems(model);

        ValidateOrderInput(model);
        if (!ModelState.IsValid)
        {
            ViewData["OrderId"] = orderId;
            ViewData["OrderNumber"] = existingOrder.OrderNumber;
            return View(model);
        }

        var orderItems = model.Items
            .Where(i => i.Quantity > 0)
            .GroupBy(i => i.ProductId)
            .Select(group => new OrderProductRequest(group.Key, group.Sum(item => item.Quantity)))
            .ToList();

        try
        {
            await _orderService.UpdateOrderAsync(
                orderId,
                orderItems,
                string.IsNullOrWhiteSpace(model.CustomerName) ? model.CustomerEmail?.Trim() ?? string.Empty : model.CustomerName.Trim(),
                string.IsNullOrWhiteSpace(model.CustomerEmail) ? User.Identity?.Name ?? string.Empty : model.CustomerEmail.Trim(),
                model.CompanyEik.Trim(),
                model.InvoiceAddress.Trim(),
                model.PaymentMethod,
                model.VatNumber,
                model.ContactPhone,
                User.FindFirstValue(ClaimTypes.NameIdentifier),
                cancellationToken);

            TempData["Success"] = $"Поръчка {existingOrder.OrderNumber} е обновена успешно.";
            return RedirectToAction(nameof(MyOrders));
        }
        catch (Exception ex)
        {
            ModelState.AddModelError(string.Empty, ex.Message);
            ViewData["OrderId"] = orderId;
            ViewData["OrderNumber"] = existingOrder.OrderNumber;
            return View(model);
        }
    }

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

    private static void PopulateOrderItems(NewOrderViewModel model)
    {
        var existingItems = model.Items.ToDictionary(item => item.ProductId, item => item.Quantity);
        model.Items = model.AvailableProducts
            .Select(product => new NewOrderItemViewModel
            {
                ProductId = product.Id,
                Quantity = existingItems.GetValueOrDefault(product.Id, 0)
            })
            .ToList();
    }

    private void ValidateOrderInput(NewOrderViewModel model)
    {
        if (!model.Items.Any(item => item.Quantity > 0))
        {
            ModelState.AddModelError(nameof(model.Items), "Добавете количество за поне един продукт.");
        }

        foreach (var item in model.Items.Select((value, index) => new { value, index }))
        {
            if (item.value.Quantity < 0)
            {
                ModelState.AddModelError($"Items[{item.index}].Quantity", "Количеството не може да е отрицателно.");
                continue;
            }

            if (item.value.Quantity == 0)
            {
                continue;
            }

            var selectedProduct = model.AvailableProducts.FirstOrDefault(p => p.Id == item.value.ProductId);
            if (selectedProduct?.InventoryItem is null)
            {
                ModelState.AddModelError($"Items[{item.index}].ProductId", "Моля, изберете наличен продукт.");
                continue;
            }

            if (item.value.Quantity > selectedProduct.InventoryItem.QuantityOnHand)
            {
                ModelState.AddModelError($"Items[{item.index}].Quantity", $"Налични са само {selectedProduct.InventoryItem.QuantityOnHand} бр. за {selectedProduct.Name}.");
            }
        }

        string[] allowedPaymentMethods = ["По банков път", "В брой", "С карта"];
        if (!allowedPaymentMethods.Contains(model.PaymentMethod))
        {
            ModelState.AddModelError(nameof(model.PaymentMethod), "Изберете валиден начин на плащане.");
        }
    }

    private bool CanManageOrder(Domain.Entities.Order order)
    {
        var email = User.Identity?.Name;
        return !string.IsNullOrWhiteSpace(email)
               && string.Equals(order.CustomerEmail, email, StringComparison.OrdinalIgnoreCase);
    }
}
