namespace ElectronCash.Client.Models;

/// <summary>Запись в журнале аудита действий администратора.</summary>
public class AuditLogEntry
{
    public int Id { get; set; }
    public DateTime Timestamp { get; set; }
    public int UserId { get; set; }
    public string UserName { get; set; } = string.Empty;
    public string Action { get; set; } = string.Empty;
    public string Details { get; set; } = string.Empty;
}
