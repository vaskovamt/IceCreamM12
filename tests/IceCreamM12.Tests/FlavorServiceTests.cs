using IceCreamM12.Application.Interfaces;
using IceCreamM12.Application.Services;
using IceCreamM12.Domain.Entities;
using Xunit;

namespace IceCreamM12.Tests;

public class FlavorServiceTests
{
    [Fact]
    public async Task GetAvailableFlavorsAsync_ReturnsAllFlavors()
    {
        var flavors = new List<IceCreamFlavor>
        {
            new() { Id = 1, Name = "Vanilla", Price = 3.50m, IsSeasonal = false },
            new() { Id = 2, Name = "Mint", Price = 4.00m, IsSeasonal = true }
        };

        IFlavorRepository repository = new InMemoryFlavorRepository(flavors);
        var service = new FlavorService(repository);

        IReadOnlyList<IceCreamFlavor> result = await service.GetAvailableFlavorsAsync(CancellationToken.None);

        Assert.Equal(2, result.Count);
        Assert.Equal("Mint", result[1].Name);
    }

    private sealed class InMemoryFlavorRepository : IFlavorRepository
    {
        private readonly IReadOnlyList<IceCreamFlavor> _flavors;

        public InMemoryFlavorRepository(IReadOnlyList<IceCreamFlavor> flavors)
        {
            _flavors = flavors;
        }

        public Task<IReadOnlyList<IceCreamFlavor>> GetAllAsync(CancellationToken cancellationToken)
        {
            return Task.FromResult(_flavors);
        }
    }
}
