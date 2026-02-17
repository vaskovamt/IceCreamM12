using IceCreamM12.Domain.Entities;

namespace IceCreamM12.Application.Models;

public class OwnerDashboardData
{
    public int PendingOrdersCount { get; set; }
    public int ApprovedOrdersCount { get; set; }
    public int RejectedOrdersCount { get; set; }
    public int TotalOrdersCount { get; set; }
    public int TotalProducts { get; set; }
    public int TotalInventoryUnits { get; set; }
    public decimal PendingOrdersAmount { get; set; }
    public decimal ApprovedOrdersAmount { get; set; }
    public List<Product> LowStockProducts { get; set; } = [];
    public List<Order> LatestOrders { get; set; } = [];
    public List<InventoryAudit> RecentAudits { get; set; } = [];
}

public class WorkerDashboardData
{
    public int PendingOrdersCount { get; set; }
    public List<Product> LowStockProducts { get; set; } = [];
    public int TodayOperationsCount { get; set; }
    public List<Order> Orders { get; set; } = [];
    public List<InventoryAudit> Operations { get; set; } = [];
    public List<InventoryItem> InventoryItems { get; set; } = [];
}

public class DailyCheckResult
{
    public int ProductId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public int SystemQuantity { get; set; }
    public int CountedQuantity { get; set; }
    public int Difference => CountedQuantity - SystemQuantity;
    public bool HasMismatch => Difference != 0;
}

public class IngredientDailyCheckResult
{
    public int IngredientId { get; set; }
    public string IngredientName { get; set; } = string.Empty;
    public decimal SystemQuantity { get; set; }
    public decimal CountedQuantity { get; set; }
    public string Unit { get; set; } = string.Empty;
    public decimal Difference => CountedQuantity - SystemQuantity;
    public bool HasMismatch => Difference != 0;
}
