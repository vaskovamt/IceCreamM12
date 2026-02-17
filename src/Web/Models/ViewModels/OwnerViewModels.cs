using System.ComponentModel.DataAnnotations;
using IceCreamM12.Application.Models;
using IceCreamM12.Domain.Entities;

namespace IceCreamM12.Web.Models.ViewModels;

public class OwnerDashboardViewModel
{
    public OwnerDashboardData Data { get; set; } = new();
}

public class WorkerDashboardViewModel
{
    public WorkerDashboardData Data { get; set; } = new();
}

public class OrdersManagementViewModel
{
    public string? StatusFilter { get; set; }
    public List<Order> Orders { get; set; } = [];
}

public class InventoryManagementViewModel
{
    public List<InventoryItem> InventoryItems { get; set; } = [];
    public List<Ingredient> Ingredients { get; set; } = [];
    public List<InventoryAudit> RecentAudits { get; set; } = [];

    public InventoryOperationInputModel Load { get; set; } = new();
    public InventoryOperationInputModel Scrap { get; set; } = new();
    public InventoryReplaceInputModel Replace { get; set; } = new();
}

public class InventoryOperationInputModel
{
    [Required]
    public int ProductId { get; set; }

    [Range(1, int.MaxValue)]
    public int Quantity { get; set; }

    [Required(ErrorMessage = "Причината е задължителна.")]
    public string Reason { get; set; } = string.Empty;
}

public class InventoryReplaceInputModel
{
    [Required]
    public int FromProductId { get; set; }

    [Required]
    public int ToProductId { get; set; }

    [Range(1, int.MaxValue)]
    public int Quantity { get; set; }

    [Required(ErrorMessage = "Причината е задължителна.")]
    public string Reason { get; set; } = string.Empty;
}

public class DailyCheckViewModel
{
    public List<DailyCheckItemInputModel> Items { get; set; } = [];
    public List<DailyCheckResult> Results { get; set; } = [];
}

public class DailyCheckItemInputModel
{
    public int ProductId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public int SystemQuantity { get; set; }

    [Range(0, int.MaxValue)]
    public int CountedQuantity { get; set; }
}
