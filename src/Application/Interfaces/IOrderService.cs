using IceCreamM12.Domain.Entities;

namespace IceCreamM12.Application.Interfaces;

public interface IOrderService
{
    Task<List<Product>> GetAvailableProductsAsync(CancellationToken cancellationToken);

    Task<List<Order>> GetOrdersByCustomerEmailAsync(
        string customerEmail,
        CancellationToken cancellationToken);

    Task<Order> CreatePendingOrderAsync(
        IReadOnlyCollection<OrderProductRequest> products,
        string customerName,
        string customerEmail,
        string companyEik,
        string invoiceAddress,
        string paymentMethod,
        string? vatNumber,
        string? contactPhone,
        CancellationToken cancellationToken);

    Task ApproveOrderAsync(
        int orderId,
        string? performedByUserId,
        CancellationToken cancellationToken);

    Task RejectOrderAsync(
        int orderId,
        string rejectionReason,
        string? performedByUserId,
        CancellationToken cancellationToken);
}

public sealed record OrderProductRequest(int ProductId, int Quantity);
