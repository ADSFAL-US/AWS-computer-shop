using ElectronCash.Client.Models;

namespace ElectronCash.Client.Services;

/// <summary>
/// Сервис с захардкоженными данными для мока.
/// Никаких сетевых запросов, всё локально.
/// </summary>
public class MockDataService
{
    private static MockDataService? _instance;
    public static MockDataService Instance => _instance ??= new MockDataService();

    private readonly List<User> _users;
    private readonly List<Category> _categories;
    private readonly List<Product> _products;
    private readonly List<Order> _orders;
    private readonly List<CrossSellOffer> _crossSellOffers;
    private readonly List<AuditLogEntry> _auditLog;
    private readonly List<IpEntry> _ipWhitelist;

    private MockDataService()
    {
        _users = InitializeUsers();
        _categories = InitializeCategories();
        _products = InitializeProducts();
        _orders = InitializeOrders();
        _crossSellOffers = InitializeCrossSellOffers();
        _auditLog = InitializeAuditLog();
        _ipWhitelist = InitializeIpWhitelist();
    }

    // ============= ИНИЦИАЛИЗАЦИЯ =============

    private static List<User> InitializeUsers()
    {
        return new List<User>
        {
            new User { Id = 1, Login = "admin", Password = "admin", FullName = "Администратор", Role = Role.Admin },
            new User { Id = 2, Login = "manager", Password = "manager", FullName = "Иванов И.И.", Role = Role.Manager },
            new User { Id = 3, Login = "cashier", Password = "cashier", FullName = "Петрова А.С.", Role = Role.Cashier },
        };
    }

    private static List<Category> InitializeCategories()
    {
        return new List<Category>
        {
            new Category { Id = 1, Name = "Смартфоны", Icon = "\uE8EA" },
            new Category { Id = 2, Name = "Ноутбуки", Icon = "\uE7F7" },
            new Category { Id = 3, Name = "Планшеты", Icon = "\uE70A" },
            new Category { Id = 4, Name = "Наушники", Icon = "\uE720" },
            new Category { Id = 5, Name = "Часы", Icon = "\uE81C" },
            new Category { Id = 6, Name = "Аксессуары", Icon = "\uE71D" },
            new Category { Id = 7, Name = "Периферия", Icon = "\uE765" },
            new Category { Id = 8, Name = "Фото/Видео", Icon = "\uE722" },
            new Category { Id = 9, Name = "Хранение данных", Icon = "\uE7B3" },
            new Category { Id = 10, Name = "Умный дом", Icon = "\uE90F" },
        };
    }

    private static List<Product> InitializeProducts()
    {
        return new List<Product>
        {
            // Смартфоны
            new Product 
            { 
                Id = 1, Sku = "SK001", Barcode = "1234567890001", 
                Name = "iPhone 15 Pro 256GB", CategoryId = 1, 
                Price = 149999m, Stock = 12, 
                Tags = new List<string> { "apple", "premium", "new" },
                ImageGradient = "#00D9A3;#60A5FA",
                Icon = "\uE8EA"
            },
            new Product 
            { 
                Id = 2, Sku = "SK002", Barcode = "1234567890002", 
                Name = "iPhone 15", CategoryId = 1, 
                Price = 89999m, Stock = 25, 
                Tags = new List<string> { "apple", "premium" },
                ImageGradient = "#60A5FA;#34D399",
                Icon = "\uE8EA"
            },
            new Product 
            { 
                Id = 3, Sku = "SK003", Barcode = "1234567890003", 
                Name = "Samsung Galaxy S24 Ultra", CategoryId = 1, 
                Price = 139999m, Stock = 8, 
                Tags = new List<string> { "samsung", "premium", "new" },
                ImageGradient = "#FBBF24;#F87171",
                Icon = "\uE8EA"
            },
            new Product 
            { 
                Id = 4, Sku = "SK004", Barcode = "1234567890004", 
                Name = "Samsung Galaxy A55", CategoryId = 1, 
                Price = 34999m, Stock = 40, 
                Tags = new List<string> { "samsung", "budget" },
                ImageGradient = "#34D399;#60A5FA",
                Icon = "\uE8EA"
            },
            new Product 
            { 
                Id = 5, Sku = "SK005", Barcode = "1234567890005", 
                Name = "Google Pixel 8", CategoryId = 1, 
                Price = 79999m, Stock = 15, 
                Tags = new List<string> { "google", "premium" },
                ImageGradient = "#00D9A3;#F87171",
                Icon = "\uE8EA"
            },
            new Product 
            { 
                Id = 6, Sku = "SK006", Barcode = "1234567890006", 
                Name = "Xiaomi 14", CategoryId = 1, 
                Price = 69999m, Stock = 22, 
                Tags = new List<string> { "xiaomi", "premium" },
                ImageGradient = "#FBBF24;#34D399",
                Icon = "\uE8EA"
            },

            // Ноутбуки
            new Product 
            { 
                Id = 7, Sku = "SK007", Barcode = "1234567890007", 
                Name = "MacBook Air M3 13\"", CategoryId = 2, 
                Price = 129999m, Stock = 6, 
                Tags = new List<string> { "apple", "premium" },
                ImageGradient = "#60A5FA;#00D9A3",
                Icon = "\uE7F7"
            },
            new Product 
            { 
                Id = 8, Sku = "SK008", Barcode = "1234567890008", 
                Name = "MacBook Pro 14\" M3 Pro", CategoryId = 2, 
                Price = 249999m, Stock = 4, 
                Tags = new List<string> { "apple", "premium" },
                ImageGradient = "#34D399;#FBBF24",
                Icon = "\uE7F7"
            },
            new Product 
            { 
                Id = 9, Sku = "SK009", Barcode = "1234567890009", 
                Name = "ASUS ROG Zephyrus G14", CategoryId = 2, 
                Price = 189999m, Stock = 5, 
                Tags = new List<string> { "gaming", "premium" },
                ImageGradient = "#F87171;#FBBF24",
                Icon = "\uE7F7"
            },
            new Product 
            { 
                Id = 10, Sku = "SK010", Barcode = "1234567890010", 
                Name = "Lenovo IdeaPad 5", CategoryId = 2, 
                Price = 59999m, Stock = 18, 
                Tags = new List<string> { "budget" },
                ImageGradient = "#60A5FA;#34D399",
                Icon = "\uE7F7"
            },

            // Планшеты
            new Product 
            { 
                Id = 11, Sku = "SK011", Barcode = "1234567890011", 
                Name = "iPad Pro 11\" M2", CategoryId = 3, 
                Price = 99999m, Stock = 9, 
                Tags = new List<string> { "apple", "premium" },
                ImageGradient = "#00D9A3;#60A5FA",
                Icon = "\uE70A"
            },
            new Product 
            { 
                Id = 12, Sku = "SK012", Barcode = "1234567890012", 
                Name = "iPad Air 5", CategoryId = 3, 
                Price = 69999m, Stock = 12, 
                Tags = new List<string> { "apple", "premium" },
                ImageGradient = "#FBBF24;#34D399",
                Icon = "\uE70A"
            },
            new Product 
            { 
                Id = 13, Sku = "SK013", Barcode = "1234567890013", 
                Name = "Samsung Galaxy Tab S9", CategoryId = 3, 
                Price = 54999m, Stock = 14, 
                Tags = new List<string> { "samsung", "premium" },
                ImageGradient = "#60A5FA;#FBBF24",
                Icon = "\uE70A"
            },

            // Наушники
            new Product 
            { 
                Id = 14, Sku = "SK014", Barcode = "1234567890014", 
                Name = "AirPods Pro 2", CategoryId = 4, 
                Price = 29999m, Stock = 30, 
                Tags = new List<string> { "apple", "audio", "premium" },
                ImageGradient = "#F0F4F8;#60A5FA",
                Icon = "\uE720"
            },
            new Product 
            { 
                Id = 15, Sku = "SK015", Barcode = "1234567890015", 
                Name = "Sony WH-1000XM5", CategoryId = 4, 
                Price = 42999m, Stock = 16, 
                Tags = new List<string> { "audio", "premium" },
                ImageGradient = "#34D399;#FBBF24",
                Icon = "\uE720"
            },
            new Product 
            { 
                Id = 16, Sku = "SK016", Barcode = "1234567890016", 
                Name = "Sony WF-1000XM5", CategoryId = 4, 
                Price = 38999m, Stock = 20, 
                Tags = new List<string> { "audio", "premium" },
                ImageGradient = "#FBBF24;#00D9A3",
                Icon = "\uE720"
            },
            new Product 
            { 
                Id = 17, Sku = "SK017", Barcode = "1234567890017", 
                Name = "JBL Tune 770NC", CategoryId = 4, 
                Price = 14999m, Stock = 35, 
                Tags = new List<string> { "audio", "budget" },
                ImageGradient = "#60A5FA;#F87171",
                Icon = "\uE720"
            },

            // Часы
            new Product 
            { 
                Id = 18, Sku = "SK018", Barcode = "1234567890018", 
                Name = "Apple Watch Series 9", CategoryId = 5, 
                Price = 34999m, Stock = 18, 
                Tags = new List<string> { "apple", "wearable", "premium" },
                ImageGradient = "#00D9A3;#60A5FA",
                Icon = "\uE81C"
            },
            new Product 
            { 
                Id = 19, Sku = "SK019", Barcode = "1234567890019", 
                Name = "Samsung Galaxy Watch 6", CategoryId = 5, 
                Price = 24999m, Stock = 22, 
                Tags = new List<string> { "samsung", "wearable" },
                ImageGradient = "#FBBF24;#34D399",
                Icon = "\uE81C"
            },
            new Product 
            { 
                Id = 20, Sku = "SK020", Barcode = "1234567890020", 
                Name = "Xiaomi Smart Band 8", CategoryId = 5, 
                Price = 4999m, Stock = 45, 
                Tags = new List<string> { "wearable", "budget" },
                ImageGradient = "#60A5FA;#F87171",
                Icon = "\uE81C"
            },

            // Аксессуары
            new Product 
            { 
                Id = 21, Sku = "SK021", Barcode = "1234567890021", 
                Name = "Logitech MX Master 3S", CategoryId = 6, 
                Price = 14999m, Stock = 28, 
                Tags = new List<string> { "accessory", "premium" },
                ImageGradient = "#34D399;#00D9A3",
                Icon = "\uE71D"
            },
            new Product 
            { 
                Id = 22, Sku = "SK022", Barcode = "1234567890022", 
                Name = "Logitech MX Keys S", CategoryId = 6, 
                Price = 12999m, Stock = 24, 
                Tags = new List<string> { "accessory", "premium" },
                ImageGradient = "#FBBF24;#60A5FA",
                Icon = "\uE71D"
            },
            new Product 
            { 
                Id = 23, Sku = "SK023", Barcode = "1234567890023", 
                Name = "Apple Magic Keyboard", CategoryId = 6, 
                Price = 9999m, Stock = 16, 
                Tags = new List<string> { "apple", "accessory" },
                ImageGradient = "#F0F4F8;#00D9A3",
                Icon = "\uE71D"
            },
            new Product 
            { 
                Id = 24, Sku = "SK024", Barcode = "1234567890024", 
                Name = "Anker PowerCore 20000", CategoryId = 6, 
                Price = 3999m, Stock = 50, 
                Tags = new List<string> { "accessory", "budget" },
                ImageGradient = "#F87171;#FBBF24",
                Icon = "\uE71D"
            },

            // Периферия
            new Product 
            { 
                Id = 25, Sku = "SK025", Barcode = "1234567890025", 
                Name = "HyperX Cloud III", CategoryId = 7, 
                Price = 19999m, Stock = 11, 
                Tags = new List<string> { "gaming", "audio" },
                ImageGradient = "#60A5FA;#34D399",
                Icon = "\uE765"
            },
            new Product 
            { 
                Id = 26, Sku = "SK026", Barcode = "1234567890026", 
                Name = "Razer BlackShark V2", CategoryId = 7, 
                Price = 17999m, Stock = 13, 
                Tags = new List<string> { "gaming", "audio" },
                ImageGradient = "#F87171;#00D9A3",
                Icon = "\uE765"
            },
            new Product 
            { 
                Id = 27, Sku = "SK027", Barcode = "1234567890027", 
                Name = "Logitech G Pro X", CategoryId = 7, 
                Price = 21999m, Stock = 9, 
                Tags = new List<string> { "gaming", "audio" },
                ImageGradient = "#FBBF24;#60A5FA",
                Icon = "\uE765"
            },
            new Product 
            { 
                Id = 28, Sku = "SK028", Barcode = "1234567890028", 
                Name = "Keychron K3", CategoryId = 7, 
                Price = 9999m, Stock = 26, 
                Tags = new List<string> { "accessory" },
                ImageGradient = "#34D399;#FBBF24",
                Icon = "\uE765"
            },

            // Фото/Видео
            new Product 
            { 
                Id = 29, Sku = "SK029", Barcode = "1234567890029", 
                Name = "Canon EOS R50", CategoryId = 8, 
                Price = 89999m, Stock = 5, 
                Tags = new List<string> { "premium" },
                ImageGradient = "#60A5FA;#F87171",
                Icon = "\uE722"
            },
            new Product 
            { 
                Id = 30, Sku = "SK030", Barcode = "1234567890030", 
                Name = "Sony Alpha A6400", CategoryId = 8, 
                Price = 79999m, Stock = 7, 
                Tags = new List<string> { "premium" },
                ImageGradient = "#F87171;#34D399",
                Icon = "\uE722"
            },
            new Product 
            { 
                Id = 31, Sku = "SK031", Barcode = "1234567890031", 
                Name = "GoPro Hero 12", CategoryId = 8, 
                Price = 34999m, Stock = 19, 
                Tags = new List<string> { "premium" },
                ImageGradient = "#00D9A3;#FBBF24",
                Icon = "\uE722"
            },

            // Хранение данных
            new Product 
            { 
                Id = 32, Sku = "SK032", Barcode = "1234567890032", 
                Name = "Samsung T7 SSD 1TB", CategoryId = 9, 
                Price = 12999m, Stock = 31, 
                Tags = new List<string> { "storage", "premium" },
                ImageGradient = "#FBBF24;#00D9A3",
                Icon = "\uE7B3"
            },
            new Product 
            { 
                Id = 33, Sku = "SK033", Barcode = "1234567890033", 
                Name = "Kingston XS2000 2TB", CategoryId = 9, 
                Price = 24999m, Stock = 8, 
                Tags = new List<string> { "storage", "premium" },
                ImageGradient = "#34D399;#60A5FA",
                Icon = "\uE7B3"
            },
            new Product 
            { 
                Id = 34, Sku = "SK034", Barcode = "1234567890034", 
                Name = "Seagate Expansion 4TB", CategoryId = 9, 
                Price = 8999m, Stock = 23, 
                Tags = new List<string> { "storage", "budget" },
                ImageGradient = "#60A5FA;#FBBF24",
                Icon = "\uE7B3"
            },

            // Дополнительные товары
            new Product 
            { 
                Id = 35, Sku = "SK035", Barcode = "1234567890035", 
                Name = "Baseus 65W GaN Charger", CategoryId = 6, 
                Price = 4999m, Stock = 42, 
                Tags = new List<string> { "accessory", "budget" },
                ImageGradient = "#F87171;#60A5FA",
                Icon = "\uE71D"
            },
            new Product 
            { 
                Id = 36, Sku = "SK036", Barcode = "1234567890036", 
                Name = "Ugreen USB-C Hub 7-in-1", CategoryId = 6, 
                Price = 6999m, Stock = 38, 
                Tags = new List<string> { "accessory" },
                ImageGradient = "#00D9A3;#34D399",
                Icon = "\uE71D"
            },
            new Product 
            { 
                Id = 37, Sku = "SK037", Barcode = "1234567890037", 
                Name = "Razer Viper Wireless", CategoryId = 7, 
                Price = 11999m, Stock = 19, 
                Tags = new List<string> { "gaming" },
                ImageGradient = "#FBBF24;#F87171",
                Icon = "\uE765"
            },
            new Product 
            { 
                Id = 38, Sku = "SK038", Barcode = "1234567890038", 
                Name = "Webcam Logitech C920", CategoryId = 7, 
                Price = 5999m, Stock = 25, 
                Tags = new List<string> { "accessory" },
                ImageGradient = "#60A5FA;#00D9A3",
                Icon = "\uE765"
            },
            new Product 
            { 
                Id = 39, Sku = "SK039", Barcode = "1234567890039", 
                Name = "iPhone Screen Protector", CategoryId = 6, 
                Price = 999m, Stock = 100, 
                Tags = new List<string> { "apple", "accessory" },
                ImageGradient = "#34D399;#F87171",
                Icon = "\uE71D"
            },
            new Product 
            { 
                Id = 40, Sku = "SK040", Barcode = "1234567890040", 
                Name = "Phone Case Premium", CategoryId = 6, 
                Price = 1999m, Stock = 85, 
                Tags = new List<string> { "accessory" },
                ImageGradient = "#FBBF24;#60A5FA",
                Icon = "\uE71D"
            },
        };
    }

    private static List<Order> InitializeOrders()
    {
        var orders = new List<Order>();
        var now = DateTime.Now;

        // 15 заказов за последние 7 дней
        orders.Add(new Order 
        { 
            Id = 1001, CreatedAt = now.AddDays(-6).AddHours(10), CashierId = 3,
            Items = new List<OrderItem> 
            { 
                new OrderItem { ProductId = 1, ProductName = "iPhone 15 Pro 256GB", UnitPrice = 149999, Quantity = 1 }
            },
            Discount = 0, Total = 149999, Status = OrderStatus.Paid
        });

        orders.Add(new Order 
        { 
            Id = 1002, CreatedAt = now.AddDays(-6).AddHours(11), CashierId = 3,
            Items = new List<OrderItem> 
            { 
                new OrderItem { ProductId = 2, ProductName = "iPhone 15", UnitPrice = 89999, Quantity = 1 },
                new OrderItem { ProductId = 14, ProductName = "AirPods Pro 2", UnitPrice = 29999, Quantity = 1 }
            },
            Discount = 5000, Total = 114998, Status = OrderStatus.Paid
        });

        orders.Add(new Order 
        { 
            Id = 1003, CreatedAt = now.AddDays(-5).AddHours(9), CashierId = 3,
            Items = new List<OrderItem> 
            { 
                new OrderItem { ProductId = 7, ProductName = "MacBook Air M3 13\"", UnitPrice = 129999, Quantity = 1 }
            },
            Discount = 0, Total = 129999, Status = OrderStatus.Paid
        });

        orders.Add(new Order 
        { 
            Id = 1004, CreatedAt = now.AddDays(-5).AddHours(14), CashierId = 3,
            Items = new List<OrderItem> 
            { 
                new OrderItem { ProductId = 15, ProductName = "Sony WH-1000XM5", UnitPrice = 42999, Quantity = 1 },
                new OrderItem { ProductId = 21, ProductName = "Logitech MX Master 3S", UnitPrice = 14999, Quantity = 1 }
            },
            Discount = 3000, Total = 54998, Status = OrderStatus.Paid
        });

        orders.Add(new Order 
        { 
            Id = 1005, CreatedAt = now.AddDays(-4).AddHours(10), CashierId = 3,
            Items = new List<OrderItem> 
            { 
                new OrderItem { ProductId = 11, ProductName = "iPad Pro 11\" M2", UnitPrice = 99999, Quantity = 1 }
            },
            Discount = 10000, Total = 89999, Status = OrderStatus.Paid
        });

        orders.Add(new Order 
        { 
            Id = 1006, CreatedAt = now.AddDays(-4).AddHours(13), CashierId = 2,
            Items = new List<OrderItem> 
            { 
                new OrderItem { ProductId = 3, ProductName = "Samsung Galaxy S24 Ultra", UnitPrice = 139999, Quantity = 1 },
                new OrderItem { ProductId = 16, ProductName = "Sony WF-1000XM5", UnitPrice = 38999, Quantity = 1 }
            },
            Discount = 0, Total = 178998, Status = OrderStatus.Paid
        });

        orders.Add(new Order 
        { 
            Id = 1007, CreatedAt = now.AddDays(-3).AddHours(11), CashierId = 3,
            Items = new List<OrderItem> 
            { 
                new OrderItem { ProductId = 4, ProductName = "Samsung Galaxy A55", UnitPrice = 34999, Quantity = 2 }
            },
            Discount = 0, Total = 69998, Status = OrderStatus.Paid
        });

        orders.Add(new Order 
        { 
            Id = 1008, CreatedAt = now.AddDays(-3).AddHours(15), CashierId = 3,
            Items = new List<OrderItem> 
            { 
                new OrderItem { ProductId = 29, ProductName = "Canon EOS R50", UnitPrice = 89999, Quantity = 1 }
            },
            Discount = 5000, Total = 84999, Status = OrderStatus.Paid
        });

        orders.Add(new Order 
        { 
            Id = 1009, CreatedAt = now.AddDays(-2).AddHours(10), CashierId = 2,
            Items = new List<OrderItem> 
            { 
                new OrderItem { ProductId = 5, ProductName = "Google Pixel 8", UnitPrice = 79999, Quantity = 1 },
                new OrderItem { ProductId = 24, ProductName = "Anker PowerCore 20000", UnitPrice = 3999, Quantity = 1 }
            },
            Discount = 2000, Total = 81998, Status = OrderStatus.Paid
        });

        orders.Add(new Order 
        { 
            Id = 1010, CreatedAt = now.AddDays(-2).AddHours(12), CashierId = 3,
            Items = new List<OrderItem> 
            { 
                new OrderItem { ProductId = 18, ProductName = "Apple Watch Series 9", UnitPrice = 34999, Quantity = 1 }
            },
            Discount = 0, Total = 34999, Status = OrderStatus.Paid
        });

        orders.Add(new Order 
        { 
            Id = 1011, CreatedAt = now.AddDays(-1).AddHours(9), CashierId = 3,
            Items = new List<OrderItem> 
            { 
                new OrderItem { ProductId = 6, ProductName = "Xiaomi 14", UnitPrice = 69999, Quantity = 1 },
                new OrderItem { ProductId = 35, ProductName = "Baseus 65W GaN Charger", UnitPrice = 4999, Quantity = 1 }
            },
            Discount = 0, Total = 74998, Status = OrderStatus.Paid
        });

        orders.Add(new Order 
        { 
            Id = 1012, CreatedAt = now.AddDays(-1).AddHours(14), CashierId = 2,
            Items = new List<OrderItem> 
            { 
                new OrderItem { ProductId = 17, ProductName = "JBL Tune 770NC", UnitPrice = 14999, Quantity = 1 }
            },
            Discount = 1000, Total = 13999, Status = OrderStatus.Paid
        });

        orders.Add(new Order 
        { 
            Id = 1013, CreatedAt = now.AddHours(-8), CashierId = 3,
            Items = new List<OrderItem> 
            { 
                new OrderItem { ProductId = 32, ProductName = "Samsung T7 SSD 1TB", UnitPrice = 12999, Quantity = 1 }
            },
            Discount = 0, Total = 12999, Status = OrderStatus.Paid
        });

        orders.Add(new Order 
        { 
            Id = 1014, CreatedAt = now.AddHours(-4), CashierId = 3,
            Items = new List<OrderItem> 
            { 
                new OrderItem { ProductId = 9, ProductName = "ASUS ROG Zephyrus G14", UnitPrice = 189999, Quantity = 1 }
            },
            Discount = 15000, Total = 174999, Status = OrderStatus.Paid
        });

        orders.Add(new Order 
        { 
            Id = 1015, CreatedAt = now.AddHours(-1), CashierId = 3,
            Items = new List<OrderItem> 
            { 
                new OrderItem { ProductId = 12, ProductName = "iPad Air 5", UnitPrice = 69999, Quantity = 1 },
                new OrderItem { ProductId = 22, ProductName = "Logitech MX Keys S", UnitPrice = 12999, Quantity = 1 }
            },
            Discount = 5000, Total = 77998, Status = OrderStatus.Draft
        });

        return orders;
    }

    private static List<CrossSellOffer> InitializeCrossSellOffers()
    {
        return new List<CrossSellOffer>
        {
            new CrossSellOffer 
            { 
                Id = 1, Name = "Защитное стекло", Description = "Закалённое стекло 9H", 
                Price = 999m, Icon = "\uE71E", Category = "accessory"
            },
            new CrossSellOffer 
            { 
                Id = 2, Name = "Чехол премиум", Description = "Кожаный чехол с МагСейфом", 
                Price = 2999m, Icon = "\uE71D", Category = "accessory"
            },
            new CrossSellOffer 
            { 
                Id = 3, Name = "Зарядное устройство", Description = "Быстрая зарядка 65W", 
                Price = 3999m, Icon = "\uE70F", Category = "accessory"
            },
            new CrossSellOffer 
            { 
                Id = 4, Name = "Расширенная гарантия", Description = "+2 года полного покрытия", 
                Price = 9999m, Icon = "\uE7BA", Category = "service"
            },
            new CrossSellOffer 
            { 
                Id = 5, Name = "Установка и настройка", Description = "Профессиональная установка и оптимизация", 
                Price = 1999m, Icon = "\uE72C", Category = "service"
            },
            new CrossSellOffer 
            { 
                Id = 6, Name = "Страховка экрана", Description = "Защита от повреждений дисплея", 
                Price = 4999m, Icon = "\uE71E", Category = "service"
            },
        };
    }

    private static List<AuditLogEntry> InitializeAuditLog()
    {
        var now = DateTime.Now;
        return new List<AuditLogEntry>
        {
            new AuditLogEntry 
            { 
                Id = 1, Timestamp = now.AddHours(-3), UserId = 1, UserName = "Администратор",
                Action = "ROLE_CHANGE", Details = "Изменена роль пользователя manager с Manager на Cashier"
            },
            new AuditLogEntry 
            { 
                Id = 2, Timestamp = now.AddHours(-5), UserId = 1, UserName = "Администратор",
                Action = "IP_WHITELIST_ADD", Details = "Добавлен IP-адрес 192.168.1.100"
            },
            new AuditLogEntry 
            { 
                Id = 3, Timestamp = now.AddHours(-7), UserId = 1, UserName = "Администратор",
                Action = "USER_BLOCK", Details = "Заблокирован пользователь cashier"
            },
            new AuditLogEntry 
            { 
                Id = 4, Timestamp = now.AddHours(-24), UserId = 2, UserName = "Иванов И.И.",
                Action = "LARGE_DISCOUNT", Details = "Применена скидка 50% на заказ #1005"
            },
        };
    }

    private static List<IpEntry> InitializeIpWhitelist()
    {
        return new List<IpEntry>
        {
            new IpEntry { Id = 1, IpAddress = "192.168.1.100", Comment = "Касса №1", AddedAt = DateTime.Now.AddDays(-30) },
            new IpEntry { Id = 2, IpAddress = "192.168.1.101", Comment = "Касса №2", AddedAt = DateTime.Now.AddDays(-25) },
            new IpEntry { Id = 3, IpAddress = "192.168.1.102", Comment = "Касса №3", AddedAt = DateTime.Now.AddDays(-20) },
            new IpEntry { Id = 4, IpAddress = "127.0.0.1", Comment = "Локальный тест", AddedAt = DateTime.Now.AddDays(-15) },
        };
    }

    // ============= ПУБЛИЧНЫЕ МЕТОДЫ =============

    /// <summary>Получить пользователя по логину и паролю. Возвращает null если не найден.</summary>
    public User? GetUserByCredentials(string login, string password)
    {
        return _users.FirstOrDefault(u => u.Login == login && u.Password == password);
    }

    /// <summary>Получить все категории товаров.</summary>
    public List<Category> GetCategories() => new List<Category>(_categories);

    /// <summary>Получить все товары с фильтрацией (поиск по названию, категория, теги, диапазон цен).</summary>
    public List<Product> GetProducts(string? searchText = null, int? categoryId = null, List<string>? tags = null, 
        decimal? minPrice = null, decimal? maxPrice = null, bool onlyInStock = false)
    {
        var result = _products.AsEnumerable();

        if (!string.IsNullOrWhiteSpace(searchText))
            result = result.Where(p => p.Name.Contains(searchText, StringComparison.OrdinalIgnoreCase));

        if (categoryId.HasValue)
            result = result.Where(p => p.CategoryId == categoryId);

        if (tags?.Any() == true)
            result = result.Where(p => p.Tags.Any(t => tags.Contains(t)));

        if (minPrice.HasValue)
            result = result.Where(p => p.Price >= minPrice);

        if (maxPrice.HasValue)
            result = result.Where(p => p.Price <= maxPrice);

        if (onlyInStock)
            result = result.Where(p => p.Stock > 0);

        return result.ToList();
    }

    /// <summary>Получить товар по штрихкоду (сканер штрихкода).</summary>
    public Product? GetProductByBarcode(string barcode)
    {
        return _products.FirstOrDefault(p => p.Barcode == barcode);
    }

    /// <summary>Получить товар по ID.</summary>
    public Product? GetProductById(int id)
    {
        return _products.FirstOrDefault(p => p.Id == id);
    }

    /// <summary>Получить все заказы.</summary>
    public List<Order> GetOrders() => new List<Order>(_orders);

    /// <summary>Получить заказ по ID.</summary>
    public Order? GetOrderById(int id)
    {
        return _orders.FirstOrDefault(o => o.Id == id);
    }

    /// <summary>Получить статистику: (Выручка за смену, Средний чек, Количество заказов, Количество товаров).</summary>
    public (decimal Revenue, decimal AvgCheck, int OrdersCount, int ProductsInStock) GetStats()
    {
        var todaysOrders = _orders.Where(o => o.CreatedAt.Date == DateTime.Now.Date && o.Status == OrderStatus.Paid).ToList();
        var revenue = todaysOrders.Sum(o => o.Total);
        var avgCheck = todaysOrders.Any() ? revenue / todaysOrders.Count : 0;
        var ordersCount = todaysOrders.Count;
        var productsInStock = _products.Sum(p => p.Stock);

        return (revenue, avgCheck, ordersCount, productsInStock);
    }

    /// <summary>Получить топ-5 товаров по продажам.</summary>
    public List<(Product Product, int QuantitySold)> GetTopProducts()
    {
        var topProductIds = _orders
            .Where(o => o.Status == OrderStatus.Paid)
            .SelectMany(o => o.Items.Select(i => (i.ProductId, i.Quantity)))
            .GroupBy(x => x.ProductId)
            .OrderByDescending(g => g.Sum(x => x.Quantity))
            .Take(5)
            .ToList();

        var result = new List<(Product, int)>();
        foreach (var group in topProductIds)
        {
            var product = _products.FirstOrDefault(p => p.Id == group.Key);
            if (product != null)
            {
                result.Add((product, group.Sum(x => x.Quantity)));
            }
        }

        return result;
    }

    /// <summary>Получить данные продаж за последние 7 дней: (дата, сумма).</summary>
    public List<(DateTime Date, decimal Revenue)> GetSalesLast7Days()
    {
        var result = new List<(DateTime, decimal)>();
        for (int i = 6; i >= 0; i--)
        {
            var date = DateTime.Now.AddDays(-i).Date;
            var dayRevenue = _orders
                .Where(o => o.CreatedAt.Date == date && o.Status == OrderStatus.Paid)
                .Sum(o => o.Total);
            result.Add((date, dayRevenue));
        }
        return result;
    }

    /// <summary>Получить предложения кросс-продажи.</summary>
    public List<CrossSellOffer> GetCrossSellOffers() => new List<CrossSellOffer>(_crossSellOffers);

    /// <summary>Получить журнал аудита.</summary>
    public List<AuditLogEntry> GetAuditLog() => new List<AuditLogEntry>(_auditLog);

    /// <summary>Получить IP-whitelist.</summary>
    public List<IpEntry> GetIpWhitelist() => new List<IpEntry>(_ipWhitelist);

    /// <summary>Получить все пользователи (для администратора).</summary>
    public List<User> GetAllUsers() => new List<User>(_users);

    /// <summary>Добавить заказ (сохранение при оплате).</summary>
    public void SaveOrder(Order order)
    {
        if (order.Id == 0)
        {
            order.Id = (_orders.Max(o => o.Id) ?? 0) + 1;
        }
        
        var existing = _orders.FirstOrDefault(o => o.Id == order.Id);
        if (existing != null)
            _orders.Remove(existing);
        
        _orders.Add(order);
    }
}
