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


public class UserManagementViewModel
{
    public List<UserManagementItem> Users { get; set; } = [];
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
    public InventoryEntityType ItemType { get; set; } = InventoryEntityType.Product;

    public int? ProductId { get; set; }

    public int? IngredientId { get; set; }

    [Range(typeof(decimal), "0,01", "79228162514264337593543950335")]
    public decimal Quantity { get; set; }

    [Required(ErrorMessage = "Причината е задължителна.")]
    public string Reason { get; set; } = string.Empty;
}

public class InventoryReplaceInputModel
{
    [Required]
    public InventoryEntityType FromItemType { get; set; } = InventoryEntityType.Product;

    public int? FromProductId { get; set; }

    public int? FromIngredientId { get; set; }

    [Required]
    public InventoryEntityType ToItemType { get; set; } = InventoryEntityType.Product;

    public int? ToProductId { get; set; }

    public int? ToIngredientId { get; set; }

    [Range(typeof(decimal), "0,1", "79228162514264337593543950335")]
    public decimal Quantity { get; set; }

    [Required(ErrorMessage = "Причината е задължителна.")]
    public string Reason { get; set; } = string.Empty;
}

public enum InventoryEntityType
{
    Product = 1,
    Ingredient = 2
}

public class DailyCheckViewModel
{
    public List<DailyCheckItemInputModel> Items { get; set; } = [];
    public List<IngredientDailyCheckItemInputModel> IngredientItems { get; set; } = [];
    public List<DailyCheckResult> Results { get; set; } = [];
    public List<IngredientDailyCheckResult> IngredientResults { get; set; } = [];
}

public class ProductionBatchViewModel
{
    public List<IngredientProductionInputModel> IngredientInputs { get; set; } = [];
    public List<ProductProductionInputModel> ProductInputs { get; set; } = [];
}

public class IngredientProductionInputModel
{
    public int IngredientId { get; set; }
    public string IngredientName { get; set; } = string.Empty;
    public string Unit { get; set; } = string.Empty;
    public decimal AvailableQuantity { get; set; }

    [Range(typeof(decimal), "0", "79228162514264337593543950335")]
    public decimal UsedQuantity { get; set; }
}

public class ProductProductionInputModel
{
    public int ProductId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public int AvailableQuantity { get; set; }

    [Range(0, int.MaxValue)]
    public int ProducedQuantity { get; set; }
}

public class DailyCheckItemInputModel
{
    public int ProductId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public int SystemQuantity { get; set; }

    [Range(0, int.MaxValue)]
    public int CountedQuantity { get; set; }
}

public class IngredientDailyCheckItemInputModel
{
    public int IngredientId { get; set; }
    public string IngredientName { get; set; } = string.Empty;
    public string Unit { get; set; } = string.Empty;
    public decimal SystemQuantity { get; set; }

    [Range(typeof(decimal), "0", "79228162514264337593543950335")]
    public decimal CountedQuantity { get; set; }
}
