namespace IceCreamM12.Web.Models;

public class ProductScrapRequest
{
    public int ProductId { get; set; }

    public int Quantity { get; set; }

    public string Reason { get; set; } = string.Empty;
}
