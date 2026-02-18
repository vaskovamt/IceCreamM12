using IceCreamM12.Application.Interfaces;
using IceCreamM12.Domain.Entities;
using IceCreamM12.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace IceCreamM12.Application.Services;

public class InventoryService : IInventoryService
{
    private readonly ApplicationDbContext _dbContext;
    private readonly IAuditService _auditService;

    public InventoryService(ApplicationDbContext dbContext, IAuditService auditService)
    {
        _dbContext = dbContext;
        _auditService = auditService;
    }

    public async Task<InventoryItem?> LoadInventoryAsync(
        int? productId,
        int? ingredientId,
        decimal quantity,
        string reason,
        string? performedByUserId,
        CancellationToken cancellationToken)
    {
        EnsurePositiveQuantity(quantity);
        var auditReason = NormalizeReason(reason);

        if (productId.HasValue)
        {
            var productQuantity = ConvertToIntQuantity(quantity);
            var inventoryItem = await _dbContext.InventoryItems
                .Include(item => item.Product)
                .FirstOrDefaultAsync(item => item.ProductId == productId.Value, cancellationToken);

            if (inventoryItem is null)
            {
                var productExists = await _dbContext.Products.AnyAsync(product => product.Id == productId.Value, cancellationToken);
                if (!productExists)
                {
                    throw new InvalidOperationException("Product not found.");
                }

                inventoryItem = new InventoryItem
                {
                    ProductId = productId.Value,
                    QuantityOnHand = productQuantity,
                    LastUpdatedAt = DateTime.UtcNow
                };

                _dbContext.InventoryItems.Add(inventoryItem);
            }
            else
            {
                inventoryItem.QuantityOnHand += productQuantity;
                inventoryItem.LastUpdatedAt = DateTime.UtcNow;
            }

            await _auditService.RecordInventoryChangeAsync(inventoryItem, productQuantity, auditReason, performedByUserId, cancellationToken);
            await _dbContext.SaveChangesAsync(cancellationToken);
            return inventoryItem;
        }

        if (ingredientId.HasValue)
        {
            var ingredient = await _dbContext.Ingredients.FirstOrDefaultAsync(i => i.Id == ingredientId.Value, cancellationToken)
                ?? throw new InvalidOperationException("Ingredient not found.");

            ingredient.QuantityOnHand += quantity;
            ingredient.LastUpdatedAt = DateTime.UtcNow;
            await _dbContext.SaveChangesAsync(cancellationToken);
            return null;
        }

        throw new InvalidOperationException("Изберете продукт или суровина.");
    }

    public async Task ScrapProductAsync(
        int? productId,
        int? ingredientId,
        decimal quantity,
        string reason,
        string? performedByUserId,
        CancellationToken cancellationToken)
    {
        EnsurePositiveQuantity(quantity);
        var auditReason = NormalizeReason(reason);

        if (productId.HasValue)
        {
            var productQuantity = ConvertToIntQuantity(quantity);
            var inventoryItem = await _dbContext.InventoryItems
                .FirstOrDefaultAsync(item => item.ProductId == productId.Value, cancellationToken)
                ?? throw new InvalidOperationException("Inventory item not found.");

            if (inventoryItem.QuantityOnHand < productQuantity)
            {
                throw new InvalidOperationException("Insufficient inventory to scrap.");
            }

            inventoryItem.QuantityOnHand -= productQuantity;
            inventoryItem.LastUpdatedAt = DateTime.UtcNow;

            await _auditService.RecordInventoryChangeAsync(inventoryItem, -productQuantity, auditReason, performedByUserId, cancellationToken);
            await _dbContext.SaveChangesAsync(cancellationToken);
            return;
        }

        if (ingredientId.HasValue)
        {
            var ingredient = await _dbContext.Ingredients.FirstOrDefaultAsync(i => i.Id == ingredientId.Value, cancellationToken)
                ?? throw new InvalidOperationException("Ingredient not found.");

            if (ingredient.QuantityOnHand < quantity)
            {
                throw new InvalidOperationException("Insufficient ingredient inventory to scrap.");
            }

            ingredient.QuantityOnHand -= quantity;
            ingredient.LastUpdatedAt = DateTime.UtcNow;
            await _dbContext.SaveChangesAsync(cancellationToken);
            return;
        }

        throw new InvalidOperationException("Изберете продукт или суровина.");
    }

    public async Task SwapProductAsync(
        int? fromProductId,
        int? fromIngredientId,
        int? toProductId,
        int? toIngredientId,
        decimal quantity,
        string reason,
        string? performedByUserId,
        CancellationToken cancellationToken)
    {
        EnsurePositiveQuantity(quantity);
        var auditReason = NormalizeReason(reason);

        var isProductSwap = fromProductId.HasValue || toProductId.HasValue;
        var isIngredientSwap = fromIngredientId.HasValue || toIngredientId.HasValue;

        if (isProductSwap && isIngredientSwap)
        {
            throw new InvalidOperationException("Замяната трябва да е между елементи от един и същ тип.");
        }

        if (fromProductId.HasValue && toProductId.HasValue)
        {
            var productQuantity = ConvertToIntQuantity(quantity);
            var fromItem = await _dbContext.InventoryItems.FirstOrDefaultAsync(item => item.ProductId == fromProductId.Value, cancellationToken)
                ?? throw new InvalidOperationException("Source inventory item not found.");

            if (fromItem.QuantityOnHand < productQuantity)
            {
                throw new InvalidOperationException("Insufficient inventory to replace.");
            }

            var toItem = await _dbContext.InventoryItems.FirstOrDefaultAsync(item => item.ProductId == toProductId.Value, cancellationToken);
            if (toItem is null)
            {
                var productExists = await _dbContext.Products.AnyAsync(product => product.Id == toProductId.Value, cancellationToken);
                if (!productExists)
                {
                    throw new InvalidOperationException("Replacement product not found.");
                }

                toItem = new InventoryItem { ProductId = toProductId.Value, QuantityOnHand = 0, LastUpdatedAt = DateTime.UtcNow };
                _dbContext.InventoryItems.Add(toItem);
            }

            fromItem.QuantityOnHand -= productQuantity;
            fromItem.LastUpdatedAt = DateTime.UtcNow;
            toItem.QuantityOnHand += productQuantity;
            toItem.LastUpdatedAt = DateTime.UtcNow;

            await _auditService.RecordInventoryChangeAsync(fromItem, -productQuantity, auditReason, performedByUserId, cancellationToken);
            await _auditService.RecordInventoryChangeAsync(toItem, productQuantity, auditReason, performedByUserId, cancellationToken);
            await _dbContext.SaveChangesAsync(cancellationToken);
            return;
        }

        if (fromIngredientId.HasValue && toIngredientId.HasValue)
        {
            var fromIngredient = await _dbContext.Ingredients.FirstOrDefaultAsync(i => i.Id == fromIngredientId.Value, cancellationToken)
                ?? throw new InvalidOperationException("Source ingredient not found.");
            var toIngredient = await _dbContext.Ingredients.FirstOrDefaultAsync(i => i.Id == toIngredientId.Value, cancellationToken)
                ?? throw new InvalidOperationException("Target ingredient not found.");

            if (fromIngredient.QuantityOnHand < quantity)
            {
                throw new InvalidOperationException("Insufficient ingredient inventory to replace.");
            }

            fromIngredient.QuantityOnHand -= quantity;
            fromIngredient.LastUpdatedAt = DateTime.UtcNow;
            toIngredient.QuantityOnHand += quantity;
            toIngredient.LastUpdatedAt = DateTime.UtcNow;

            await _dbContext.SaveChangesAsync(cancellationToken);
            return;
        }

        throw new InvalidOperationException("Изберете валидни елементи за замяна.");
    }

    private static int ConvertToIntQuantity(decimal quantity)
    {
        if (quantity != decimal.Truncate(quantity))
        {
            throw new InvalidOperationException("Количеството за продукт трябва да е цяло число.");
        }

        if (quantity > int.MaxValue)
        {
            throw new InvalidOperationException("Количеството е прекалено голямо.");
        }

        return decimal.ToInt32(quantity);
    }

    private static void EnsurePositiveQuantity(decimal quantity)
    {
        if (quantity <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(quantity), "Quantity must be positive.");
        }
    }

    private static string NormalizeReason(string? reason)
    {
        if (string.IsNullOrWhiteSpace(reason))
        {
            throw new InvalidOperationException("Причината е задължителна.");
        }

        return reason.Trim();
    }
}
