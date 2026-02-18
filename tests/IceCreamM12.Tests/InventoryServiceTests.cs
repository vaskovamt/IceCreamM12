using IceCreamM12.Application.Interfaces;
using IceCreamM12.Domain.Entities;
using IceCreamM12.Infrastructure.Data;
using IceCreamM12.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace IceCreamM12.Tests;

public class InventoryServiceTests
{
    [Fact]
    public async Task LoadInventoryAsync_LoadsIngredientQuantity()
    {
        await using var context = CreateContext();
        var ingredient = new Ingredient
        {
            Name = "Milk",
            Unit = "l",
            QuantityOnHand = 10m,
            CostPerUnit = 1m,
            ReorderLevel = 2m,
            LastUpdatedAt = DateTime.UtcNow
        };
        context.Ingredients.Add(ingredient);
        await context.SaveChangesAsync();

        var service = new InventoryService(context, new CapturingAuditService());

        await service.LoadInventoryAsync(null, ingredient.Id, 2.5m, "Зареждане на суровина", "u1", CancellationToken.None);

        var saved = await context.Ingredients.SingleAsync();
        Assert.Equal(12.5m, saved.QuantityOnHand);
    }

    [Fact]
    public async Task ScrapProductAsync_ScrapsIngredientQuantity()
    {
        await using var context = CreateContext();
        var ingredient = new Ingredient
        {
            Name = "Sugar",
            Unit = "kg",
            QuantityOnHand = 5m,
            CostPerUnit = 1m,
            ReorderLevel = 1m,
            LastUpdatedAt = DateTime.UtcNow
        };
        context.Ingredients.Add(ingredient);
        await context.SaveChangesAsync();

        var service = new InventoryService(context, new CapturingAuditService());

        await service.ScrapProductAsync(null, ingredient.Id, 1.25m, "Брак", "u1", CancellationToken.None);

        var saved = await context.Ingredients.SingleAsync();
        Assert.Equal(3.75m, saved.QuantityOnHand);
    }

    [Fact]
    public async Task SwapProductAsync_SwapsIngredientQuantities()
    {
        await using var context = CreateContext();
        var from = new Ingredient
        {
            Name = "Vanilla",
            Unit = "ml",
            QuantityOnHand = 100m,
            CostPerUnit = 1m,
            ReorderLevel = 10m,
            LastUpdatedAt = DateTime.UtcNow
        };
        var to = new Ingredient
        {
            Name = "Cocoa",
            Unit = "g",
            QuantityOnHand = 80m,
            CostPerUnit = 1m,
            ReorderLevel = 10m,
            LastUpdatedAt = DateTime.UtcNow
        };

        context.Ingredients.AddRange(from, to);
        await context.SaveChangesAsync();

        var service = new InventoryService(context, new CapturingAuditService());

        await service.SwapProductAsync(null, from.Id, null, to.Id, 15m, "Замяна", "u1", CancellationToken.None);

        var savedFrom = await context.Ingredients.SingleAsync(i => i.Id == from.Id);
        var savedTo = await context.Ingredients.SingleAsync(i => i.Id == to.Id);
        Assert.Equal(85m, savedFrom.QuantityOnHand);
        Assert.Equal(95m, savedTo.QuantityOnHand);
    }

    private static ApplicationDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase($"inventory-{Guid.NewGuid()}")
            .Options;

        return new ApplicationDbContext(options);
    }

    private sealed class CapturingAuditService : IAuditService
    {
        public Task RecordInventoryChangeAsync(InventoryItem inventoryItem, int quantityChange, string reason, string? performedByUserId, CancellationToken cancellationToken)
            => Task.CompletedTask;
    }
}
