using System.Text.RegularExpressions;

namespace LearnToShame.Services;

/// <summary>Для уровня 8: когда постов Reddit не хватает, подтягиваем картинки по запросу «hijabi girl» с Pinterest.</summary>
public class PinterestService
{
    private readonly HttpClient _httpClient;
    private const string SearchQuery = "hijabi girl";
    private static readonly Regex PinImgRegex = new(@"https://i\.pinimg\.com/[^\s""'<>]+\.(?:jpg|jpeg|png|webp)", RegexOptions.IgnoreCase | RegexOptions.Compiled);

    public PinterestService()
    {
        _httpClient = new HttpClient();
        _httpClient.DefaultRequestHeaders.TryAddWithoutValidation("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/120.0.0.0 Safari/537.36");
        _httpClient.DefaultRequestHeaders.TryAddWithoutValidation("Accept", "text/html,application/xhtml+xml,application/xml;q=0.9,*/*;q=0.8");
    }

    /// <summary>Возвращает URL картинок с Pinterest по запросу «hijabi girl». Может вернуть пустой список, если страница не отдаёт URL (Pinterest часто рендерит через JS).</summary>
    public async Task<List<string>> GetImageUrlsAsync(int maxCount = 50, CancellationToken cancellationToken = default)
    {
        var urls = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        try
        {
            Console.WriteLine($"[Pinterest] Запрос до {maxCount} URL по «{SearchQuery}» ...");
            var searchUrl = "https://www.pinterest.com/search/pins/?q=" + Uri.EscapeDataString(SearchQuery);
            var html = await _httpClient.GetStringAsync(searchUrl, cancellationToken);
            foreach (Match m in PinImgRegex.Matches(html))
            {
                var url = m.Value;
                if (string.IsNullOrEmpty(url)) continue;
                url = url.TrimEnd('\\', '"', '\'');
                if (url.Contains("avatar") || url.Contains("logo") || url.Length < 30) continue;
                urls.Add(url);
                if (urls.Count >= maxCount) break;
            }
            Console.WriteLine($"[Pinterest] Найдено {urls.Count} URL");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[Pinterest] Ошибка: {ex.Message}");
            Debug.WriteLine($"PinterestService: {ex.Message}");
        }
        return urls.Take(maxCount).ToList();
    }
}
