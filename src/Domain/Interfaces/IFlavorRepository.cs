using IceCreamM12.Domain.Entities;

namespace IceCreamM12.Domain.Interfaces;

public interface IFlavorRepository
{
    Task<IReadOnlyList<IceCreamFlavor>> GetAllAsync(CancellationToken cancellationToken);
}
