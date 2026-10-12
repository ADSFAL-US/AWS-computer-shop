namespace ElectronCash.Client.Models;

/// <summary>Пользователь системы.</summary>
public class User
{
    public int Id { get; set; }
    public string Login { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public Role Role { get; set; }
    public string? Initials => string.Concat(FullName.Split(' ').Select(w => w.FirstOrDefault()));
}
