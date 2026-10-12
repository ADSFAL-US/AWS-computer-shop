namespace ElectronCash.Client.Models;

/// <summary>Кросс-продажа: предложение дополнительного товара/услуги.</summary>
public class CrossSellOffer
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public string Icon { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty; // "accessory" или "service"
}
