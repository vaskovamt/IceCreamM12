using IceCreamM12.Domain.Entities;
using IceCreamM12.Domain.Identity;
using IceCreamM12.Infrastructure.Data;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

namespace IceCreamM12.Infrastructure.Identity;

public static class DbInitializer
{
    public static async Task SeedAsync(
        ApplicationDbContext context,
        UserManager<ApplicationUser> userManager,
        RoleManager<IdentityRole> roleManager,
        IConfiguration configuration)
    {
        await context.Database.MigrateAsync();

        string[] roles = ["Admin", "User"];

        foreach (string role in roles)
        {
            if (!await roleManager.RoleExistsAsync(role))
            {
                await roleManager.CreateAsync(new IdentityRole(role));
            }
        }

        if (!await context.IceCreamFlavors.AnyAsync())
        {
            context.IceCreamFlavors.AddRange(
                new IceCreamFlavor { Name = "Vanilla Bean", Price = 3.99m, IsSeasonal = false },
                new IceCreamFlavor { Name = "Chocolate Fudge", Price = 4.49m, IsSeasonal = false },
                new IceCreamFlavor { Name = "Strawberry Swirl", Price = 4.29m, IsSeasonal = false },
                new IceCreamFlavor { Name = "Pumpkin Spice", Price = 4.79m, IsSeasonal = true }
            );

            await context.SaveChangesAsync();
        }

        string adminEmail = configuration["Seed:AdminEmail"] ?? "admin@icecreamm12.local";
        string adminPassword = configuration["Seed:AdminPassword"] ?? "ChangeMe123!";

        ApplicationUser? adminUser = await userManager.FindByEmailAsync(adminEmail);
        if (adminUser is null)
        {
            adminUser = new ApplicationUser
            {
                UserName = adminEmail,
                Email = adminEmail,
                DisplayName = "Administrator",
                EmailConfirmed = true
            };

            IdentityResult createResult = await userManager.CreateAsync(adminUser, adminPassword);
            if (!createResult.Succeeded)
            {
                string errors = string.Join(", ",
                    createResult.Errors.Select(error => error.Description));
                throw new InvalidOperationException($"Failed to create admin user: {errors}");
            }
        }

        if (!await userManager.IsInRoleAsync(adminUser, "Admin"))
        {
            await userManager.AddToRoleAsync(adminUser, "Admin");
        }
    }
}
