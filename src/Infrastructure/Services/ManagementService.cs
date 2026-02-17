using IceCreamM12.Application.Interfaces;
using IceCreamM12.Application.Models;
using IceCreamM12.Domain.Entities;
using IceCreamM12.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace IceCreamM12.Infrastructure.Services;

public class ManagementService : IManagementService
{
    private readonly ApplicationDbContext _dbContext;
    private readonly IAuditService _auditService;

    public ManagementService(ApplicationDbContext dbContext, IAuditService auditService)
    {
        _dbContext = dbContext;
        _auditService = auditService;
    }

    public async Task<OwnerDashboardData> GetOwnerDashboardAsync(CancellationToken cancellationToken)
    {
        var lowStockProducts = await GetLowStockProductsAsync(cancellationToken);
        var allOrders = await _dbContext.Orders
            .Include(o => o.Items)
            .OrderByDescending(o => o.OrderedAt)
            .ToListAsync(cancellationToken);

        return new OwnerDashboardData
        {
            PendingOrdersCount = allOrders.Count(o => o.Status == "Pending"),
            ApprovedOrdersCount = allOrders.Count(o => o.Status == "Approved"),
            RejectedOrdersCount = allOrders.Count(o => o.Status == "Rejected"),
            TotalOrdersCount = allOrders.Count,
            TotalProducts = await _dbContext.Products.CountAsync(cancellationToken),
            TotalInventoryUnits = await _dbContext.InventoryItems.SumAsync(i => i.QuantityOnHand, cancellationToken),
            PendingOrdersAmount = allOrders
                .Where(o => o.Status == "Pending")
                .Sum(o => o.TotalAmount),
            ApprovedOrdersAmount = allOrders
                .Where(o => o.Status == "Approved")
                .Sum(o => o.TotalAmount),
            LowStockProducts = lowStockProducts,
            LatestOrders = allOrders.Take(5).ToList(),
            RecentAudits = await GetRecentAuditsAsync(10, cancellationToken)
        };
    }

    public async Task<WorkerDashboardData> GetWorkerDashboardAsync(CancellationToken cancellationToken)
    {
        var today = DateTime.UtcNow.Date;
        var orders = await GetOrdersAsync(null, cancellationToken);
        var operations = await GetRecentAuditsAsync(25, cancellationToken);

        return new WorkerDashboardData
        {
            PendingOrdersCount = orders.Count(o => o.Status == "Pending"),
            LowStockProducts = await GetLowStockProductsAsync(cancellationToken),
            TodayOperationsCount = await _dbContext.InventoryAudits.CountAsync(a => a.PerformedAt >= today, cancellationToken),
            Orders = orders,
            Operations = operations,
            InventoryItems = await GetInventoryItemsAsync(cancellationToken)
        };
    }

    public async Task<List<Order>> GetOrdersAsync(string? status, CancellationToken cancellationToken)
    {
        var query = _dbContext.Orders
            .Include(o => o.Items)
            .ThenInclude(i => i.Product)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(status))
        {
            query = query.Where(o => o.Status == status);
        }

        return await query.OrderByDescending(o => o.OrderedAt).ToListAsync(cancellationToken);
    }

    public Task<List<Order>> GetPendingOrdersAsync(CancellationToken cancellationToken)
        => GetOrdersAsync("Pending", cancellationToken);

    public Task<List<Product>> GetProductsAsync(CancellationToken cancellationToken)
        => _dbContext.Products
            .Include(p => p.Category)
            .Include(p => p.InventoryItem)
            .OrderBy(p => p.Name)
            .ToListAsync(cancellationToken);

    public Task<Product?> GetProductByIdAsync(int id, CancellationToken cancellationToken)
        => _dbContext.Products
            .Include(p => p.Category)
            .Include(p => p.InventoryItem)
            .FirstOrDefaultAsync(p => p.Id == id, cancellationToken);

    public async Task CreateProductAsync(Product product, CancellationToken cancellationToken)
    {
        _dbContext.Products.Add(product);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task UpdateProductAsync(Product product, CancellationToken cancellationToken)
    {
        _dbContext.Products.Update(product);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task DeleteProductAsync(int id, CancellationToken cancellationToken)
    {
        var product = await _dbContext.Products.FirstOrDefaultAsync(p => p.Id == id, cancellationToken);
        if (product is null) return;
        _dbContext.Products.Remove(product);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public Task<List<Category>> GetCategoriesAsync(CancellationToken cancellationToken)
        => _dbContext.Categories.OrderBy(c => c.Name).ToListAsync(cancellationToken);

    public Task<Category?> GetCategoryByIdAsync(int id, CancellationToken cancellationToken)
        => _dbContext.Categories.FirstOrDefaultAsync(c => c.Id == id, cancellationToken);

    public async Task CreateCategoryAsync(Category category, CancellationToken cancellationToken)
    {
        _dbContext.Categories.Add(category);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task UpdateCategoryAsync(Category category, CancellationToken cancellationToken)
    {
        _dbContext.Categories.Update(category);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task DeleteCategoryAsync(int id, CancellationToken cancellationToken)
    {
        var category = await _dbContext.Categories.FirstOrDefaultAsync(c => c.Id == id, cancellationToken);
        if (category is null) return;
        _dbContext.Categories.Remove(category);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public Task<List<Ingredient>> GetIngredientsAsync(CancellationToken cancellationToken)
        => _dbContext.Ingredients.OrderBy(i => i.Name).ToListAsync(cancellationToken);

    public Task<Ingredient?> GetIngredientByIdAsync(int id, CancellationToken cancellationToken)
        => _dbContext.Ingredients.FirstOrDefaultAsync(i => i.Id == id, cancellationToken);

    public async Task CreateIngredientAsync(Ingredient ingredient, CancellationToken cancellationToken)
    {
        ingredient.LastUpdatedAt = DateTime.UtcNow;
        _dbContext.Ingredients.Add(ingredient);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task UpdateIngredientAsync(Ingredient ingredient, CancellationToken cancellationToken)
    {
        ingredient.LastUpdatedAt = DateTime.UtcNow;
        _dbContext.Ingredients.Update(ingredient);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task DeleteIngredientAsync(int id, CancellationToken cancellationToken)
    {
        var ingredient = await _dbContext.Ingredients.FirstOrDefaultAsync(i => i.Id == id, cancellationToken);
        if (ingredient is null) return;
        _dbContext.Ingredients.Remove(ingredient);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public Task<List<InventoryItem>> GetInventoryItemsAsync(CancellationToken cancellationToken)
        => _dbContext.InventoryItems.Include(i => i.Product).OrderBy(i => i.Product!.Name).ToListAsync(cancellationToken);

    public Task<List<InventoryAudit>> GetRecentAuditsAsync(int take, CancellationToken cancellationToken)
        => _dbContext.InventoryAudits
            .Include(a => a.InventoryItem)
            .ThenInclude(i => i!.Product)
            .OrderByDescending(a => a.PerformedAt)
            .Take(take)
            .ToListAsync(cancellationToken);

    public async Task<List<DailyCheckResult>> ExecuteDailyCheckAsync(
        Dictionary<int, int> countedQuantities,
        string? performedByUserId,
        CancellationToken cancellationToken)
    {
        var systemItems = await _dbContext.InventoryItems.Include(i => i.Product).ToListAsync(cancellationToken);
        var results = new List<DailyCheckResult>();

        foreach (var item in systemItems)
        {
            var counted = countedQuantities.GetValueOrDefault(item.ProductId, item.QuantityOnHand);
            if (counted < 0)
            {
                throw new InvalidOperationException($"Counted quantity cannot be negative for product {item.ProductId}.");
            }

            var result = new DailyCheckResult
            {
                ProductId = item.ProductId,
                ProductName = item.Product?.Name ?? $"Product #{item.ProductId}",
                SystemQuantity = item.QuantityOnHand,
                CountedQuantity = counted
            };
            results.Add(result);

            if (result.HasMismatch)
            {
                var delta = result.CountedQuantity - result.SystemQuantity;
                item.QuantityOnHand = result.CountedQuantity;
                item.LastUpdatedAt = DateTime.UtcNow;

                await _auditService.RecordInventoryChangeAsync(
                    item,
                    delta,
                    $"Daily check reconciliation: system={result.SystemQuantity}, counted={result.CountedQuantity}",
                    performedByUserId,
                    cancellationToken);
            }
        }

        await _dbContext.SaveChangesAsync(cancellationToken);
        return results;
    }

    private Task<List<Product>> GetLowStockProductsAsync(CancellationToken cancellationToken)
        => _dbContext.Products
            .Include(p => p.InventoryItem)
            .Where(p => p.InventoryItem != null && p.InventoryItem.QuantityOnHand <= 5)
            .OrderBy(p => p.InventoryItem!.QuantityOnHand)
            .ToListAsync(cancellationToken);
}
