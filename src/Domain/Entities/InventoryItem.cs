using System.ComponentModel.DataAnnotations;

namespace IceCreamM12.Domain.Entities;

public class InventoryItem
{
    [Key]
    public int Id { get; set; }

    [Required]
    public int ProductId { get; set; }

    public Product? Product { get; set; }

    public int QuantityOnHand { get; set; }

    public int ReorderLevel { get; set; }

    [MaxLength(200)]
    public string? StorageLocation { get; set; }

    public DateTime LastUpdatedAt { get; set; }

    public ICollection<InventoryAudit> Audits { get; set; } = new List<InventoryAudit>();
}
