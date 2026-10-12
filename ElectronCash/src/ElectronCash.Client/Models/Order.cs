namespace ElectronCash.Client.Models;

/// <summary>Статус заказа.</summary>
public enum OrderStatus
{
    Draft,
    Paid,
    Cancelled
}

/// <summary>Заказ (продажа).</summary>
public class Order
{
    public int Id { get; set; }
    public DateTime CreatedAt { get; set; }
    public int CashierId { get; set; }
    public List<OrderItem> Items { get; set; } = new();
    public decimal Discount { get; set; }
    public decimal Total { get; set; }
    public OrderStatus Status { get; set; }
}
