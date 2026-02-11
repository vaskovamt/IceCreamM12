using System.ComponentModel.DataAnnotations;
using IceCreamM12.Domain.Entities;

namespace IceCreamM12.Web.Models.ViewModels;

public class NewOrderViewModel
{
    [Required]
    [Display(Name = "Продукт")]
    public int ProductId { get; set; }

    [Range(1, 1000, ErrorMessage = "Количеството трябва да е по-голямо от 0.")]
    [Display(Name = "Количество")]
    public int Quantity { get; set; } = 1;

    [StringLength(200)]
    [Display(Name = "Име")]
    public string? CustomerName { get; set; }

    [EmailAddress]
    [Display(Name = "Имейл")]
    public string? CustomerEmail { get; set; }

    public List<Product> AvailableProducts { get; set; } = [];
}

public class MyOrdersViewModel
{
    public List<Order> Orders { get; set; } = [];
}
