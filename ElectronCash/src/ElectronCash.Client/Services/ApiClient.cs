using System.Net;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using ElectronCash.Client.Models;

namespace ElectronCash.Client.Services;

/// <summary>Ошибка обращения к API (с текстом из поля detail).</summary>
public class ApiException : Exception
{
    public HttpStatusCode StatusCode { get; }

    public ApiException(HttpStatusCode statusCode, string message) : base(message)
    {
        StatusCode = statusCode;
    }
}

/// <summary>
/// HTTP-клиент REST API (FastAPI). Адрес задаётся переменной окружения
/// ELECTRONCASH_API_URL, по умолчанию http://localhost:8000.
/// </summary>
public sealed class ApiClient
{
    private static readonly Lazy<ApiClient> _lazy = new(() => new ApiClient());
    public static ApiClient Instance => _lazy.Value;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
        PropertyNameCaseInsensitive = true,
        Converters =
        {
            new JsonStringEnumConverter(),
            new FlexibleDecimalConverter()
        }
    };

    private readonly HttpClient _http;
    private string? _token;

    private ApiClient()
    {
        var baseUrl = Environment.GetEnvironmentVariable("ELECTRONCASH_API_URL");
        if (string.IsNullOrWhiteSpace(baseUrl))
            baseUrl = "http://localhost:8000";

        _http = new HttpClient
        {
            BaseAddress = new Uri(baseUrl.TrimEnd('/') + "/"),
            Timeout = TimeSpan.FromSeconds(20)
        };
    }

    public bool IsAuthenticated => _token != null;

    // ============= АВТОРИЗАЦИЯ =============

    public async Task<User> LoginAsync(string login, string password, CancellationToken ct = default)
    {
        var response = await SendAsync<LoginResponse>(
            HttpMethod.Post, "api/auth/login", new { login, password }, ct);
        _token = response.AccessToken;
        return response.User;
    }

    public void Logout() => _token = null;

    // ============= КАТАЛОГ =============

    public List<Category> GetCategories() => Send<List<Category>>(HttpMethod.Get, "api/categories");

    public List<Product> GetAllProducts() => Send<List<Product>>(HttpMethod.Get, "api/products");

    public List<Product> GetProducts(string? searchText = null, int? categoryId = null,
        List<string>? tags = null, decimal? minPrice = null, decimal? maxPrice = null,
        bool onlyInStock = false)
    {
        var query = new List<string>();
        if (!string.IsNullOrWhiteSpace(searchText))
            query.Add($"search={Uri.EscapeDataString(searchText)}");
        if (categoryId.HasValue)
            query.Add($"category_id={categoryId.Value}");
        if (tags?.Any() == true)
            query.AddRange(tags.Select(t => $"tags={Uri.EscapeDataString(t)}"));
        if (minPrice.HasValue)
            query.Add($"min_price={minPrice.Value.ToString(System.Globalization.CultureInfo.InvariantCulture)}");
        if (maxPrice.HasValue)
            query.Add($"max_price={maxPrice.Value.ToString(System.Globalization.CultureInfo.InvariantCulture)}");
        if (onlyInStock)
            query.Add("only_in_stock=true");

        var path = query.Count > 0 ? $"api/products?{string.Join("&", query)}" : "api/products";
        return Send<List<Product>>(HttpMethod.Get, path);
    }

    public Product? GetProductByBarcode(string barcode) =>
        SendOrDefault<Product>(HttpMethod.Get, $"api/products/barcode/{Uri.EscapeDataString(barcode)}");

    public Product? GetProductById(int id) =>
        SendOrDefault<Product>(HttpMethod.Get, $"api/products/{id}");

    public List<CrossSellOffer> GetCrossSellOffers() =>
        Send<List<CrossSellOffer>>(HttpMethod.Get, "api/cross-sell-offers");

    // ============= ИЗОБРАЖЕНИЯ ТОВАРОВ =============

    /// <summary>
    /// Метаданные изображения: доступно ли, версия и ссылка на скачивание.
    /// </summary>
    public ProductImageMeta? GetProductImageMeta(int productId) =>
        SendOrDefault<ProductImageMeta>(HttpMethod.Get, $"api/products/{productId}/image");

    /// <summary>Скачивает файл изображения по ссылке, которую вернул бэкенд.</summary>
    public byte[]? DownloadProductImage(string downloadUrl)
    {
        using var request = BuildRequest(HttpMethod.Get, AbsolutePath(downloadUrl), null);
        using var response = _http.Send(request);
        if (!response.IsSuccessStatusCode)
            return null;
        return response.Content.ReadAsByteArrayAsync().GetAwaiter().GetResult();
    }

    private string AbsolutePath(string url)
    {
        if (Uri.TryCreate(url, UriKind.Absolute, out var absolute))
            return absolute.AbsolutePath.TrimStart('/');
        return url.TrimStart('/');
    }

    // ============= ЗАКАЗЫ =============

    public List<Order> GetOrders() => Send<List<Order>>(HttpMethod.Get, "api/orders");

    public Order? GetOrderById(int id) => SendOrDefault<Order>(HttpMethod.Get, $"api/orders/{id}");

    public Order CreateOrder(IEnumerable<OrderItem> items, decimal discount = 0, string status = "Paid")
    {
        var payload = new
        {
            items = items.Select(i => new { product_id = i.ProductId, quantity = i.Quantity }).ToList(),
            discount,
            status
        };
        return Send<Order>(HttpMethod.Post, "api/orders", payload);
    }

    public async Task<Order> CreateOrderAsync(IEnumerable<OrderItem> items, decimal discount = 0,
        string status = "Paid", CancellationToken ct = default)
    {
        var payload = new
        {
            items = items.Select(i => new { product_id = i.ProductId, quantity = i.Quantity }).ToList(),
            discount,
            status
        };
        return await SendAsync<Order>(HttpMethod.Post, "api/orders", payload, ct);
    }

    // ============= СТАТИСТИКА =============

    public (decimal Revenue, decimal AvgCheck, int OrdersCount, int ProductsInStock) GetStats()
    {
        var stats = Send<StatsResponse>(HttpMethod.Get, "api/stats");
        return (stats.Revenue, stats.AvgCheck, stats.OrdersCount, stats.ProductsInStock);
    }

    public List<(Product Product, int QuantitySold)> GetTopProducts()
    {
        var rows = Send<List<TopProductResponse>>(HttpMethod.Get, "api/stats/top-products");
        return rows.Select(r => (r.Product, r.QuantitySold)).ToList();
    }

    public List<(DateTime Date, decimal Revenue)> GetSalesLast7Days()
    {
        var rows = Send<List<SalesPointResponse>>(HttpMethod.Get, "api/stats/sales");
        return rows
            .Select(r => (DateTime.Parse(r.Date, System.Globalization.CultureInfo.InvariantCulture), r.Revenue))
            .ToList();
    }

    // ============= АДМИНИСТРИРОВАНИЕ =============

    public List<User> GetUsers() => Send<List<User>>(HttpMethod.Get, "api/admin/users");

    public User AddUser(User user)
    {
        var payload = new
        {
            login = user.Login,
            password = user.Password,
            full_name = user.FullName,
            role = user.Role.ToString()
        };
        return Send<User>(HttpMethod.Post, "api/admin/users", payload);
    }

    public User UpdateUser(User user)
    {
        var payload = new
        {
            login = user.Login,
            password = string.IsNullOrEmpty(user.Password) ? null : user.Password,
            full_name = user.FullName,
            role = user.Role.ToString()
        };
        return Send<User>(HttpMethod.Put, $"api/admin/users/{user.Id}", payload);
    }

    public void DeleteUser(int id) => SendVoid(HttpMethod.Delete, $"api/admin/users/{id}");

    public List<AuditLogEntry> GetAuditLog() =>
        Send<List<AuditLogEntry>>(HttpMethod.Get, "api/admin/audit-log");

    public List<IpEntry> GetIpWhitelist() =>
        Send<List<IpEntry>>(HttpMethod.Get, "api/admin/ip-whitelist");

    public IpEntry AddIpEntry(IpEntry entry) =>
        Send<IpEntry>(HttpMethod.Post, "api/admin/ip-whitelist",
            new { ip_address = entry.IpAddress, comment = entry.Comment });

    public void DeleteIpEntry(int id) =>
        SendVoid(HttpMethod.Delete, $"api/admin/ip-whitelist/{id}");

    // ============= ТОВАРЫ (АДМИН) =============

    public List<Product> GetAdminProducts() =>
        Send<List<Product>>(HttpMethod.Get, "api/admin/products");

    public List<Category> GetAdminCategories() =>
        Send<List<Category>>(HttpMethod.Get, "api/admin/categories");

    public Category AddCategory(string name, string icon = "") =>
        Send<Category>(HttpMethod.Post, "api/admin/categories", new { name, icon });

    public Product AddProduct(Product product) =>
        Send<Product>(HttpMethod.Post, "api/admin/products", BuildProductPayload(product));

    public Product UpdateProduct(Product product) =>
        Send<Product>(HttpMethod.Put, $"api/admin/products/{product.Id}",
            BuildProductPayload(product));

    public void DeleteProduct(int id) =>
        SendVoid(HttpMethod.Delete, $"api/admin/products/{id}");

    /// <summary>
    /// Загружает изображение товара: base64 (data-URI допустим) или http(s)-ссылка.
    /// </summary>
    public ProductImageMeta UploadProductImage(int productId, string data, string filename = "") =>
        Send<ProductImageMeta>(HttpMethod.Post, $"api/admin/products/{productId}/image",
            new { data, filename });

    public void DeleteProductImage(int productId) =>
        SendVoid(HttpMethod.Delete, $"api/admin/products/{productId}/image");

    /// <summary>Импорт товаров: base64 от JSON-массива со ссылками на картинки.</summary>
    public ImportResult ImportProducts(string base64Payload) =>
        Send<ImportResult>(HttpMethod.Post, "api/admin/products/import",
            new { payload = base64Payload });

    private static Dictionary<string, object?> BuildProductPayload(Product p)
    {
        var payload = new Dictionary<string, object?>
        {
            ["name"] = p.Name,
            ["description"] = p.Description,
            ["category_id"] = p.CategoryId,
            ["price"] = p.Price,
            ["stock"] = p.Stock,
            ["tags"] = p.Tags,
            ["image_gradient"] = p.ImageGradient,
            ["icon"] = p.Icon,
            ["is_cross_sell"] = p.IsCrossSell,
            ["cross_sell_category"] = p.CrossSellCategory
        };
        // Пустые артикул/штрихкод не отправляем: бэкенд сгенерирует сам
        if (!string.IsNullOrWhiteSpace(p.Sku))
            payload["sku"] = p.Sku.Trim();
        if (!string.IsNullOrWhiteSpace(p.Barcode))
            payload["barcode"] = p.Barcode.Trim();
        return payload;
    }

    // ============= ТРАНСПОРТ =============

    private HttpRequestMessage BuildRequest(HttpMethod method, string path, object? body)
    {
        var request = new HttpRequestMessage(method, path);
        if (_token != null)
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _token);
        if (body != null)
        {
            request.Content = new StringContent(
                JsonSerializer.Serialize(body, JsonOptions), Encoding.UTF8, "application/json");
        }
        return request;
    }

    private T Send<T>(HttpMethod method, string path, object? body = null)
    {
        using var request = BuildRequest(method, path, body);
        using var response = _http.Send(request);
        return Read<T>(response);
    }

    private T? SendOrDefault<T>(HttpMethod method, string path) where T : class
    {
        using var request = BuildRequest(method, path, null);
        using var response = _http.Send(request);
        if (response.StatusCode == HttpStatusCode.NotFound)
            return null;
        return Read<T>(response);
    }

    private void SendVoid(HttpMethod method, string path, object? body = null)
    {
        using var request = BuildRequest(method, path, body);
        using var response = _http.Send(request);
        if (!response.IsSuccessStatusCode)
            throw BuildError(response);
    }

    private async Task<T> SendAsync<T>(HttpMethod method, string path, object? body,
        CancellationToken ct)
    {
        using var request = BuildRequest(method, path, body);
        using var response = await _http.SendAsync(request, ct);
        var payload = await response.Content.ReadAsStringAsync(ct);
        if (!response.IsSuccessStatusCode)
            throw BuildError(response.StatusCode, payload);
        return JsonSerializer.Deserialize<T>(payload, JsonOptions)
               ?? throw new ApiException(response.StatusCode, "Пустой ответ сервера");
    }

    private static T Read<T>(HttpResponseMessage response)
    {
        var payload = response.Content.ReadAsStringAsync().GetAwaiter().GetResult();
        if (!response.IsSuccessStatusCode)
            throw BuildError(response.StatusCode, payload);
        return JsonSerializer.Deserialize<T>(payload, JsonOptions)
               ?? throw new ApiException(response.StatusCode, "Пустой ответ сервера");
    }

    private static ApiException BuildError(HttpResponseMessage response) =>
        BuildError(response.StatusCode, response.Content.ReadAsStringAsync().GetAwaiter().GetResult());

    private static ApiException BuildError(HttpStatusCode statusCode, string payload)
    {
        var message = $"Ошибка API ({(int)statusCode})";
        try
        {
            using var doc = JsonDocument.Parse(payload);
            if (doc.RootElement.TryGetProperty("detail", out var detail))
            {
                message = detail.ValueKind switch
                {
                    JsonValueKind.String => detail.GetString() ?? message,
                    JsonValueKind.Array => string.Join("; ", detail.EnumerateArray()
                        .Select(e => e.TryGetProperty("msg", out var msg) ? msg.GetString() : e.ToString())),
                    _ => detail.ToString()
                };
            }
        }
        catch (JsonException)
        {
            // тело не JSON — оставляем общий текст
        }
        return new ApiException(statusCode, message);
    }

    private sealed record LoginResponse(string AccessToken, User User);

    private sealed record StatsResponse(decimal Revenue, decimal AvgCheck, int OrdersCount, int ProductsInStock);

    private sealed record TopProductResponse(Product Product, int QuantitySold);

    private sealed record SalesPointResponse(string Date, decimal Revenue);

    /// <summary>Decimal приходит из API строкой (точность), но принимаем и число.</summary>
    private sealed class FlexibleDecimalConverter : JsonConverter<decimal>
    {
        public override decimal Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) =>
            reader.TokenType switch
            {
                JsonTokenType.String => decimal.Parse(reader.GetString()!,
                    System.Globalization.CultureInfo.InvariantCulture),
                JsonTokenType.Number => reader.GetDecimal(),
                _ => throw new JsonException($"Неожиданный тип для decimal: {reader.TokenType}")
            };

        public override void Write(Utf8JsonWriter writer, decimal value, JsonSerializerOptions options) =>
            writer.WriteNumberValue(value);
    }
}
