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
        if ((await context.Database.GetMigrationsAsync()).Any())
        {
            await context.Database.MigrateAsync();
        }
        else
        {
            await context.Database.EnsureCreatedAsync();
        }

        string[] roles = ["Owner", "Worker", "Client"];

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
                new IceCreamFlavor { Name = "Mint Chip", Price = 4.39m, IsSeasonal = false },
                new IceCreamFlavor { Name = "Pumpkin Spice", Price = 4.79m, IsSeasonal = true }
            );

            await context.SaveChangesAsync();
        }

        if (!await context.Categories.AnyAsync())
        {
            context.Categories.AddRange(
                new Category { Name = "Scoops", Description = "Hand-scooped ice cream servings." },
                new Category { Name = "Cones", Description = "Ice cream served in cones." },
                new Category { Name = "Pints", Description = "Take-home pints." },
                new Category { Name = "Sundaes", Description = "Signature sundaes with toppings." }
            );

            await context.SaveChangesAsync();
        }

        if (!await context.Ingredients.AnyAsync())
        {
            context.Ingredients.AddRange(
                new Ingredient { Name = "Whole Milk", Unit = "L", CostPerUnit = 0.95m },
                new Ingredient { Name = "Heavy Cream", Unit = "L", CostPerUnit = 1.45m },
                new Ingredient { Name = "Granulated Sugar", Unit = "kg", CostPerUnit = 0.85m },
                new Ingredient { Name = "Vanilla Extract", Unit = "ml", CostPerUnit = 0.12m },
                new Ingredient { Name = "Stabilizer", Unit = "g", CostPerUnit = 0.05m },
                new Ingredient { Name = "Chocolate Chips", Unit = "kg", CostPerUnit = 3.25m }
            );

            await context.SaveChangesAsync();
        }

        if (!await context.Products.AnyAsync())
        {
            Dictionary<string, Category> categories = await context.Categories
                .ToDictionaryAsync(category => category.Name);

            List<Product> products = new();

            products.AddRange(
            [
                new Product
                {
                    Name = "Classic Vanilla Scoop",
                    Description = "Signature vanilla ice cream scoop.",
                    Price = 3.50m,
                    CategoryId = categories["Scoops"].Id
                },
                new Product
                {
                    Name = "Chocolate Fudge Sundae",
                    Description = "Rich chocolate ice cream with fudge drizzle.",
                    Price = 6.25m,
                    CategoryId = categories["Sundaes"].Id
                },
                new Product
                {
                    Name = "Strawberry Swirl Cone",
                    Description = "Strawberry swirl served in a crispy cone.",
                    Price = 4.75m,
                    CategoryId = categories["Cones"].Id
                }
            ]);

            string[] randomFlavors =
            [
                "Vanilla Bean",
                "Chocolate Fudge",
                "Strawberry Swirl",
                "Mint Chip",
                "Salted Caramel",
                "Cookies & Cream"
            ];

            (string Portion, decimal Price, string Category)[] portions =
            [
                ("Single Scoop", 3.25m, "Scoops"),
                ("Double Scoop", 4.75m, "Scoops"),
                ("Waffle Cone", 4.50m, "Cones"),
                ("Pint", 7.50m, "Pints"),
                ("Sundae", 6.75m, "Sundaes")
            ];

            HashSet<string> productNames = new(products.Select(product => product.Name));
            Random random = new(42);

            while (productNames.Count < 9)
            {
                string flavor = randomFlavors[random.Next(randomFlavors.Length)];
                (string Portion, decimal Price, string Category) = portions[random.Next(portions.Length)];
                string name = $"{flavor} {Portion}";

                if (!productNames.Add(name))
                {
                    continue;
                }

                products.Add(new Product
                {
                    Name = name,
                    Description = $"Season-ready {flavor.ToLowerInvariant()} in a {Portion.ToLowerInvariant()}.",
                    Price = Price,
                    CategoryId = categories[Category].Id
                });
            }

            context.Products.AddRange(products);
            await context.SaveChangesAsync();
        }

        if (!await context.RecipeItems.AnyAsync())
        {
            Product? baseProduct = await context.Products
                .FirstOrDefaultAsync(product => product.Name == "Classic Vanilla Scoop");

            if (baseProduct is not null)
            {
                Dictionary<string, Ingredient> ingredients = await context.Ingredients
                    .ToDictionaryAsync(ingredient => ingredient.Name);

                context.RecipeItems.AddRange(
                    new RecipeItem
                    {
                        ProductId = baseProduct.Id,
                        IngredientId = ingredients["Whole Milk"].Id,
                        Quantity = 0.35m,
                        Unit = "L"
                    },
                    new RecipeItem
                    {
                        ProductId = baseProduct.Id,
                        IngredientId = ingredients["Heavy Cream"].Id,
                        Quantity = 0.20m,
                        Unit = "L"
                    },
                    new RecipeItem
                    {
                        ProductId = baseProduct.Id,
                        IngredientId = ingredients["Granulated Sugar"].Id,
                        Quantity = 0.12m,
                        Unit = "kg"
                    },
                    new RecipeItem
                    {
                        ProductId = baseProduct.Id,
                        IngredientId = ingredients["Vanilla Extract"].Id,
                        Quantity = 15m,
                        Unit = "ml"
                    },
                    new RecipeItem
                    {
                        ProductId = baseProduct.Id,
                        IngredientId = ingredients["Stabilizer"].Id,
                        Quantity = 6m,
                        Unit = "g"
                    }
                );

                await context.SaveChangesAsync();
            }
        }

        await EnsureUserWithRoleAsync(
            userManager,
            roleManager,
            configuration["Seed:OwnerEmail"] ?? "owner@icecreamm12.local",
            configuration["Seed:OwnerPassword"] ?? "Owner123!",
            "Owner",
            "Store Owner");

        await EnsureUserWithRoleAsync(
            userManager,
            roleManager,
            configuration["Seed:WorkerEmail"] ?? "worker@icecreamm12.local",
            configuration["Seed:WorkerPassword"] ?? "Worker123!",
            "Worker",
            "Store Worker");

        await EnsureUserWithRoleAsync(
            userManager,
            roleManager,
            configuration["Seed:ClientEmail"] ?? "client@icecreamm12.local",
            configuration["Seed:ClientPassword"] ?? "Client123!",
            "Client",
            "Sample Client");
    }

    private static async Task EnsureUserWithRoleAsync(
        UserManager<ApplicationUser> userManager,
        RoleManager<IdentityRole> roleManager,
        string email,
        string password,
        string role,
        string displayName)
    {
        if (!await roleManager.RoleExistsAsync(role))
        {
            await roleManager.CreateAsync(new IdentityRole(role));
        }

        ApplicationUser? user = await userManager.FindByEmailAsync(email);
        if (user is null)
        {
            user = new ApplicationUser
            {
                UserName = email,
                Email = email,
                DisplayName = displayName,
                EmailConfirmed = true
            };

            IdentityResult createResult = await userManager.CreateAsync(user, password);
            if (!createResult.Succeeded)
            {
                string errors = string.Join(", ",
                    createResult.Errors.Select(error => error.Description));
                throw new InvalidOperationException($"Failed to create user '{email}': {errors}");
            }
        }
        else if (!await userManager.CheckPasswordAsync(user, password))
        {
            string resetToken = await userManager.GeneratePasswordResetTokenAsync(user);
            IdentityResult resetResult = await userManager.ResetPasswordAsync(user, resetToken, password);
            if (!resetResult.Succeeded)
            {
                string errors = string.Join(", ",
                    resetResult.Errors.Select(error => error.Description));
                throw new InvalidOperationException($"Failed to reset password for '{email}': {errors}");
            }
        }

        if (!await userManager.IsInRoleAsync(user, role))
        {
            await userManager.AddToRoleAsync(user, role);
        }
    }
}
