namespace ElectronCash.Client.Models;

/// <summary>Результат импорта товаров из base64-списка.</summary>
public class ImportResult
{
    public int Total { get; set; }
    public int Created { get; set; }
    public int Updated { get; set; }
    public int Failed { get; set; }
    public List<ImportResultItem> Items { get; set; } = new();
}

/// <summary>Строка отчёта импорта по одному товару.</summary>
public class ImportResultItem
{
    public int Index { get; set; }
    public string Sku { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;

    /// <summary>created / updated / error</summary>
    public string Status { get; set; } = string.Empty;

    public string Message { get; set; } = string.Empty;
    public int? ProductId { get; set; }
    public bool ImageSaved { get; set; }
}
