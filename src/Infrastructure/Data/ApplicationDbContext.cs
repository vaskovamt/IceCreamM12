using IceCreamM12.Domain.Entities;
using IceCreamM12.Domain.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace IceCreamM12.Infrastructure.Data;

public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }

    public DbSet<IceCreamFlavor> IceCreamFlavors => Set<IceCreamFlavor>();
}
