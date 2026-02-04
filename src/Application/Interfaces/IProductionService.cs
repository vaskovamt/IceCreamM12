namespace IceCreamM12.Application.Interfaces;

public interface IProductionService
{
    Task ProduceAsync(
        int productId,
        int quantity,
        string? performedByUserId,
        CancellationToken cancellationToken);
}
