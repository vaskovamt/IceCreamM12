using IceCreamM12.Application.Models;
using IceCreamM12.Domain.Entities;

namespace IceCreamM12.Application.Interfaces;

public interface IManagementService
{
    Task<OwnerDashboardData> GetOwnerDashboardAsync(CancellationToken cancellationToken);
    Task<WorkerDashboardData> GetWorkerDashboardAsync(CancellationToken cancellationToken);
    Task<List<Order>> GetOrdersAsync(string? status, CancellationToken cancellationToken);
    Task<List<Order>> GetPendingOrdersAsync(CancellationToken cancellationToken);

    Task<List<Product>> GetProductsAsync(CancellationToken cancellationToken);
    Task<Product?> GetProductByIdAsync(int id, CancellationToken cancellationToken);
    Task CreateProductAsync(Product product, CancellationToken cancellationToken);
    Task UpdateProductAsync(Product product, CancellationToken cancellationToken);
    Task DeleteProductAsync(int id, CancellationToken cancellationToken);

    Task<List<Category>> GetCategoriesAsync(CancellationToken cancellationToken);
    Task<Category?> GetCategoryByIdAsync(int id, CancellationToken cancellationToken);
    Task CreateCategoryAsync(Category category, CancellationToken cancellationToken);
    Task UpdateCategoryAsync(Category category, CancellationToken cancellationToken);
    Task DeleteCategoryAsync(int id, CancellationToken cancellationToken);

    Task<List<Ingredient>> GetIngredientsAsync(CancellationToken cancellationToken);
    Task<Ingredient?> GetIngredientByIdAsync(int id, CancellationToken cancellationToken);
    Task CreateIngredientAsync(Ingredient ingredient, CancellationToken cancellationToken);
    Task UpdateIngredientAsync(Ingredient ingredient, CancellationToken cancellationToken);
    Task DeleteIngredientAsync(int id, CancellationToken cancellationToken);

    Task<List<InventoryItem>> GetInventoryItemsAsync(CancellationToken cancellationToken);
    Task<List<InventoryAudit>> GetRecentAuditsAsync(int take, CancellationToken cancellationToken);
    Task<List<DailyCheckResult>> ExecuteDailyCheckAsync(
        Dictionary<int, int> countedQuantities,
        string? performedByUserId,
        CancellationToken cancellationToken);
}
