using IceCreamM12.Application.Interfaces;
using IceCreamM12.Domain.Entities;
using IceCreamM12.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace IceCreamM12.Infrastructure.Services;

public class InventoryService : IInventoryService
{
    private const string DefaultAuditReason = "Няма причина";
    private readonly ApplicationDbContext _dbContext;
    private readonly IAuditService _auditService;

    public InventoryService(ApplicationDbContext dbContext, IAuditService auditService)
    {
        _dbContext = dbContext;
        _auditService = auditService;
    }

    public async Task<InventoryItem> LoadInventoryAsync(
        int productId,
        int quantity,
        string reason,
        string? performedByUserId,
        CancellationToken cancellationToken)
    {
        if (quantity <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(quantity), "Quantity must be positive.");
        }

        var inventoryItem = await _dbContext.InventoryItems
            .Include(item => item.Product)
            .FirstOrDefaultAsync(item => item.ProductId == productId, cancellationToken);

        if (inventoryItem is null)
        {
            var productExists = await _dbContext.Products.AnyAsync(
                product => product.Id == productId,
                cancellationToken);

            if (!productExists)
            {
                throw new InvalidOperationException("Product not found.");
            }

            inventoryItem = new InventoryItem
            {
                ProductId = productId,
                QuantityOnHand = quantity,
                LastUpdatedAt = DateTime.UtcNow
            };

            _dbContext.InventoryItems.Add(inventoryItem);
        }
        else
        {
            inventoryItem.QuantityOnHand += quantity;
            inventoryItem.LastUpdatedAt = DateTime.UtcNow;
        }

        var auditReason = NormalizeReason(reason);

        await _auditService.RecordInventoryChangeAsync(
            inventoryItem,
            quantity,
            auditReason,
            performedByUserId,
            cancellationToken);

        await _dbContext.SaveChangesAsync(cancellationToken);
        return inventoryItem;
    }

    public async Task ScrapProductAsync(
        int productId,
        int quantity,
        string reason,
        string? performedByUserId,
        CancellationToken cancellationToken)
    {
        if (quantity <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(quantity), "Quantity must be positive.");
        }

        var inventoryItem = await _dbContext.InventoryItems
            .FirstOrDefaultAsync(item => item.ProductId == productId, cancellationToken);

        if (inventoryItem is null)
        {
            throw new InvalidOperationException("Inventory item not found.");
        }

        if (inventoryItem.QuantityOnHand < quantity)
        {
            throw new InvalidOperationException("Insufficient inventory to scrap.");
        }

        inventoryItem.QuantityOnHand -= quantity;
        inventoryItem.LastUpdatedAt = DateTime.UtcNow;

        var auditReason = NormalizeReason(reason);

        await _auditService.RecordInventoryChangeAsync(
            inventoryItem,
            -quantity,
            auditReason,
            performedByUserId,
            cancellationToken);

        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task SwapProductAsync(
        int fromProductId,
        int toProductId,
        int quantity,
        string reason,
        string? performedByUserId,
        CancellationToken cancellationToken)
    {
        if (quantity <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(quantity), "Quantity must be positive.");
        }

        var fromItem = await _dbContext.InventoryItems
            .FirstOrDefaultAsync(item => item.ProductId == fromProductId, cancellationToken);

        if (fromItem is null)
        {
            throw new InvalidOperationException("Source inventory item not found.");
        }

        if (fromItem.QuantityOnHand < quantity)
        {
            throw new InvalidOperationException("Insufficient inventory to replace.");
        }

        var toItem = await _dbContext.InventoryItems
            .FirstOrDefaultAsync(item => item.ProductId == toProductId, cancellationToken);

        if (toItem is null)
        {
            var productExists = await _dbContext.Products.AnyAsync(
                product => product.Id == toProductId,
                cancellationToken);

            if (!productExists)
            {
                throw new InvalidOperationException("Replacement product not found.");
            }

            toItem = new InventoryItem
            {
                ProductId = toProductId,
                QuantityOnHand = 0,
                LastUpdatedAt = DateTime.UtcNow
            };

            _dbContext.InventoryItems.Add(toItem);
        }

        fromItem.QuantityOnHand -= quantity;
        fromItem.LastUpdatedAt = DateTime.UtcNow;
        toItem.QuantityOnHand += quantity;
        toItem.LastUpdatedAt = DateTime.UtcNow;

        var auditReason = NormalizeReason(reason);

        await _auditService.RecordInventoryChangeAsync(
            fromItem,
            -quantity,
            auditReason,
            performedByUserId,
            cancellationToken);

        await _auditService.RecordInventoryChangeAsync(
            toItem,
            quantity,
            auditReason,
            performedByUserId,
            cancellationToken);

        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    private static string NormalizeReason(string? reason)
    {
        return string.IsNullOrWhiteSpace(reason)
            ? DefaultAuditReason
            : reason.Trim();
    }
}
