namespace IceCreamM12.Web.Models;

public class InventorySwapRequest
{
    public int FromProductId { get; set; }

    public int ToProductId { get; set; }

    public int Quantity { get; set; }

    public string Reason { get; set; } = string.Empty;
}
