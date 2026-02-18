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
        IEnumerable<string> availableMigrations = context.Database.GetMigrations();
        if (availableMigrations.Any())
        {
            await context.Database.MigrateAsync();
        }
        else
        {
            await context.Database.EnsureCreatedAsync();
        }

        await EnsureOrderInvoiceColumnsAsync(context);

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

        Dictionary<string, Category> categoriesByName = await LoadCategoriesByNameAsync(context);

        if (!categoriesByName.ContainsKey("IceCream") || !categoriesByName.ContainsKey("Cones") || !categoriesByName.ContainsKey("ingridients"))
        {
            if (!categoriesByName.ContainsKey("IceCream"))
            {
                context.Categories.Add(new Category { Name = "IceCream", Description = "All ice cream products." });
            }

            if (!categoriesByName.ContainsKey("Cones"))
            {
                context.Categories.Add(new Category { Name = "Cones", Description = "All cone products." });
            }

            if (!categoriesByName.ContainsKey("ingridients"))
            {
                context.Categories.Add(new Category { Name = "ingridients", Description = "Ingredient-related items." });
            }

            await context.SaveChangesAsync();

            categoriesByName = await LoadCategoriesByNameAsync(context);
        }

        int iceCreamCategoryId = categoriesByName["IceCream"].Id;
        int conesCategoryId = categoriesByName["Cones"].Id;

        (string Name, string Unit, decimal CostPerUnit, decimal QuantityOnHand)[] ingredientCatalog =
        [
            ("Whole Milk", "L", 0.95m, 132m),
            ("Heavy Cream", "L", 1.45m, 104m),
            ("Granulated Sugar", "kg", 0.85m, 86m),
            ("Vanilla Extract", "ml", 0.12m, 3200m),
            ("Stabilizer", "g", 0.05m, 4100m),
            ("Chocolate Chips", "kg", 3.25m, 57m),
            ("Cocoa Powder", "kg", 2.10m, 48m),
            ("Strawberry Puree", "kg", 2.90m, 52m),
            ("Raspberry Puree", "kg", 3.40m, 46m),
            ("Blueberry Puree", "kg", 3.75m, 39m),
            ("Melon Puree", "kg", 2.65m, 44m),
            ("Caramel Syrup", "L", 2.30m, 61m),
            ("Hazelnut Paste", "kg", 4.80m, 29m)
        ];

        Dictionary<string, Ingredient> existingIngredientsByName = await context.Ingredients
            .ToDictionaryAsync(ingredient => ingredient.Name, StringComparer.OrdinalIgnoreCase);

        var missingIngredients = ingredientCatalog
            .Where(ingredient => !existingIngredientsByName.ContainsKey(ingredient.Name))
            .Select(ingredient => new Ingredient
            {
                Name = ingredient.Name,
                Unit = ingredient.Unit,
                CostPerUnit = ingredient.CostPerUnit,
                QuantityOnHand = ingredient.QuantityOnHand,
                ReorderLevel = Math.Round(ingredient.QuantityOnHand * 0.25m, 2, MidpointRounding.AwayFromZero),
                LastUpdatedAt = DateTime.UtcNow
            })
            .ToList();

        if (missingIngredients.Count > 0)
        {
            context.Ingredients.AddRange(missingIngredients);
            await context.SaveChangesAsync();
        }

        string[] flavors =
        [
            "ВАНИЛИЯ",
            "КАКАО",
            "ЯГОДА",
            "МАЛИНА",
            "БОРОВИНКА",
            "ПЪПЕШ",
            "КАРАМЕЛ",
            "ЛЕШНИК"
        ];

        (string Size, decimal Price)[] iceCreamSizes =
        [
            ("0.100kg", 0.87m),
            ("0.300kg", 1.79m),
            ("0.500kg", 2.66m),
            ("2.000kg", 8.95m),
            ("2.500kg", 13.04m),
            ("8.000kg", 29.14m)
        ];

        (string Name, decimal Price)[] cones =
        [
            ("Вафлено рогче", 0.05m),
            ("Вафлена Чашка", 0.10m),
            ("Захарно рогче", 0.15m)
        ];

        var seededProducts = new List<Product>();

        foreach ((string size, decimal price) in iceCreamSizes)
        {
            foreach (string flavor in flavors)
            {
                seededProducts.Add(new Product
                {
                    Name = $"Сладолед {size} - {flavor}",
                    Description = $"Размер: {size}; Вкус: {flavor}",
                    Price = price,
                    CategoryId = iceCreamCategoryId
                });
            }
        }

        foreach ((string coneName, decimal price) in cones)
        {
            seededProducts.Add(new Product
            {
                Name = $"Фунийка - {coneName}",
                Description = $"Тип фунийка: {coneName}",
                Price = price,
                CategoryId = conesCategoryId
            });
        }

        Dictionary<string, string> legacyConeNames = new()
        {
            ["Фунийка - малка"] = "Фунийка - Вафлено рогче",
            ["Фунийка - средна"] = "Фунийка - Вафлена Чашка",
            ["Фунийка - голяма"] = "Фунийка - Захарно рогче"
        };

        List<Product> legacyConeProducts = await context.Products
            .Where(product => legacyConeNames.Keys.Contains(product.Name))
            .ToListAsync();

        foreach (Product legacyConeProduct in legacyConeProducts)
        {
            if (!legacyConeNames.TryGetValue(legacyConeProduct.Name, out string? updatedName))
            {
                continue;
            }

            legacyConeProduct.Name = updatedName;
            legacyConeProduct.Description = $"Тип фунийка: {updatedName.Replace("Фунийка - ", string.Empty)}";
        }

        if (legacyConeProducts.Count > 0)
        {
            await context.SaveChangesAsync();
        }

        var productStocks = new List<(string ProductName, int Quantity)>
        {
            ("Сладолед 0.100kg - ВАНИЛИЯ", 190),
            ("Сладолед 0.100kg - КАКАО", 170),
            ("Сладолед 0.100kg - ЯГОДА", 160),
            ("Сладолед 0.100kg - МАЛИНА", 145),
            ("Сладолед 0.100kg - БОРОВИНКА", 120),
            ("Сладолед 0.100kg - ПЪПЕШ", 98),
            ("Сладолед 0.100kg - КАРАМЕЛ", 184),
            ("Сладолед 0.100kg - ЛЕШНИК", 136),
            ("Сладолед 0.300kg - ВАНИЛИЯ", 110),
            ("Сладолед 0.300kg - КАКАО", 95),
            ("Сладолед 0.300kg - ЯГОДА", 82),
            ("Сладолед 0.300kg - МАЛИНА", 76),
            ("Сладолед 0.300kg - БОРОВИНКА", 58),
            ("Сладолед 0.300kg - ПЪПЕШ", 44),
            ("Сладолед 0.300kg - КАРАМЕЛ", 104),
            ("Сладолед 0.300kg - ЛЕШНИК", 68),
            ("Сладолед 0.500kg - ВАНИЛИЯ", 70),
            ("Сладолед 0.500kg - КАКАО", 66),
            ("Сладолед 0.500kg - ЯГОДА", 54),
            ("Сладолед 0.500kg - МАЛИНА", 49),
            ("Сладолед 0.500kg - БОРОВИНКА", 42),
            ("Сладолед 0.500kg - ПЪПЕШ", 28),
            ("Сладолед 0.500kg - КАРАМЕЛ", 73),
            ("Сладолед 0.500kg - ЛЕШНИК", 37),
            ("Сладолед 2.000kg - ВАНИЛИЯ", 26),
            ("Сладолед 2.000kg - КАКАО", 24),
            ("Сладолед 2.000kg - ЯГОДА", 20),
            ("Сладолед 2.000kg - МАЛИНА", 17),
            ("Сладолед 2.000kg - БОРОВИНКА", 12),
            ("Сладолед 2.000kg - ПЪПЕШ", 9),
            ("Сладолед 2.000kg - КАРАМЕЛ", 29),
            ("Сладолед 2.000kg - ЛЕШНИК", 15),
            ("Сладолед 2.500kg - ВАНИЛИЯ", 21),
            ("Сладолед 2.500kg - КАКАО", 18),
            ("Сладолед 2.500kg - ЯГОДА", 16),
            ("Сладолед 2.500kg - МАЛИНА", 13),
            ("Сладолед 2.500kg - БОРОВИНКА", 10),
            ("Сладолед 2.500kg - ПЪПЕШ", 7),
            ("Сладолед 2.500kg - КАРАМЕЛ", 23),
            ("Сладолед 2.500kg - ЛЕШНИК", 11),
            ("Сладолед 8.000kg - ВАНИЛИЯ", 9),
            ("Сладолед 8.000kg - КАКАО", 8),
            ("Сладолед 8.000kg - ЯГОДА", 7),
            ("Сладолед 8.000kg - МАЛИНА", 5),
            ("Сладолед 8.000kg - БОРОВИНКА", 3),
            ("Сладолед 8.000kg - ПЪПЕШ", 2),
            ("Сладолед 8.000kg - КАРАМЕЛ", 10),
            ("Сладолед 8.000kg - ЛЕШНИК", 4),
            ("Фунийка - Вафлено рогче", 1320),
            ("Фунийка - Вафлена Чашка", 860),
            ("Фунийка - Захарно рогче", 420)
        };

        Dictionary<string, Product> existingProductsByName = await context.Products
            .ToDictionaryAsync(product => product.Name);

        List<Product> missingProducts = seededProducts
            .Where(product => !existingProductsByName.ContainsKey(product.Name))
            .ToList();

        if (missingProducts.Count > 0)
        {
            context.Products.AddRange(missingProducts);
            await context.SaveChangesAsync();

            foreach (Product product in missingProducts)
            {
                existingProductsByName[product.Name] = product;
            }
        }

        HashSet<int> existingInventoryProductIds = new(
            await context.InventoryItems
                .Select(inventoryItem => inventoryItem.ProductId)
                .ToListAsync());

        var missingInventoryItems = productStocks
            .Where(stock => existingProductsByName.ContainsKey(stock.ProductName))
            .Select(stock => (Product: existingProductsByName[stock.ProductName], stock.Quantity))
            .Where(entry => !existingInventoryProductIds.Contains(entry.Product.Id))
            .Select(entry => new InventoryItem
            {
                ProductId = entry.Product.Id,
                QuantityOnHand = entry.Quantity,
                ReorderLevel = Math.Max(1, entry.Quantity / 5),
                StorageLocation = "Main Freezer",
                LastUpdatedAt = DateTime.UtcNow
            })
            .ToList();

        if (missingInventoryItems.Count > 0)
        {
            context.InventoryItems.AddRange(missingInventoryItems);
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

    private static async Task<Dictionary<string, Category>> LoadCategoriesByNameAsync(ApplicationDbContext context)
    {
        List<Category> categories = await context.Categories
            .AsNoTracking()
            .OrderBy(category => category.Id)
            .ToListAsync();

        return categories
            .GroupBy(category => category.Name, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(group => group.Key, group => group.First(), StringComparer.OrdinalIgnoreCase);
    }

    private static async Task EnsureOrderInvoiceColumnsAsync(ApplicationDbContext context)
    {
        var existingColumns = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        await using var command = context.Database.GetDbConnection().CreateCommand();
        command.CommandText = "PRAGMA table_info('Orders');";

        if (command.Connection?.State != System.Data.ConnectionState.Open)
        {
            await command.Connection!.OpenAsync();
        }

        await using var reader = await command.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            if (!reader.IsDBNull(1))
            {
                existingColumns.Add(reader.GetString(1));
            }
        }

        if (!existingColumns.Contains("CompanyEik"))
        {
            await context.Database.ExecuteSqlRawAsync("ALTER TABLE Orders ADD COLUMN CompanyEik TEXT NOT NULL DEFAULT '';");
        }

        if (!existingColumns.Contains("InvoiceAddress"))
        {
            await context.Database.ExecuteSqlRawAsync("ALTER TABLE Orders ADD COLUMN InvoiceAddress TEXT NOT NULL DEFAULT '';");
        }

        if (!existingColumns.Contains("PaymentMethod"))
        {
            await context.Database.ExecuteSqlRawAsync("ALTER TABLE Orders ADD COLUMN PaymentMethod TEXT NOT NULL DEFAULT '';");
        }

        if (!existingColumns.Contains("VatNumber"))
        {
            await context.Database.ExecuteSqlRawAsync("ALTER TABLE Orders ADD COLUMN VatNumber TEXT NULL;");
        }

        if (!existingColumns.Contains("ContactPhone"))
        {
            await context.Database.ExecuteSqlRawAsync("ALTER TABLE Orders ADD COLUMN ContactPhone TEXT NULL;");
        }

        if (!existingColumns.Contains("RejectionReason"))
        {
            await context.Database.ExecuteSqlRawAsync("ALTER TABLE Orders ADD COLUMN RejectionReason TEXT NULL;");
        }
    }

}
