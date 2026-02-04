using System.ComponentModel.DataAnnotations;

namespace IceCreamM12.Domain.Entities;

public class InventoryAudit
{
    [Key]
    public int Id { get; set; }

    [Required]
    public int InventoryItemId { get; set; }

    public InventoryItem? InventoryItem { get; set; }

    public int QuantityChange { get; set; }

    [Required]
    [MaxLength(250)]
    public string Reason { get; set; } = string.Empty;

    public DateTime PerformedAt { get; set; }

    [MaxLength(450)]
    public string? PerformedByUserId { get; set; }
}
