using System.ComponentModel.DataAnnotations;
using IceCreamM12.Domain.Entities;

namespace IceCreamM12.Web.Models.ViewModels;

public class NewOrderViewModel
{
    public List<NewOrderItemViewModel> Items { get; set; } = [new()];

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

public class NewOrderItemViewModel
{
    [Required]
    [Display(Name = "Продукт")]
    public int ProductId { get; set; }

    [Range(0, 1000, ErrorMessage = "Количеството не може да е отрицателно.")]
    [Display(Name = "Количество")]
    public int Quantity { get; set; } = 1;
}
