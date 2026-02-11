using System.ComponentModel.DataAnnotations;

namespace IceCreamM12.Web.Models;

public class ClientOrderRequest
{
    [Required]
    [Display(Name = "Product")]
    public int ProductId { get; set; }

    [Range(1, 100)]
    public int Quantity { get; set; } = 1;

    [StringLength(200)]
    [Display(Name = "Customer name")]
    public string? CustomerName { get; set; }

    [EmailAddress]
    [StringLength(320)]
    [Display(Name = "Customer email")]
    public string? CustomerEmail { get; set; }
}
