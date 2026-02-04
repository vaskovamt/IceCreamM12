using IceCreamM12.Application.Interfaces;
using IceCreamM12.Domain.Entities;
using IceCreamM12.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace IceCreamM12.Infrastructure.Repositories;

public class FlavorRepository : IFlavorRepository
{
    private readonly ApplicationDbContext _dbContext;

    public FlavorRepository(ApplicationDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<IReadOnlyList<IceCreamFlavor>> GetAllAsync(CancellationToken cancellationToken)
    {
        return await _dbContext.IceCreamFlavors
            .AsNoTracking()
            .OrderBy(flavor => flavor.Name)
            .ToListAsync(cancellationToken);
    }
}
