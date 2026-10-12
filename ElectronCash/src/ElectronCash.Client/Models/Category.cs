namespace ElectronCash.Client.Models;

/// <summary>Категория товара.</summary>
public class Category
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Icon { get; set; } = string.Empty; // Segoe MDL2 Assets Unicode
}
