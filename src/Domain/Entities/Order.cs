using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace IceCreamM12.Domain.Entities;

public class Order
{
    [Key]
    public int Id { get; set; }

    [Required]
    [MaxLength(50)]
    public string OrderNumber { get; set; } = string.Empty;

    public DateTime OrderedAt { get; set; }

    [MaxLength(100)]
    public string Status { get; set; } = "Pending";

    [Required]
    [MaxLength(200)]
    public string CustomerName { get; set; } = string.Empty;

    [MaxLength(320)]
    public string? CustomerEmail { get; set; }

    [Required]
    [MaxLength(15)]
    public string CompanyEik { get; set; } = string.Empty;

    [Required]
    [MaxLength(150)]
    public string InvoiceAddress { get; set; } = string.Empty;

    [Required]
    [MaxLength(50)]
    public string PaymentMethod { get; set; } = string.Empty;

    [MaxLength(20)]
    public string? VatNumber { get; set; }

    [MaxLength(20)]
    public string? ContactPhone { get; set; }

    [Column(TypeName = "decimal(12,2)")]
    public decimal TotalAmount { get; set; }

    public ICollection<OrderItem> Items { get; set; } = new List<OrderItem>();
}
