using IceCreamM12.Application.Interfaces;
using IceCreamM12.Domain.Entities;

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
