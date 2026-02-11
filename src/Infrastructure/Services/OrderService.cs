using IceCreamM12.Application.Interfaces;
using IceCreamM12.Domain.Entities;
using IceCreamM12.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace IceCreamM12.Infrastructure.Services;

public class OrderService : IOrderService
{
    private readonly ApplicationDbContext _dbContext;
    private readonly IAuditService _auditService;

    public OrderService(ApplicationDbContext dbContext, IAuditService auditService)
    {
        _dbContext = dbContext;
        _auditService = auditService;
    }

    public Task<List<Product>> GetAvailableProductsAsync(CancellationToken cancellationToken)
        => _dbContext.Products
            .Include(p => p.InventoryItem)
            .Where(p => p.InventoryItem != null && p.InventoryItem.QuantityOnHand > 0)
            .OrderBy(p => p.Name)
            .ToListAsync(cancellationToken);

    public Task<List<Order>> GetOrdersByCustomerEmailAsync(string customerEmail, CancellationToken cancellationToken)
        => _dbContext.Orders
            .Include(o => o.Items)
            .ThenInclude(i => i.Product)
            .Where(o => o.CustomerEmail == customerEmail)
            .OrderByDescending(o => o.OrderedAt)
            .ToListAsync(cancellationToken);

    public async Task<Order> CreatePendingOrderAsync(
        int productId,
        int quantity,
        string customerName,
        string customerEmail,
        CancellationToken cancellationToken)
    {
        if (quantity <= 0)
        {
            throw new InvalidOperationException("Quantity must be greater than zero.");
        }

        var product = await _dbContext.Products
            .Include(p => p.InventoryItem)
            .FirstOrDefaultAsync(p => p.Id == productId, cancellationToken);

        if (product?.InventoryItem is null || product.InventoryItem.QuantityOnHand <= 0)
        {
            throw new InvalidOperationException("Продуктът не е наличен.");
        }

        if (quantity > product.InventoryItem.QuantityOnHand)
        {
            throw new InvalidOperationException($"Налични са само {product.InventoryItem.QuantityOnHand} бр.");
        }

        var order = new Order
        {
            OrderNumber = $"ORD-{DateTime.UtcNow:yyyyMMddHHmmss}-{Random.Shared.Next(100, 1000)}",
            OrderedAt = DateTime.UtcNow,
            Status = "Pending",
            CustomerName = customerName,
            CustomerEmail = customerEmail,
            TotalAmount = product.Price * quantity,
            Items =
            [
                new OrderItem
                {
                    ProductId = product.Id,
                    Quantity = quantity,
                    UnitPrice = product.Price
                }
            ]
        };

        _dbContext.Orders.Add(order);
        await _dbContext.SaveChangesAsync(cancellationToken);
        return order;
    }

    public async Task ApproveOrderAsync(
        int orderId,
        string? performedByUserId,
        CancellationToken cancellationToken)
    {
        var order = await _dbContext.Orders
            .Include(orderEntity => orderEntity.Items)
            .ThenInclude(item => item.Product)
            .ThenInclude(product => product!.InventoryItem)
            .FirstOrDefaultAsync(orderEntity => orderEntity.Id == orderId, cancellationToken);

        if (order is null)
        {
            throw new InvalidOperationException("Order not found.");
        }

        if (!string.Equals(order.Status, "Pending", StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException("Only pending orders can be approved.");
        }

        foreach (var item in order.Items)
        {
            if (item.Product?.InventoryItem is null)
            {
                throw new InvalidOperationException($"Inventory not found for product {item.ProductId}.");
            }

            if (item.Product.InventoryItem.QuantityOnHand < item.Quantity)
            {
                throw new InvalidOperationException($"Insufficient inventory for product {item.ProductId}.");
            }
        }

        foreach (var item in order.Items)
        {
            var inventoryItem = item.Product!.InventoryItem!;
            inventoryItem.QuantityOnHand -= item.Quantity;
            inventoryItem.LastUpdatedAt = DateTime.UtcNow;

            await _auditService.RecordInventoryChangeAsync(
                inventoryItem,
                -item.Quantity,
                $"Order {order.OrderNumber} approved",
                performedByUserId,
                cancellationToken);
        }

        order.Status = "Approved";
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task RejectOrderAsync(
        int orderId,
        string rejectionReason,
        string? performedByUserId,
        CancellationToken cancellationToken)
    {
        var order = await _dbContext.Orders
            .FirstOrDefaultAsync(orderEntity => orderEntity.Id == orderId, cancellationToken);

        if (order is null)
        {
            throw new InvalidOperationException("Order not found.");
        }

        if (!string.Equals(order.Status, "Pending", StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException("Only pending orders can be rejected.");
        }

        order.Status = "Rejected";

        if (!string.IsNullOrWhiteSpace(rejectionReason))
        {
            order.Status = $"Rejected: {rejectionReason}";
        }

        await _dbContext.SaveChangesAsync(cancellationToken);
    }
}
