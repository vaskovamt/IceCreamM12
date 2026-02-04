using IceCreamM12.Domain.Entities;

namespace IceCreamM12.Application.Interfaces;

public interface IInventoryService
{
    Task<InventoryItem> LoadInventoryAsync(
        int productId,
        int quantity,
        string reason,
        string? performedByUserId,
        CancellationToken cancellationToken);

    Task ScrapProductAsync(
        int productId,
        int quantity,
        string reason,
        string? performedByUserId,
        CancellationToken cancellationToken);

    Task SwapProductAsync(
        int fromProductId,
        int toProductId,
        int quantity,
        string reason,
        string? performedByUserId,
        CancellationToken cancellationToken);
}
