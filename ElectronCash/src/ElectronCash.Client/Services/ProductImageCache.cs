using ElectronCash.Client.Models;

namespace ElectronCash.Client.Services;

/// <summary>
/// Кеш изображений товаров на диске.
///
/// Схема: клиент спрашивает у бэкенда метаданные (доступно ли изображение и
/// какая версия). Если файл этой версии уже лежит в кеше — берём с диска.
/// Иначе скачиваем по ссылке, которую вернул бэкенд. Записи с истёкшим сроком
/// удаляются, а когда товар пропадает из UI — удаляем и его картинку.
/// </summary>
public sealed class ProductImageCache
{
    private static readonly Lazy<ProductImageCache> _lazy = new(() => new ProductImageCache());
    public static ProductImageCache Instance => _lazy.Value;

    /// <summary>Сколько живёт файл в кеше без обращения.</summary>
    private static readonly TimeSpan MaxAge = TimeSpan.FromDays(30);

    private readonly string _cacheDir;
    private readonly Dictionary<string, byte[]> _memory = new();
    private readonly HashSet<string> _failed = new();
    private readonly HashSet<string> _loading = new();
    private readonly object _sync = new();

    private ProductImageCache()
    {
        _cacheDir = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "ElectronCash", "image-cache");
        Directory.CreateDirectory(_cacheDir);
    }

    public string CacheDirectory => _cacheDir;

    /// <summary>
    /// Быстрая попытка получить картинку только из памяти/диска.
    /// Используется при отрисовке плитки, чтобы не блокировать UI сетью.
    /// </summary>
    public Image? TryGetCached(Product product)
    {
        if (!product.HasImage || string.IsNullOrEmpty(product.ImageToken))
            return null;

        string key = CacheKey(product.Id, product.ImageToken);

        lock (_sync)
        {
            if (_memory.TryGetValue(key, out var cachedBytes))
                return Decode(cachedBytes);
            if (_failed.Contains(key) || _loading.Contains(key))
                return null;
        }

        string path = PathFor(key);
        if (!File.Exists(path))
            return null;

        try
        {
            Touch(path);
            var bytes = File.ReadAllBytes(path);
            lock (_sync) _memory[key] = bytes;
            return Decode(bytes);
        }
        catch (IOException)
        {
            return null;
        }
    }

    /// <summary>
    /// Отдаёт картинку из кеша или скачивает её с бэкенда (сетевой вызов,
    /// вызывать в фоне).
    /// </summary>
    public async Task<Image?> GetAsync(Product product)
    {
        var cached = TryGetCached(product);
        if (cached != null)
            return cached;
        if (!product.HasImage || string.IsNullOrEmpty(product.ImageToken))
            return null;

        string key = CacheKey(product.Id, product.ImageToken);
        lock (_sync)
        {
            if (_failed.Contains(key))
                return null;
            if (!_loading.Add(key))
                return null;
        }

        try
        {
            var meta = await Task.Run(() => ApiClient.Instance.GetProductImageMeta(product.Id));
            if (meta is null || !meta.Available || meta.Token != product.ImageToken)
            {
                lock (_sync) _failed.Add(key);
                return null;
            }

            var bytes = await Task.Run(() => ApiClient.Instance.DownloadProductImage(meta.DownloadUrl));
            if (bytes is null || bytes.Length == 0)
            {
                lock (_sync) _failed.Add(key);
                return null;
            }

            try
            {
                File.WriteAllBytes(PathFor(key), bytes);
            }
            catch (IOException)
            {
                // Кеш на диске не критичен — работаем из памяти
            }

            lock (_sync)
            {
                _memory[key] = bytes;
                _failed.Remove(key);
            }
            return Decode(bytes);
        }
        catch (Exception ex)
        {
            AppLog.Error($"Не удалось получить изображение товара {product.Id}", ex);
            lock (_sync) _failed.Add(key);
            return null;
        }
        finally
        {
            lock (_sync) _loading.Remove(key);
        }
    }

    /// <summary>
    /// Убирает из кеша картинки товаров, которые больше не отображаются,
    /// и записи с истёкшим сроком.
    /// </summary>
    public void Sweep(IEnumerable<Product> visibleProducts)
    {
        var alive = new HashSet<string>(
            visibleProducts
                .Where(p => p.HasImage && !string.IsNullOrEmpty(p.ImageToken))
                .Select(p => CacheKey(p.Id, p.ImageToken)));

        // Пустой каталог обычно означает ошибку загрузки, а не «все товары удалены»:
        // кеш в этом случае не трогаем, иначе теряем картинки зря.
        if (alive.Count == 0)
            return;

        lock (_sync)
        {
            foreach (var key in _memory.Keys.Where(k => !alive.Contains(k)).ToList())
                _memory.Remove(key);
            foreach (var key in _failed.Where(k => !alive.Contains(k)).ToList())
                _failed.Remove(key);
        }

        var cutoff = DateTime.UtcNow - MaxAge;
        foreach (var file in Directory.EnumerateFiles(_cacheDir))
        {
            var key = Path.GetFileNameWithoutExtension(file);
            try
            {
                var info = new FileInfo(file);
                // Просроченные записи удаляем всегда, отсутствующие в каталоге — тоже
                if (info.LastWriteTimeUtc < cutoff || !alive.Contains(key))
                    info.Delete();
            }
            catch (IOException)
            {
                // файл занят — уберём при следующей уборке
            }
        }
    }

    /// <summary>Полная очистка кеша (например, при выходе из аккаунта).</summary>
    public void Clear()
    {
        lock (_sync)
        {
            _memory.Clear();
            _failed.Clear();
        }
        foreach (var file in Directory.EnumerateFiles(_cacheDir))
        {
            try { File.Delete(file); } catch (IOException) { }
        }
    }

    private static string CacheKey(int productId, string token) => $"p{productId}_{token}";

    private string PathFor(string key) => Path.Combine(_cacheDir, key + ".bin");

    private static void Touch(string path)
    {
        try { File.SetLastWriteTimeUtc(path, DateTime.UtcNow); } catch (IOException) { }
    }

    private static Image? Decode(byte[] bytes)
    {
        try
        {
            using var stream = new MemoryStream(bytes);
            using var original = Image.FromStream(stream);
            return new Bitmap(original);
        }
        catch
        {
            return null;
        }
    }
}
