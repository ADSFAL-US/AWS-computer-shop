namespace ElectronCash.Client.Models;

/// <summary>Товар/продукт в системе.</summary>
public class Product
{
    public int Id { get; set; }
    public string Sku { get; set; } = string.Empty;
    public string Barcode { get; set; } = string.Empty; // 13 цифр
    public string Name { get; set; } = string.Empty;
    public int CategoryId { get; set; }
    public decimal Price { get; set; }
    public int Stock { get; set; }
    public List<string> Tags { get; set; } = new();
    
    /// <summary>Два цвета для градиента плитки товара: Color1;Color2</summary>
    public string ImageGradient { get; set; } = string.Empty;
    
    /// <summary>Иконка категории (Segoe MDL2 Assets Unicode)</summary>
    public string Icon { get; set; } = string.Empty;
}
