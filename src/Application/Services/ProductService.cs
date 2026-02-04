using IceCreamM12.Application.Interfaces;

namespace IceCreamM12.Application.Services;

public class ProductService : IProductService
{
    private readonly IInventoryService _inventoryService;

    public ProductService(IInventoryService inventoryService)
    {
        _inventoryService = inventoryService;
    }

    public Task ScrapProductAsync(
        int productId,
        int quantity,
        string reason,
        string? performedByUserId,
        CancellationToken cancellationToken)
    {
        return _inventoryService.ScrapProductAsync(
            productId,
            quantity,
            reason,
            performedByUserId,
            cancellationToken);
    }

    public Task ReplaceProductAsync(
        int originalProductId,
        int replacementProductId,
        int quantity,
        string reason,
        string? performedByUserId,
        CancellationToken cancellationToken)
    {
        return _inventoryService.SwapProductAsync(
            originalProductId,
            replacementProductId,
            quantity,
            reason,
            performedByUserId,
            cancellationToken);
    }
}
