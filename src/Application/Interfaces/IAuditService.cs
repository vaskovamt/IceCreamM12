using IceCreamM12.Domain.Entities;

namespace IceCreamM12.Application.Interfaces;

public interface IAuditService
{
    Task RecordInventoryChangeAsync(
        InventoryItem inventoryItem,
        int quantityChange,
        string reason,
        string? performedByUserId,
        CancellationToken cancellationToken);
}
