using ElectronCash.Client.Models;

namespace ElectronCash.Client.Services;

/// <summary>
/// Статический класс для хранения состояния пользовательской сессии.
/// Используется для отслеживания текущего пользователя, корзины и параметров заказа.
/// </summary>
public static class Session
{
    private static User? _currentUser;
    private static List<OrderItem> _currentCart = new();
    private static int _currentOrderId;
    private static decimal _discount;

    /// <summary>Текущий вошедший в систему пользователь.</summary>
    public static User? CurrentUser => _currentUser;

    /// <summary>Вошёл ли пользователь в систему.</summary>
    public static bool IsLoggedIn => _currentUser != null;

    /// <summary>Текущая корзина заказа.</summary>
    public static List<OrderItem> CurrentCart => _currentCart;

    /// <summary>ID текущего заказа (номер, выдаётся при открытии новой смены).</summary>
    public static int CurrentOrderId
    {
        get => _currentOrderId;
        set => _currentOrderId = value;
    }

    /// <summary>Скидка на текущий заказ (в абсолютных единицах).</summary>
    public static decimal Discount
    {
        get => _discount;
        set => _discount = value;
    }

    /// <summary>Сумма корзины без учёта скидки и НДС.</summary>
    public static decimal CartSubtotal => _currentCart.Sum(item => item.Total);

    /// <summary>НДС 20% от суммы.</summary>
    public static decimal Vat => CartSubtotal * 0.20m;

    /// <summary>Итоговая сумма: Сумма - Скидка + НДС.</summary>
    public static decimal CartTotal => CartSubtotal - Discount + Vat;

    /// <summary>Событие, вызываемое при изменении корзины (добавление, удаление, изменение количества).</summary>
    public static event Action? CartChanged;

    /// <summary>Установить текущего пользователя (вход).</summary>
    public static void Login(User user)
    {
        _currentUser = user;
        ClearCart();
        _currentOrderId = new Random().Next(1000, 9999);
    }

    /// <summary>Выход из системы.</summary>
    public static void Logout()
    {
        _currentUser = null;
        ClearCart();
        _discount = 0;
    }

    /// <summary>Добавить товар в корзину (или увеличить количество, если уже есть).</summary>
    public static void AddToCart(Product product, int quantity = 1)
    {
        var existing = _currentCart.FirstOrDefault(item => item.ProductId == product.Id);
        if (existing != null)
        {
            existing.Quantity += quantity;
        }
        else
        {
            _currentCart.Add(new OrderItem
            {
                ProductId = product.Id,
                ProductName = product.Name,
                UnitPrice = product.Price,
                Quantity = quantity
            });
        }
        CartChanged?.Invoke();
    }

    /// <summary>Удалить товар из корзины по ID товара.</summary>
    public static void RemoveFromCart(int productId)
    {
        var item = _currentCart.FirstOrDefault(i => i.ProductId == productId);
        if (item != null)
        {
            _currentCart.Remove(item);
            CartChanged?.Invoke();
        }
    }

    /// <summary>Изменить количество товара в корзине.</summary>
    public static void UpdateCartItemQuantity(int productId, int newQuantity)
    {
        if (newQuantity <= 0)
        {
            RemoveFromCart(productId);
            return;
        }

        var item = _currentCart.FirstOrDefault(i => i.ProductId == productId);
        if (item != null)
        {
            item.Quantity = newQuantity;
            CartChanged?.Invoke();
        }
    }

    /// <summary>Очистить корзину.</summary>
    public static void ClearCart()
    {
        _currentCart.Clear();
        _discount = 0;
        CartChanged?.Invoke();
    }

    /// <summary>Применить скидку на заказ.</summary>
    public static void ApplyDiscount(decimal amount)
    {
        _discount = Math.Max(0, amount);
        CartChanged?.Invoke();
    }

    /// <summary>Создать заказ из текущей корзины.</summary>
    public static Order CreateOrder()
    {
        if (!IsLoggedIn)
            throw new InvalidOperationException("Пользователь не вошёл в систему");

        return new Order
        {
            Id = _currentOrderId,
            CreatedAt = DateTime.Now,
            CashierId = CurrentUser!.Id,
            Items = new List<OrderItem>(_currentCart),
            Discount = _discount,
            Total = CartTotal,
            Status = OrderStatus.Paid
        };
    }
}
