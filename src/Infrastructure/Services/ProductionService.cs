using IceCreamM12.Application.Interfaces;
using IceCreamM12.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace IceCreamM12.Infrastructure.Services;

public class ProductionService : IProductionService
{
    private readonly ApplicationDbContext _dbContext;
    private readonly IInventoryService _inventoryService;

    public ProductionService(ApplicationDbContext dbContext, IInventoryService inventoryService)
    {
        _dbContext = dbContext;
        _inventoryService = inventoryService;
    }

    public async Task ProduceAsync(
        int productId,
        int quantity,
        string? performedByUserId,
        CancellationToken cancellationToken)
    {
        if (quantity <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(quantity), "Quantity must be positive.");
        }

        var product = await _dbContext.Products
            .Include(productEntity => productEntity.RecipeItems)
            .ThenInclude(recipeItem => recipeItem.Ingredient)
            .FirstOrDefaultAsync(productEntity => productEntity.Id == productId, cancellationToken);

        if (product is null)
        {
            throw new InvalidOperationException("Product not found.");
        }

        foreach (var recipeItem in product.RecipeItems)
        {
            if (recipeItem.Ingredient is null)
            {
                throw new InvalidOperationException("Ingredient not found for recipe.");
            }

            var requiredQuantity = recipeItem.Quantity * quantity;

            if (recipeItem.Ingredient.QuantityOnHand < requiredQuantity)
            {
                throw new InvalidOperationException(
                    $"Insufficient ingredient inventory for {recipeItem.Ingredient.Name}.");
            }
        }

        foreach (var recipeItem in product.RecipeItems)
        {
            var ingredient = recipeItem.Ingredient!;
            var requiredQuantity = recipeItem.Quantity * quantity;
            ingredient.QuantityOnHand -= requiredQuantity;
            ingredient.LastUpdatedAt = DateTime.UtcNow;
        }

        await _inventoryService.LoadInventoryAsync(
            productId,
            null,
            quantity,
            $"Production batch for {product.Name}",
            performedByUserId,
            cancellationToken);
    }
}
