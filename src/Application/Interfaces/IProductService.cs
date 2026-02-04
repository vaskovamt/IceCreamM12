namespace IceCreamM12.Application.Interfaces;

public interface IProductService
{
    Task ScrapProductAsync(
        int productId,
        int quantity,
        string reason,
        string? performedByUserId,
        CancellationToken cancellationToken);

    Task ReplaceProductAsync(
        int originalProductId,
        int replacementProductId,
        int quantity,
        string reason,
        string? performedByUserId,
        CancellationToken cancellationToken);
}
