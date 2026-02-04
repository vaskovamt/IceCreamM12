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
    public DbSet<Product> Products => Set<Product>();
    public DbSet<Category> Categories => Set<Category>();
    public DbSet<Ingredient> Ingredients => Set<Ingredient>();
    public DbSet<RecipeItem> RecipeItems => Set<RecipeItem>();
    public DbSet<InventoryItem> InventoryItems => Set<InventoryItem>();
    public DbSet<InventoryAudit> InventoryAudits => Set<InventoryAudit>();
    public DbSet<Order> Orders => Set<Order>();
    public DbSet<OrderItem> OrderItems => Set<OrderItem>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.Entity<RecipeItem>()
            .HasKey(recipeItem => new { recipeItem.ProductId, recipeItem.IngredientId });

        builder.Entity<RecipeItem>()
            .HasOne(recipeItem => recipeItem.Product)
            .WithMany(product => product.RecipeItems)
            .HasForeignKey(recipeItem => recipeItem.ProductId);

        builder.Entity<RecipeItem>()
            .HasOne(recipeItem => recipeItem.Ingredient)
            .WithMany(ingredient => ingredient.RecipeItems)
            .HasForeignKey(recipeItem => recipeItem.IngredientId);

        builder.Entity<InventoryItem>()
            .HasOne(inventoryItem => inventoryItem.Product)
            .WithOne(product => product.InventoryItem)
            .HasForeignKey<InventoryItem>(inventoryItem => inventoryItem.ProductId);
    }
}
