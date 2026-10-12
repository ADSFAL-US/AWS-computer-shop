namespace ElectronCash.Client.Models;

/// <summary>Запись IP-адреса в whitelist.</summary>
public class IpEntry
{
    public int Id { get; set; }
    public string IpAddress { get; set; } = string.Empty;
    public string Comment { get; set; } = string.Empty;
    public DateTime AddedAt { get; set; }
}
