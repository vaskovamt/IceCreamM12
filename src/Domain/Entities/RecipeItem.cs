using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace IceCreamM12.Domain.Entities;

public class RecipeItem
{
    [Required]
    public int ProductId { get; set; }

    public Product? Product { get; set; }

    [Required]
    public int IngredientId { get; set; }

    public Ingredient? Ingredient { get; set; }

    [Column(TypeName = "decimal(10,2)")]
    public decimal Quantity { get; set; }

    [Required]
    [MaxLength(50)]
    public string Unit { get; set; } = string.Empty;
}
