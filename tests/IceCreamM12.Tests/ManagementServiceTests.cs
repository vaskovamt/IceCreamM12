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

        var results = await service.ExecuteDailyCheckAsync(new Dictionary<int, int> { [product.Id] = 7 }, "u1", CancellationToken.None);

        Assert.Single(results);
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
            service.ExecuteDailyCheckAsync(new Dictionary<int, int> { [product.Id] = -1 }, "u1", CancellationToken.None));
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
