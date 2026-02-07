using System.Security.Cryptography;

namespace LearnToShame.Services;

/// <summary>Кэш изображений по уровням контента (1–8) для офлайн-тренировок. Все фото в кэше уникальные (по хешу содержимого). Уровень 8: Reddit + при нехватке Pinterest «hijabi girl».</summary>
public class ContentCacheService
{
    private readonly RedditService _reddit;
    private readonly PinterestService _pinterest;
    private readonly HttpClient _httpClient;
    private static readonly string CacheRoot = Path.Combine(FileSystem.AppDataDirectory, "ContentCache");

    public const int MinLevel = 1;
    public const int MaxLevel = 8;
    public const int DefaultMaxPerLevel = 50;

    public ContentCacheService(RedditService reddit, PinterestService pinterest)
    {
        _reddit = reddit;
        _pinterest = pinterest;
        _httpClient = new HttpClient();
        _httpClient.DefaultRequestHeaders.TryAddWithoutValidation("User-Agent", "LearnToShame/1.0 (by /u/learning_dev)");
    }

    /// <summary>Папка кэша для уровня (1–8).</summary>
    private static string GetLevelDir(int level) => Path.Combine(CacheRoot, $"Level_{level}");

    /// <summary>URL картинок для уровня (для сессии без кэша). Уровень 8: Reddit + при нехватке Pinterest «hijabi girl».</summary>
    public async Task<List<string>> GetImageUrlsForLevelAsync(int level, CancellationToken cancellationToken = default)
    {
        if (level == MaxLevel)
        {
            var redditPosts = await _reddit.GetPostsForLevelAsync(level);
            var list = redditPosts.Select(p => p.Url).Where(u => !string.IsNullOrEmpty(u)).ToList();
            if (list.Count < 20)
            {
                var pinterestUrls = await _pinterest.GetImageUrlsAsync(50, cancellationToken);
                foreach (var u in pinterestUrls)
                    if (!list.Contains(u, StringComparer.OrdinalIgnoreCase))
                        list.Add(u);
            }
            return list;
        }
        var posts = await _reddit.GetPostsForLevelAsync(level);
        return posts.Select(p => p.Url).Where(u => !string.IsNullOrEmpty(u)).ToList();
    }

    /// <summary>Список путей к закэшированным файлам для уровня (пустые если нет кэша).</summary>
    public List<string> GetCachedImagePaths(int level)
    {
        var dir = GetLevelDir(level);
        if (!Directory.Exists(dir)) return new List<string>();
        return Directory.EnumerateFiles(dir, "*.*")
            .Where(f =>
                f.EndsWith(".jpg", StringComparison.OrdinalIgnoreCase) ||
                f.EndsWith(".jpeg", StringComparison.OrdinalIgnoreCase) ||
                f.EndsWith(".png", StringComparison.OrdinalIgnoreCase) ||
                f.EndsWith(".gif", StringComparison.OrdinalIgnoreCase) ||
                f.EndsWith(".webp", StringComparison.OrdinalIgnoreCase))
            .ToList();
    }

    /// <summary>Скачивает до maxPerLevel изображений для уровня. Уровень 8: сначала Reddit, при нехватке — Pinterest «hijabi girl». Все кэшированные фото уникальны (имя файла = хеш содержимого).</summary>
    public async Task<int> DownloadLevelAsync(int level, int maxPerLevel, IProgress<(int level, int downloaded, int total)>? progress = null, CancellationToken cancellationToken = default)
    {
        progress?.Report((level, 0, maxPerLevel));
        Console.WriteLine($"[Cache] Level {level}: начинаем, цель до {maxPerLevel} фото");

        var dir = GetLevelDir(level);
        Directory.CreateDirectory(dir);

        var urlsToDownload = new List<string>();

        if (level == MaxLevel)
        {
            var redditPosts = await _reddit.GetPostsForLevelAsync(level);
            var redditUrls = redditPosts.Select(p => p.Url).Where(u => !string.IsNullOrEmpty(u)).ToList();
            urlsToDownload.AddRange(redditUrls.Take(maxPerLevel));
            Console.WriteLine($"[Cache] Level 8: Reddit дал {redditUrls.Count} URL");
            if (urlsToDownload.Count < maxPerLevel)
            {
                var pinterestUrls = await _pinterest.GetImageUrlsAsync(maxPerLevel - urlsToDownload.Count, cancellationToken);
                foreach (var u in pinterestUrls)
                {
                    if (urlsToDownload.Count >= maxPerLevel) break;
                    if (!urlsToDownload.Contains(u, StringComparer.OrdinalIgnoreCase))
                        urlsToDownload.Add(u);
                }
                Console.WriteLine($"[Cache] Level 8: после Pinterest всего {urlsToDownload.Count} URL");
            }
        }
        else
        {
            var posts = await _reddit.GetPostsForLevelAsync(level);
            urlsToDownload.AddRange(posts.Take(maxPerLevel).Select(p => p.Url).Where(u => !string.IsNullOrEmpty(u)));
            Console.WriteLine($"[Cache] Level {level}: Reddit дал {urlsToDownload.Count} URL");
        }

        int total = urlsToDownload.Count;
        progress?.Report((level, 0, total));
        int downloaded = 0;
        var existingHashes = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        if (Directory.Exists(dir))
        {
            foreach (var f in Directory.EnumerateFiles(dir))
                existingHashes.Add(Path.GetFileNameWithoutExtension(f));
        }

        foreach (var url in urlsToDownload)
        {
            if (cancellationToken.IsCancellationRequested) break;
            if (string.IsNullOrEmpty(url)) continue;

            try
            {
                var bytes = await _httpClient.GetByteArrayAsync(url, cancellationToken);
                var (contentHash, ext) = GetFileNameFromContent(bytes, url);
                if (existingHashes.Contains(contentHash))
                {
                    downloaded++;
                    progress?.Report((level, downloaded, total));
                    continue;
                }
                var path = Path.Combine(dir, contentHash + ext);
                await File.WriteAllBytesAsync(path, bytes, cancellationToken);
                existingHashes.Add(contentHash);
                downloaded++;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Cache] Ошибка загрузки {url}: {ex.Message}");
                Debug.WriteLine($"ContentCache: failed to download {url}: {ex.Message}");
            }

            progress?.Report((level, downloaded, total));
        }

        return downloaded;
    }

    /// <summary>Имя файла по хешу содержимого — все кэшированные фото уникальны.</summary>
    private static (string contentHash, string ext) GetFileNameFromContent(byte[] bytes, string url)
    {
        var hash = Convert.ToHexString(SHA256.HashData(bytes)).AsSpan(0, 16).ToString();
        var ext = ".jpg";
        var lower = url.ToLowerInvariant();
        if (lower.Contains(".png")) ext = ".png";
        else if (lower.Contains(".gif")) ext = ".gif";
        else if (lower.Contains(".webp")) ext = ".webp";
        return (hash, ext);
    }

    /// <summary>Скачивает контент для всех уровней 1–8. progress: (level, downloaded, total).</summary>
    public async Task<Dictionary<int, int>> DownloadAllLevelsAsync(int maxPerLevel = DefaultMaxPerLevel, IProgress<(int level, int downloaded, int total)>? progress = null, CancellationToken cancellationToken = default)
    {
        var result = new Dictionary<int, int>();
        for (int level = MinLevel; level <= MaxLevel; level++)
        {
            if (cancellationToken.IsCancellationRequested) break;
            int count = await DownloadLevelAsync(level, maxPerLevel, progress, cancellationToken);
            result[level] = count;
        }
        return result;
    }

    /// <summary>Выбирает нужное количество в случайном порядке. Если в источнике достаточно — без повторов; иначе с повторами.</summary>
    public static List<string> PickRandomInRandomOrder(IList<string> source, int count, Random? rnd = null)
    {
        rnd ??= new Random();
        if (source.Count == 0) return new List<string>();
        List<string> list;
        if (source.Count >= count)
        {
            var shuffled = source.OrderBy(_ => rnd.Next()).Take(count).ToList();
            list = new List<string>(shuffled);
        }
        else
        {
            list = new List<string>(count);
            for (int i = 0; i < count; i++)
                list.Add(source[rnd.Next(source.Count)]);
        }
        // Финальное перемешивание порядка показа
        for (int i = list.Count - 1; i > 0; i--)
        {
            int j = rnd.Next(i + 1);
            (list[i], list[j]) = (list[j], list[i]);
        }
        return list;
    }

    public void ClearCache(int? level = null)
    {
        if (level.HasValue)
        {
            var dir = GetLevelDir(level.Value);
            if (Directory.Exists(dir))
                Directory.Delete(dir, true);
        }
        else if (Directory.Exists(CacheRoot))
        {
            Directory.Delete(CacheRoot, true);
        }
    }
}
