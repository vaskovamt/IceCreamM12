using IceCreamM12.Application.Interfaces;
using IceCreamM12.Domain.Entities;
using IceCreamM12.Infrastructure.Data;

namespace IceCreamM12.Application.Services;

public class AuditService : IAuditService
{
    private readonly ApplicationDbContext _dbContext;

    public AuditService(ApplicationDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public Task RecordInventoryChangeAsync(
        InventoryItem inventoryItem,
        int quantityChange,
        string reason,
        string? performedByUserId,
        CancellationToken cancellationToken)
    {
        var audit = new InventoryAudit
        {
            InventoryItemId = inventoryItem.Id,
            QuantityChange = quantityChange,
            Reason = reason,
            PerformedAt = DateTime.UtcNow,
            PerformedByUserId = performedByUserId
        };

        _dbContext.InventoryAudits.Add(audit);
        return Task.CompletedTask;
    }
}
