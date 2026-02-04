namespace IceCreamM12.Web.Models;

public class ProductReplaceRequest
{
    public int OriginalProductId { get; set; }

    public int ReplacementProductId { get; set; }

    public int Quantity { get; set; }

    public string Reason { get; set; } = string.Empty;
}
