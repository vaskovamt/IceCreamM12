using IceCreamM12.Domain.Entities;

namespace IceCreamM12.Application.Interfaces;

public interface IFlavorService
{
    Task<IReadOnlyList<IceCreamFlavor>> GetAvailableFlavorsAsync(CancellationToken cancellationToken);
}
