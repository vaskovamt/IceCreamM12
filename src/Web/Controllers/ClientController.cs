using IceCreamM12.Domain.Entities;
using IceCreamM12.Infrastructure.Data;
using IceCreamM12.Web.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace IceCreamM12.Web.Controllers;

[Authorize]
public class ClientController : Controller
{
    private readonly ApplicationDbContext _dbContext;

    public ClientController(ApplicationDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<IActionResult> MyOrders(CancellationToken cancellationToken)
    {
        string? email = User.Identity?.Name;

        var orders = await _dbContext.Orders
            .Include(order => order.Items)
            .ThenInclude(item => item.Product)
            .Where(order => order.CustomerEmail == email)
            .OrderByDescending(order => order.OrderedAt)
            .ToListAsync(cancellationToken);

        return View(orders);
    }

    [HttpGet]
    public async Task<IActionResult> NewOrder(CancellationToken cancellationToken)
    {
        await LoadProductsAsync(cancellationToken);

        return View(new ClientOrderRequest
        {
            CustomerEmail = User.Identity?.Name
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> NewOrder(ClientOrderRequest request, CancellationToken cancellationToken)
    {
        Product? product = await _dbContext.Products
            .AsNoTracking()
            .FirstOrDefaultAsync(candidate => candidate.Id == request.ProductId, cancellationToken);

        if (product is null)
        {
            ModelState.AddModelError(nameof(request.ProductId), "Please select a valid product.");
        }

        if (!ModelState.IsValid)
        {
            await LoadProductsAsync(cancellationToken);
            return View(request);
        }

        string customerEmail = request.CustomerEmail?.Trim() ?? User.Identity?.Name ?? string.Empty;
        string customerName = request.CustomerName?.Trim();

        if (string.IsNullOrWhiteSpace(customerName))
        {
            customerName = customerEmail;
        }

        var order = new Order
        {
            OrderNumber = $"ORD-{DateTime.UtcNow:yyyyMMddHHmmss}-{Random.Shared.Next(100, 1000)}",
            OrderedAt = DateTime.UtcNow,
            Status = "Pending",
            CustomerName = customerName,
            CustomerEmail = customerEmail,
            TotalAmount = product!.Price * request.Quantity,
            Items =
            [
                new OrderItem
                {
                    ProductId = product.Id,
                    Quantity = request.Quantity,
                    UnitPrice = product.Price
                }
            ]
        };

        _dbContext.Orders.Add(order);
        await _dbContext.SaveChangesAsync(cancellationToken);

        TempData["OrderSuccess"] = $"Order {order.OrderNumber} was submitted.";
        return RedirectToAction(nameof(MyOrders));
    }

    public IActionResult Profile()
    {
        return View();
    }

    private async Task LoadProductsAsync(CancellationToken cancellationToken)
    {
        var products = await _dbContext.Products
            .OrderBy(product => product.Name)
            .Select(product => new SelectListItem
            {
                Value = product.Id.ToString(),
                Text = $"{product.Name} - {product.Price:F2} lv"
            })
            .ToListAsync(cancellationToken);

        ViewBag.ProductOptions = products;
    }
}
