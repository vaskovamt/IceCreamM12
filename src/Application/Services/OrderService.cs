using IceCreamM12.Application.Interfaces;
using IceCreamM12.Domain.Entities;
using IceCreamM12.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace IceCreamM12.Application.Services;

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
        IReadOnlyCollection<OrderProductRequest> products,
        string customerName,
        string customerEmail,
        string companyEik,
        string invoiceAddress,
        string paymentMethod,
        string? vatNumber,
        string? contactPhone,
        CancellationToken cancellationToken)
    {
        if (products.Count == 0)
        {
            throw new InvalidOperationException("Order must include at least one product.");
        }

        var normalizedItems = products
            .GroupBy(item => item.ProductId)
            .Select(group => new OrderProductRequest(group.Key, group.Sum(item => item.Quantity)))
            .ToList();

        if (normalizedItems.Any(item => item.Quantity <= 0))
        {
            throw new InvalidOperationException("Quantity must be greater than zero.");
        }

        var productIds = normalizedItems.Select(item => item.ProductId).ToList();
        var dbProducts = await _dbContext.Products
            .Include(p => p.InventoryItem)
            .Where(p => productIds.Contains(p.Id))
            .ToDictionaryAsync(p => p.Id, cancellationToken);

        foreach (var item in normalizedItems)
        {
            if (!dbProducts.TryGetValue(item.ProductId, out var product) || product.InventoryItem is null || product.InventoryItem.QuantityOnHand <= 0)
            {
                throw new InvalidOperationException("Продуктът не е наличен.");
            }

            if (item.Quantity > product.InventoryItem.QuantityOnHand)
            {
                throw new InvalidOperationException($"Налични са само {product.InventoryItem.QuantityOnHand} бр. за {product.Name}.");
            }
        }

        var orderItems = normalizedItems
            .Select(item =>
            {
                var product = dbProducts[item.ProductId];
                return new OrderItem
                {
                    ProductId = product.Id,
                    Quantity = item.Quantity,
                    UnitPrice = product.Price
                };
            })
            .ToList();

        var order = new Order
        {
            OrderNumber = $"ORD-{DateTime.UtcNow:yyyyMMddHHmmss}-{Random.Shared.Next(100, 1000)}",
            OrderedAt = DateTime.UtcNow,
            Status = "Pending",
            CustomerName = customerName,
            CustomerEmail = customerEmail,
            CompanyEik = companyEik,
            InvoiceAddress = invoiceAddress,
            PaymentMethod = paymentMethod,
            VatNumber = string.IsNullOrWhiteSpace(vatNumber) ? null : vatNumber.Trim(),
            ContactPhone = string.IsNullOrWhiteSpace(contactPhone) ? null : contactPhone.Trim(),
            TotalAmount = orderItems.Sum(item => item.UnitPrice * item.Quantity),
            Items = orderItems
        };

        _dbContext.Orders.Add(order);
        await _dbContext.SaveChangesAsync(cancellationToken);
        return order;
    }


    public Task<Order?> GetOrderByIdAsync(int orderId, CancellationToken cancellationToken)
        => _dbContext.Orders
            .Include(order => order.Items)
            .ThenInclude(item => item.Product)
            .FirstOrDefaultAsync(order => order.Id == orderId, cancellationToken);

    public async Task UpdateOrderAsync(
        int orderId,
        IReadOnlyCollection<OrderProductRequest> products,
        string customerName,
        string customerEmail,
        string companyEik,
        string invoiceAddress,
        string paymentMethod,
        string? vatNumber,
        string? contactPhone,
        string? performedByUserId,
        CancellationToken cancellationToken)
    {
        var order = await _dbContext.Orders
            .Include(orderEntity => orderEntity.Items)
            .FirstOrDefaultAsync(orderEntity => orderEntity.Id == orderId, cancellationToken);

        if (order is null)
        {
            throw new InvalidOperationException("Order not found.");
        }

        if (!string.Equals(order.Status, "Approved", StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException("Only approved orders can be updated.");
        }

        var normalizedItems = products
            .GroupBy(item => item.ProductId)
            .Select(group => new OrderProductRequest(group.Key, group.Sum(item => item.Quantity)))
            .ToList();

        if (normalizedItems.Count == 0 || normalizedItems.Any(item => item.Quantity <= 0))
        {
            throw new InvalidOperationException("Order must include at least one product with quantity greater than zero.");
        }

        var productIds = normalizedItems.Select(item => item.ProductId).ToList();
        var dbProducts = await _dbContext.Products
            .Include(p => p.InventoryItem)
            .Where(p => productIds.Contains(p.Id))
            .ToDictionaryAsync(p => p.Id, cancellationToken);

        var currentItemsByProduct = order.Items
            .GroupBy(item => item.ProductId)
            .ToDictionary(group => group.Key, group => group.Sum(item => item.Quantity));

        var allProductIds = currentItemsByProduct.Keys.Union(productIds).Distinct().ToList();
        var impactedProducts = await _dbContext.Products
            .Include(p => p.InventoryItem)
            .Where(p => allProductIds.Contains(p.Id))
            .ToDictionaryAsync(p => p.Id, cancellationToken);

        foreach (var productId in allProductIds)
        {
            if (!impactedProducts.TryGetValue(productId, out var product) || product.InventoryItem is null)
            {
                throw new InvalidOperationException($"Inventory not found for product {productId}.");
            }

            var oldQty = currentItemsByProduct.GetValueOrDefault(productId, 0);
            var newQty = normalizedItems.FirstOrDefault(item => item.ProductId == productId)?.Quantity ?? 0;
            var delta = newQty - oldQty;

            if (delta > 0 && product.InventoryItem.QuantityOnHand < delta)
            {
                throw new InvalidOperationException($"Няма достатъчна наличност за {product.Name}. Налични: {product.InventoryItem.QuantityOnHand} бр.");
            }
        }

        foreach (var existingItem in order.Items.ToList())
        {
            _dbContext.OrderItems.Remove(existingItem);
        }

        order.Items = normalizedItems
            .Select(item =>
            {
                if (!dbProducts.TryGetValue(item.ProductId, out var product) || product.InventoryItem is null)
                {
                    throw new InvalidOperationException("Продуктът не е наличен.");
                }

                return new OrderItem
                {
                    ProductId = product.Id,
                    Quantity = item.Quantity,
                    UnitPrice = product.Price
                };
            })
            .ToList();

        foreach (var productId in allProductIds)
        {
            var product = impactedProducts[productId];
            var inventoryItem = product.InventoryItem!;

            var oldQty = currentItemsByProduct.GetValueOrDefault(productId, 0);
            var newQty = normalizedItems.FirstOrDefault(item => item.ProductId == productId)?.Quantity ?? 0;
            var delta = newQty - oldQty;

            if (delta == 0)
            {
                continue;
            }

            inventoryItem.QuantityOnHand -= delta;
            inventoryItem.LastUpdatedAt = DateTime.UtcNow;

            var changeReason = delta > 0
                ? $"Order {order.OrderNumber} updated (quantity increased)"
                : $"Order {order.OrderNumber} updated (quantity decreased)";

            await _auditService.RecordInventoryChangeAsync(
                inventoryItem,
                -delta,
                changeReason,
                performedByUserId,
                cancellationToken);
        }

        order.CustomerName = customerName;
        order.CustomerEmail = customerEmail;
        order.CompanyEik = companyEik;
        order.InvoiceAddress = invoiceAddress;
        order.PaymentMethod = paymentMethod;
        order.VatNumber = string.IsNullOrWhiteSpace(vatNumber) ? null : vatNumber.Trim();
        order.ContactPhone = string.IsNullOrWhiteSpace(contactPhone) ? null : contactPhone.Trim();
        order.TotalAmount = order.Items.Sum(item => item.UnitPrice * item.Quantity);

        await _dbContext.SaveChangesAsync(cancellationToken);
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
