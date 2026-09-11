namespace BarcodePrinter;
public sealed class Product
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Name { get; set; } = "";
    public string Barcode { get; set; } = "";
    public string Sku { get; set; } = "";
    public string Category { get; set; } = "Genel";
    public decimal Cost { get; set; }
    public decimal Price { get; set; }
    public decimal? OldPrice { get; set; }
    public decimal Stock { get; set; }
    public decimal Minimum { get; set; } = 5;
    public decimal Maximum { get; set; } = 100;
    public string Unit { get; set; } = "Adet";
    public string Description { get; set; } = "";
    public string ImagePath { get; set; } = "";
    public string SourceImage { get; set; } = "";
    public byte[]? ImageData { get; set; }
    public Dictionary<string, string?> SourceFields { get; set; } = new(StringComparer.OrdinalIgnoreCase);
    public bool OnMenu { get; set; } = true;
    public bool Active { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.Now;
    public DateTime UpdatedAt { get; set; } = DateTime.Now;
    public Product Copy()
    {
        var copy = (Product)MemberwiseClone();
        copy.SourceFields = new Dictionary<string, string?>(SourceFields, StringComparer.OrdinalIgnoreCase);
        copy.ImageData = ImageData?.ToArray();
        return copy;
    }
}
public sealed class Movement
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid ProductId { get; set; }
    public string ProductName { get; set; } = "";
    public string Barcode { get; set; } = "";
    public string Kind { get; set; } = "";
    public decimal Before { get; set; }
    public decimal Delta { get; set; }
    public decimal After { get; set; }
    public string Note { get; set; } = "";
    public DateTime At { get; set; } = DateTime.Now;
}
public sealed class InventoryData
{
    public string Revision { get; set; } = "";
    public List<Product> Products { get; set; } = [];
    public List<Movement> Movements { get; set; } = [];
}
