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
    public async Task<IActionResult> OrderHistory(CancellationToken cancellationToken)
        => View(new MyOrdersViewModel { Orders = await _managementService.GetOrdersAsync(null, cancellationToken) });

    [HttpGet]
    public Task<IActionResult> MyOrders(CancellationToken cancellationToken)
        => Task.FromResult<IActionResult>(RedirectToAction(nameof(OrderHistory)));

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
            var customerEmail = User.Identity?.Name?.Trim() ?? string.Empty;

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
                model.CompanyEik?.Trim() ?? string.Empty,
                model.InvoiceAddress?.Trim() ?? string.Empty,
                model.PaymentMethod,
                model.VatNumber,
                model.ContactPhone,
                cancellationToken);

            await _orderService.ApproveOrderAsync(order.Id, User.FindFirstValue(ClaimTypes.NameIdentifier), cancellationToken);

            TempData["Success"] = $"Поръчката {order.OrderNumber} е създадена и одобрена.";
            return RedirectToAction(nameof(OrderHistory));
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
            return RedirectToAction(nameof(OrderHistory));
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
            IsBusinessOrder = !string.IsNullOrWhiteSpace(order.CompanyEik) || !string.IsNullOrWhiteSpace(order.InvoiceAddress),
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
            return RedirectToAction(nameof(OrderHistory));
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
                string.IsNullOrWhiteSpace(model.CustomerName) ? User.Identity?.Name?.Trim() ?? string.Empty : model.CustomerName.Trim(),
                User.Identity?.Name?.Trim() ?? string.Empty,
                model.CompanyEik?.Trim() ?? string.Empty,
                model.InvoiceAddress?.Trim() ?? string.Empty,
                model.PaymentMethod,
                model.VatNumber,
                model.ContactPhone,
                User.FindFirstValue(ClaimTypes.NameIdentifier),
                cancellationToken);

            TempData["Success"] = $"Поръчка {existingOrder.OrderNumber} е обновена успешно.";
            return RedirectToAction(nameof(OrderHistory));
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
            Ingredients = await _managementService.GetIngredientsAsync(cancellationToken),
            RecentAudits = await _managementService.GetRecentAuditsAsync(10, cancellationToken)
        });

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

            model = new InventoryManagementViewModel
            {
                InventoryItems = await _managementService.GetInventoryItemsAsync(cancellationToken),
                Ingredients = await _managementService.GetIngredientsAsync(cancellationToken),
                RecentAudits = await _managementService.GetRecentAuditsAsync(10, cancellationToken),
                Load = model.Load,
                Scrap = model.Scrap,
                Replace = model.Replace
            };

            return View(nameof(Inventory), model);
        }

        await ExecuteWithTempDataAsync(async () =>
            await _inventoryService.LoadInventoryAsync(model.Load.ProductId, null, model.Load.Quantity, model.Load.Reason,
                User.FindFirstValue(ClaimTypes.NameIdentifier), cancellationToken));
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

            model = new InventoryManagementViewModel
            {
                InventoryItems = await _managementService.GetInventoryItemsAsync(cancellationToken),
                Ingredients = await _managementService.GetIngredientsAsync(cancellationToken),
                RecentAudits = await _managementService.GetRecentAuditsAsync(10, cancellationToken),
                Load = model.Load,
                Scrap = model.Scrap,
                Replace = model.Replace
            };

            return View(nameof(Inventory), model);
        }

        await ExecuteWithTempDataAsync(async () =>
            await _inventoryService.ScrapProductAsync(model.Scrap.ProductId, null, model.Scrap.Quantity, model.Scrap.Reason,
                User.FindFirstValue(ClaimTypes.NameIdentifier), cancellationToken));
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

            model = new InventoryManagementViewModel
            {
                InventoryItems = await _managementService.GetInventoryItemsAsync(cancellationToken),
                Ingredients = await _managementService.GetIngredientsAsync(cancellationToken),
                RecentAudits = await _managementService.GetRecentAuditsAsync(10, cancellationToken),
                Load = model.Load,
                Scrap = model.Scrap,
                Replace = model.Replace
            };

            return View(nameof(Inventory), model);
        }

        await ExecuteWithTempDataAsync(async () =>
            await _inventoryService.SwapProductAsync(model.Replace.FromProductId, null, model.Replace.ToProductId, null, model.Replace.Quantity, model.Replace.Reason,
                User.FindFirstValue(ClaimTypes.NameIdentifier), cancellationToken));
        return RedirectToAction(nameof(Inventory));
    }


    [HttpGet]
    public async Task<IActionResult> Production(CancellationToken cancellationToken)
    {
        var ingredients = await _managementService.GetIngredientsAsync(cancellationToken);
        var products = (await _managementService.GetInventoryItemsAsync(cancellationToken))
            .Where(item => !IsConeProduct(item.Product?.Name))
            .ToList();

        return View(new ProductionBatchViewModel
        {
            IngredientInputs = ingredients.Select(i => new IngredientProductionInputModel
            {
                IngredientId = i.Id,
                IngredientName = i.Name,
                Unit = i.Unit,
                AvailableQuantity = i.QuantityOnHand,
                UsedQuantity = 0
            }).ToList(),
            ProductInputs = products.Select(i => new ProductProductionInputModel
            {
                ProductId = i.ProductId,
                ProductName = i.Product?.Name ?? $"Product #{i.ProductId}",
                AvailableQuantity = i.QuantityOnHand,
                ProducedQuantity = 0
            }).ToList()
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Production(ProductionBatchViewModel model, CancellationToken cancellationToken)
    {
        var ingredientMap = (await _managementService.GetIngredientsAsync(cancellationToken))
            .ToDictionary(i => i.Id);
        var productMap = (await _managementService.GetInventoryItemsAsync(cancellationToken))
            .Where(item => !IsConeProduct(item.Product?.Name))
            .ToDictionary(i => i.ProductId);

        var ingredientInputs = model.IngredientInputs
            .Where(i => i.UsedQuantity > 0)
            .ToList();
        var productInputs = model.ProductInputs
            .Where(p => p.ProducedQuantity > 0)
            .ToList();

        if (ingredientInputs.Count == 0)
        {
            ModelState.AddModelError(string.Empty, "Въведете използвано количество за поне една суровина.");
        }

        if (productInputs.Count == 0)
        {
            ModelState.AddModelError(string.Empty, "Въведете произведено количество за поне един продукт.");
        }

        foreach (var input in ingredientInputs)
        {
            if (!ingredientMap.TryGetValue(input.IngredientId, out var ingredient))
            {
                ModelState.AddModelError(string.Empty, $"Невалидна суровина с ID {input.IngredientId}.");
                continue;
            }

            if (input.UsedQuantity > ingredient.QuantityOnHand)
            {
                ModelState.AddModelError(string.Empty,
                    $"Недостатъчна наличност за {ingredient.Name}. Налични: {ingredient.QuantityOnHand} {ingredient.Unit}.");
            }
        }

        foreach (var input in productInputs)
        {
            if (!productMap.ContainsKey(input.ProductId))
            {
                ModelState.AddModelError(string.Empty, $"Невалиден продукт с ID {input.ProductId}.");
            }
        }

        if (!ModelState.IsValid)
        {
            model.IngredientInputs = ingredientMap.Values.Select(i =>
            {
                var existing = model.IngredientInputs.FirstOrDefault(x => x.IngredientId == i.Id);
                return new IngredientProductionInputModel
                {
                    IngredientId = i.Id,
                    IngredientName = i.Name,
                    Unit = i.Unit,
                    AvailableQuantity = i.QuantityOnHand,
                    UsedQuantity = existing?.UsedQuantity ?? 0
                };
            }).ToList();

            model.ProductInputs = productMap.Values.Select(p =>
            {
                var existing = model.ProductInputs.FirstOrDefault(x => x.ProductId == p.ProductId);
                return new ProductProductionInputModel
                {
                    ProductId = p.ProductId,
                    ProductName = p.Product?.Name ?? $"Product #{p.ProductId}",
                    AvailableQuantity = p.QuantityOnHand,
                    ProducedQuantity = existing?.ProducedQuantity ?? 0
                };
            }).ToList();

            return View(model);
        }

        try
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            foreach (var input in ingredientInputs)
            {
                var ingredient = ingredientMap[input.IngredientId];
                ingredient.QuantityOnHand -= input.UsedQuantity;
                ingredient.LastUpdatedAt = DateTime.UtcNow;
            }

            foreach (var ingredient in ingredientInputs.Select(i => ingredientMap[i.IngredientId]))
            {
                await _managementService.UpdateIngredientAsync(ingredient, cancellationToken);
            }

            foreach (var input in productInputs)
            {
                await _inventoryService.LoadInventoryAsync(
                    input.ProductId,
                    null,
                    input.ProducedQuantity,
                    "Производство",
                    userId,
                    cancellationToken);
            }

            TempData["Success"] = "Операция „Производство“ е записана успешно.";
            return RedirectToAction(nameof(Production));
        }
        catch (Exception ex)
        {
            ModelState.AddModelError(string.Empty, ex.Message);

            model.IngredientInputs = ingredientMap.Values.Select(i =>
            {
                var existing = model.IngredientInputs.FirstOrDefault(x => x.IngredientId == i.Id);
                return new IngredientProductionInputModel
                {
                    IngredientId = i.Id,
                    IngredientName = i.Name,
                    Unit = i.Unit,
                    AvailableQuantity = i.QuantityOnHand,
                    UsedQuantity = existing?.UsedQuantity ?? 0
                };
            }).ToList();

            model.ProductInputs = productMap.Values.Select(p =>
            {
                var existing = model.ProductInputs.FirstOrDefault(x => x.ProductId == p.ProductId);
                return new ProductProductionInputModel
                {
                    ProductId = p.ProductId,
                    ProductName = p.Product?.Name ?? $"Product #{p.ProductId}",
                    AvailableQuantity = p.QuantityOnHand,
                    ProducedQuantity = existing?.ProducedQuantity ?? 0
                };
            }).ToList();

            return View(model);
        }
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

        var ingredientItems = (await _managementService.GetIngredientsAsync(cancellationToken))
            .Select(i => new IngredientDailyCheckItemInputModel
            {
                IngredientId = i.Id,
                IngredientName = i.Name,
                Unit = i.Unit,
                SystemQuantity = i.QuantityOnHand,
                CountedQuantity = i.QuantityOnHand
            }).ToList();

        return View(new DailyCheckViewModel { Items = items, IngredientItems = ingredientItems });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DailyCheck(DailyCheckViewModel model, CancellationToken cancellationToken)
    {
        var input = model.Items.ToDictionary(i => i.ProductId, i => i.CountedQuantity);
        var ingredientInput = model.IngredientItems.ToDictionary(i => i.IngredientId, i => i.CountedQuantity);
        var (productResults, ingredientResults) = await _managementService.ExecuteDailyCheckAsync(input, ingredientInput, User.FindFirstValue(ClaimTypes.NameIdentifier), cancellationToken);
        model.Results = productResults;
        model.IngredientResults = ingredientResults;
        TempData["Success"] = "Дневната проверка е записана.";
        return View(model);
    }


    private static bool IsConeProduct(string? productName)
        => !string.IsNullOrWhiteSpace(productName)
           && productName.Contains("Фунийка", StringComparison.OrdinalIgnoreCase);


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

        if (model.IsBusinessOrder)
        {
            if (string.IsNullOrWhiteSpace(model.CompanyEik))
            {
                ModelState.AddModelError(nameof(model.CompanyEik), "ЕИК е задължителен за поръчка за бизнес.");
            }

            if (string.IsNullOrWhiteSpace(model.InvoiceAddress))
            {
                ModelState.AddModelError(nameof(model.InvoiceAddress), "Адрес за фактура е задължителен за поръчка за бизнес.");
            }
        }
    }

    private bool CanManageOrder(Domain.Entities.Order order)
    {
        var email = User.Identity?.Name;
        return !string.IsNullOrWhiteSpace(email)
               && string.Equals(order.CustomerEmail, email, StringComparison.OrdinalIgnoreCase);
    }
}
