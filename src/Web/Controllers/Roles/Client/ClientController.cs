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

        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> NewOrder(NewOrderViewModel model, CancellationToken cancellationToken)
    {
        model.AvailableProducts = await _orderService.GetAvailableProductsAsync(cancellationToken);

        if (model.Quantity <= 0)
        {
            ModelState.AddModelError(nameof(model.Quantity), "Количеството трябва да е по-голямо от 0.");
        }

        var selectedProduct = model.AvailableProducts.FirstOrDefault(p => p.Id == model.ProductId);
        if (selectedProduct?.InventoryItem is null)
        {
            ModelState.AddModelError(nameof(model.ProductId), "Моля, изберете наличен продукт.");
        }
        else if (model.Quantity > selectedProduct.InventoryItem.QuantityOnHand)
        {
            ModelState.AddModelError(nameof(model.Quantity), $"Налични са само {selectedProduct.InventoryItem.QuantityOnHand} бр.");
        }

        if (!ModelState.IsValid)
        {
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

            var order = await _orderService.CreatePendingOrderAsync(
                model.ProductId,
                model.Quantity,
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
}
