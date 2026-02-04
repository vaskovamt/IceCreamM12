using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace IceCreamM12.Domain.Entities;

public class Ingredient
{
    [Key]
    public int Id { get; set; }

    [Required]
    [MaxLength(200)]
    public string Name { get; set; } = string.Empty;

    [Required]
    [MaxLength(50)]
    public string Unit { get; set; } = string.Empty;

    [Column(TypeName = "decimal(10,2)")]
    public decimal CostPerUnit { get; set; }

    public ICollection<RecipeItem> RecipeItems { get; set; } = new List<RecipeItem>();
}
