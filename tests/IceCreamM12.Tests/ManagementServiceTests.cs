using IceCreamM12.Application.Interfaces;
using IceCreamM12.Domain.Entities;
using IceCreamM12.Infrastructure.Data;
using IceCreamM12.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace IceCreamM12.Tests;

public class ManagementServiceTests
{
    [Fact]
    public async Task ExecuteDailyCheckAsync_ReconcilesInventoryAndRecordsDelta()
    {
        var dbName = $"daily-check-{Guid.NewGuid()}";
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(dbName)
            .Options;

        await using var context = new ApplicationDbContext(options);

        var product = new Product { Name = "Test Product", Price = 1.20m, CategoryId = 1 };
        context.Products.Add(product);
        await context.SaveChangesAsync();

        var inventory = new InventoryItem
        {
            ProductId = product.Id,
            QuantityOnHand = 10,
            LastUpdatedAt = DateTime.UtcNow
        };
        context.InventoryItems.Add(inventory);
        await context.SaveChangesAsync();

        var audit = new CapturingAuditService();
        var service = new ManagementService(context, audit);

        var (results, ingredientResults) = await service.ExecuteDailyCheckAsync(
            new Dictionary<int, int> { [product.Id] = 7 },
            new Dictionary<int, decimal>(),
            "u1",
            CancellationToken.None);

        Assert.Single(results);
        Assert.Empty(ingredientResults);
        Assert.True(results[0].HasMismatch);
        Assert.Equal(-3, results[0].Difference);

        var savedInventory = await context.InventoryItems.SingleAsync();
        Assert.Equal(7, savedInventory.QuantityOnHand);

        Assert.Single(audit.Calls);
        Assert.Equal(-3, audit.Calls[0].QuantityChange);
    }

    [Fact]
    public async Task ExecuteDailyCheckAsync_ThrowsForNegativeCountedQuantity()
    {
        var dbName = $"daily-check-negative-{Guid.NewGuid()}";
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(dbName)
            .Options;

        await using var context = new ApplicationDbContext(options);

        var product = new Product { Name = "Test Product", Price = 1.20m, CategoryId = 1 };
        context.Products.Add(product);
        await context.SaveChangesAsync();

        context.InventoryItems.Add(new InventoryItem { ProductId = product.Id, QuantityOnHand = 2, LastUpdatedAt = DateTime.UtcNow });
        await context.SaveChangesAsync();

        var service = new ManagementService(context, new CapturingAuditService());

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.ExecuteDailyCheckAsync(new Dictionary<int, int> { [product.Id] = -1 }, new Dictionary<int, decimal>(), "u1", CancellationToken.None));
    }


    [Fact]
    public async Task ExecuteDailyCheckAsync_ReconcilesIngredients()
    {
        var dbName = $"daily-check-ingredient-{Guid.NewGuid()}";
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(dbName)
            .Options;

        await using var context = new ApplicationDbContext(options);

        var ingredient = new Ingredient
        {
            Name = "Milk",
            Unit = "l",
            QuantityOnHand = 15m,
            CostPerUnit = 1m,
            ReorderLevel = 2m,
            LastUpdatedAt = DateTime.UtcNow
        };
        context.Ingredients.Add(ingredient);
        await context.SaveChangesAsync();

        var service = new ManagementService(context, new CapturingAuditService());

        var (productResults, ingredientResults) = await service.ExecuteDailyCheckAsync(
            new Dictionary<int, int>(),
            new Dictionary<int, decimal> { [ingredient.Id] = 13.5m },
            "u1",
            CancellationToken.None);

        Assert.Empty(productResults);
        Assert.Single(ingredientResults);
        Assert.True(ingredientResults[0].HasMismatch);
        Assert.Equal(-1.5m, ingredientResults[0].Difference);

        var savedIngredient = await context.Ingredients.SingleAsync();
        Assert.Equal(13.5m, savedIngredient.QuantityOnHand);
    }

    [Fact]
    public async Task ExecuteDailyCheckAsync_ThrowsForNegativeIngredientCountedQuantity()
    {
        var dbName = $"daily-check-negative-ingredient-{Guid.NewGuid()}";
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(dbName)
            .Options;

        await using var context = new ApplicationDbContext(options);

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

        var service = new ManagementService(context, new CapturingAuditService());

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.ExecuteDailyCheckAsync(
                new Dictionary<int, int>(),
                new Dictionary<int, decimal> { [ingredient.Id] = -0.1m },
                "u1",
                CancellationToken.None));
    }

    private sealed class CapturingAuditService : IAuditService
    {
        public List<(int QuantityChange, string Reason)> Calls { get; } = [];

        public Task RecordInventoryChangeAsync(InventoryItem inventoryItem, int quantityChange, string reason, string? performedByUserId, CancellationToken cancellationToken)
        {
            Calls.Add((quantityChange, reason));
            return Task.CompletedTask;
        }
    }
}
