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

    [Required(ErrorMessage = "ЕИК е задължителен за издаване на фактура.")]
    [RegularExpression(@"^(\d{9}|\d{13})$", ErrorMessage = "ЕИК трябва да е 9 или 13 цифри.")]
    [Display(Name = "ЕИК")]
    public string CompanyEik { get; set; } = string.Empty;

    [Required(ErrorMessage = "Адрес за фактура е задължителен.")]
    [StringLength(150)]
    [Display(Name = "Адрес за фактура")]
    public string InvoiceAddress { get; set; } = string.Empty;

    [Required(ErrorMessage = "Изберете начин на плащане.")]
    [StringLength(50)]
    [Display(Name = "Начин на плащане")]
    public string PaymentMethod { get; set; } = string.Empty;

    [StringLength(20)]
    [Display(Name = "ДДС номер (по избор)")]
    public string? VatNumber { get; set; }

    [Phone]
    [StringLength(20)]
    [Display(Name = "Телефон за контакт")]
    public string? ContactPhone { get; set; }

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
