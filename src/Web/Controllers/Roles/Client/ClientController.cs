using IceCreamM12.Application.Interfaces;
using IceCreamM12.Web.Models.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace IceCreamM12.Web.Controllers;

[Authorize(Roles = "Client,Owner,Worker")]
public class ClientController : Controller
{
    private readonly IOrderService _orderService;

    public ClientController(IOrderService orderService)
    {
        _orderService = orderService;
    }

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
                cancellationToken);

            TempData["Success"] = $"Поръчката {order.OrderNumber} е създадена.";
            return RedirectToAction(nameof(MyOrders));
        }
        catch (Exception ex)
        {
            ModelState.AddModelError(string.Empty, ex.Message);
            return View(model);
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

}
