using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace IceCreamM12.Domain.Entities;

public class Product
{
    [Key]
    public int Id { get; set; }

    [Required]
    [MaxLength(200)]
    public string Name { get; set; } = string.Empty;

    [MaxLength(1000)]
    public string? Description { get; set; }

    [Column(TypeName = "decimal(10,2)")]
    public decimal Price { get; set; }

    [Required]
    public int CategoryId { get; set; }

    public Category? Category { get; set; }

    public ICollection<RecipeItem> RecipeItems { get; set; } = new List<RecipeItem>();

    public InventoryItem? InventoryItem { get; set; }
}
