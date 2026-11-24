namespace ElectronCash.Client.Models;

/// <summary>Метаданные изображения товара, полученные с бэкенда.</summary>
public class ProductImageMeta
{
    public int ProductId { get; set; }
    public bool Available { get; set; }
    public string Token { get; set; } = string.Empty;
    public string DownloadUrl { get; set; } = string.Empty;
    public string ContentType { get; set; } = string.Empty;
}
