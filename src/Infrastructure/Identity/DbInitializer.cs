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
        IEnumerable<string> pending = await context.Database.GetPendingMigrationsAsync();
        if (pending.Any())
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
            string[] flavors =
            [
                "ВАНИЛИЯ",
                "КАКАО",
                "ЯГОДА",
                "МАЛИНА",
                "БОРОВНИКА",
                "ПЪПЕШ",
                "КАРАМЕЛ",
                "ЛЕШНИК"
            ];

            (string Size, decimal Price, int CategoryId)[] iceCreamSizes =
            [
                ("0.100kg", 0.87m, 1),
                ("0.300kg", 0.00m, 1),
                ("0.500kg", 2.66m, 1),
                ("2.000kg", 8.95m, 1),
                ("2.500kg", 13.04m, 1),
                ("8.000kg", 29.14m, 1)
            ];

            (string Size, decimal Price, int CategoryId)[] cones =
            [
                ("малка", 0.05m, 2),
                ("средна", 0.10m, 2),
                ("голяма", 0.15m, 2)
            ];

            var products = new List<Product>();

            foreach ((string size, decimal price, int categoryId) in iceCreamSizes)
            {
                foreach (string flavor in flavors)
                {
                    string description = $"Размер: {size}; Вкус: {flavor}";
                    if (size == "0.300kg")
                    {
                        description = $"{description}; PRICE_TBD";
                    }

                    products.Add(new Product
                    {
                        Name = $"Сладолед {size} - {flavor}",
                        Description = description,
                        Price = price,
                        CategoryId = categoryId
                    });
                }
            }

            foreach ((string size, decimal price, int categoryId) in cones)
            {
                products.Add(new Product
                {
                    Name = $"Фунийка - {size}",
                    Description = $"Размер: {size}",
                    Price = price,
                    CategoryId = categoryId
                });
            }

            var productStocks = new List<(string ProductName, int Quantity)>
            {
                ("Сладолед 0.100kg - ВАНИЛИЯ", 190),
                ("Сладолед 0.100kg - КАКАО", 170),
                ("Сладолед 0.100kg - ЯГОДА", 160),
                ("Сладолед 0.100kg - МАЛИНА", 145),
                ("Сладолед 0.100kg - БОРОВНИКА", 120),
                ("Сладолед 0.100kg - ПЪПЕШ", 98),
                ("Сладолед 0.100kg - КАРАМЕЛ", 184),
                ("Сладолед 0.100kg - ЛЕШНИК", 136),
                ("Сладолед 0.300kg - ВАНИЛИЯ", 110),
                ("Сладолед 0.300kg - КАКАО", 95),
                ("Сладолед 0.300kg - ЯГОДА", 82),
                ("Сладолед 0.300kg - МАЛИНА", 76),
                ("Сладолед 0.300kg - БОРОВНИКА", 58),
                ("Сладолед 0.300kg - ПЪПЕШ", 44),
                ("Сладолед 0.300kg - КАРАМЕЛ", 104),
                ("Сладолед 0.300kg - ЛЕШНИК", 68),
                ("Сладолед 0.500kg - ВАНИЛИЯ", 70),
                ("Сладолед 0.500kg - КАКАО", 66),
                ("Сладолед 0.500kg - ЯГОДА", 54),
                ("Сладолед 0.500kg - МАЛИНА", 49),
                ("Сладолед 0.500kg - БОРОВНИКА", 42),
                ("Сладолед 0.500kg - ПЪПЕШ", 28),
                ("Сладолед 0.500kg - КАРАМЕЛ", 73),
                ("Сладолед 0.500kg - ЛЕШНИК", 37),
                ("Сладолед 2.000kg - ВАНИЛИЯ", 26),
                ("Сладолед 2.000kg - КАКАО", 24),
                ("Сладолед 2.000kg - ЯГОДА", 20),
                ("Сладолед 2.000kg - МАЛИНА", 17),
                ("Сладолед 2.000kg - БОРОВНИКА", 12),
                ("Сладолед 2.000kg - ПЪПЕШ", 9),
                ("Сладолед 2.000kg - КАРАМЕЛ", 29),
                ("Сладолед 2.000kg - ЛЕШНИК", 15),
                ("Сладолед 2.500kg - ВАНИЛИЯ", 21),
                ("Сладолед 2.500kg - КАКАО", 18),
                ("Сладолед 2.500kg - ЯГОДА", 16),
                ("Сладолед 2.500kg - МАЛИНА", 13),
                ("Сладолед 2.500kg - БОРОВНИКА", 10),
                ("Сладолед 2.500kg - ПЪПЕШ", 7),
                ("Сладолед 2.500kg - КАРАМЕЛ", 23),
                ("Сладолед 2.500kg - ЛЕШНИК", 11),
                ("Сладолед 8.000kg - ВАНИЛИЯ", 9),
                ("Сладолед 8.000kg - КАКАО", 8),
                ("Сладолед 8.000kg - ЯГОДА", 7),
                ("Сладолед 8.000kg - МАЛИНА", 5),
                ("Сладолед 8.000kg - БОРОВНИКА", 3),
                ("Сладолед 8.000kg - ПЪПЕШ", 2),
                ("Сладолед 8.000kg - КАРАМЕЛ", 10),
                ("Сладолед 8.000kg - ЛЕШНИК", 4),
                ("Фунийка - малка", 1320),
                ("Фунийка - средна", 860),
                ("Фунийка - голяма", 420)
            };

            context.Products.AddRange(products);
            await context.SaveChangesAsync();

            var productsByName = await context.Products.ToDictionaryAsync(product => product.Name);
            var inventoryItems = productStocks
                .Where(stock => productsByName.ContainsKey(stock.ProductName))
                .Select(stock => new InventoryItem
                {
                    ProductId = productsByName[stock.ProductName].Id,
                    QuantityOnHand = stock.Quantity,
                    ReorderLevel = Math.Max(1, stock.Quantity / 5),
                    StorageLocation = "Main Freezer",
                    LastUpdatedAt = DateTime.UtcNow
                })
                .ToList();

            context.InventoryItems.AddRange(inventoryItems);
            await context.SaveChangesAsync();
        }

        if (!await context.RecipeItems.AnyAsync())
        {
            Product? baseProduct = await context.Products
                .FirstOrDefaultAsync(product => product.Name == "Сладолед 0.100kg - ВАНИЛИЯ");

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
