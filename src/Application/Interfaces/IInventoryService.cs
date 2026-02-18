using IceCreamM12.Domain.Entities;

namespace IceCreamM12.Application.Interfaces;

public interface IInventoryService
{
    Task<InventoryItem?> LoadInventoryAsync(
        int? productId,
        int? ingredientId,
        decimal quantity,
        string reason,
        string? performedByUserId,
        CancellationToken cancellationToken);

    Task ScrapProductAsync(
        int? productId,
        int? ingredientId,
        decimal quantity,
        string reason,
        string? performedByUserId,
        CancellationToken cancellationToken);

    Task SwapProductAsync(
        int? fromProductId,
        int? fromIngredientId,
        int? toProductId,
        int? toIngredientId,
        decimal quantity,
        string reason,
        string? performedByUserId,
        CancellationToken cancellationToken);
}
