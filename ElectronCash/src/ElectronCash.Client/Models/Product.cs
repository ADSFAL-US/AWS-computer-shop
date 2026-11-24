namespace ElectronCash.Client.Models;

/// <summary>Товар/продукт в системе.</summary>
public class Product
{
    public int Id { get; set; }
    public string Sku { get; set; } = string.Empty;
    public string Barcode { get; set; } = string.Empty; // 13 цифр
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public int CategoryId { get; set; }
    public decimal Price { get; set; }
    public int Stock { get; set; }
    public List<string> Tags { get; set; } = new();
    
    /// <summary>Два цвета для градиента плитки товара: Color1;Color2</summary>
    public string ImageGradient { get; set; } = string.Empty;
    
    /// <summary>Иконка категории (Segoe MDL2 Assets Unicode)</summary>
    public string Icon { get; set; } = string.Empty;

    /// <summary>Дополнительное предложение (кросс-продажа) в кассе.</summary>
    public bool IsCrossSell { get; set; }

    /// <summary>Категория доп. предложения: accessory / service.</summary>
    public string CrossSellCategory { get; set; } = "accessory";

    /// <summary>Есть ли у товара изображение на бэкенде.</summary>
    public bool HasImage { get; set; }

    /// <summary>Версия изображения: меняется при обновлении файла.</summary>
    public string ImageToken { get; set; } = string.Empty;

    public DateTime? ImageUpdatedAt { get; set; }

    public bool InStock => Stock > 0;
}
