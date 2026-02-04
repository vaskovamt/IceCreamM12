namespace IceCreamM12.Domain.Entities;

public class IceCreamFlavor
{
    public int Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public decimal Price { get; set; }

    public bool IsSeasonal { get; set; }
}
