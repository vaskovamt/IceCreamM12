using IceCreamM12.Application.Interfaces;
using IceCreamM12.Domain.Entities;
using IceCreamM12.Domain.Interfaces;

namespace IceCreamM12.Application.Services;

public class FlavorService : IFlavorService
{
    private readonly IFlavorRepository _flavorRepository;

    public FlavorService(IFlavorRepository flavorRepository)
    {
        _flavorRepository = flavorRepository;
    }

    public Task<IReadOnlyList<IceCreamFlavor>> GetAvailableFlavorsAsync(CancellationToken cancellationToken)
    {
        return _flavorRepository.GetAllAsync(cancellationToken);
    }
}
