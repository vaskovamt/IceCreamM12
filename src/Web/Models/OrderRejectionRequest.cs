namespace IceCreamM12.Web.Models;

public class OrderRejectionRequest
{
    public int OrderId { get; set; }

    public string RejectionReason { get; set; } = string.Empty;
}
