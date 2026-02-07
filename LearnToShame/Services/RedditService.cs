using System.Net;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
#if IOS || MACCATALYST
using Foundation;
#endif

namespace LearnToShame.Services;

/// <summary>С OAuth (reddit_oauth.json): запросы к oauth.reddit.com с Bearer. Без конфига — www/old (часто 403).</summary>
public class RedditService
{
    private readonly HttpClient _httpClient;
    private const string Subreddit = "PrejacLevelTraining";
    private const string HotUrlOAuth = "https://oauth.reddit.com/r/" + Subreddit + "/hot.json";
    private const string HotUrlWww = "https://www.reddit.com/r/" + Subreddit + "/hot.json";
    private const string HotUrlOld = "https://old.reddit.com/r/" + Subreddit + "/hot.json";
    private const int PageLimit = 100;
    private const int MaxPages = 20;
    private const int DelayMs = 400;

    private string? _accessToken;
    private DateTime _tokenExpiry = DateTime.MinValue;
    private readonly SemaphoreSlim _tokenLock = new(1, 1);

    public RedditService()
    {
        _httpClient = CreateRedditHttpClient();
        _httpClient.DefaultRequestHeaders.TryAddWithoutValidation("User-Agent", "LearnToShame/1.0 (by /u/learning_dev)");
    }

    private static HttpClient CreateRedditHttpClient()
    {
#if IOS || MACCATALYST
        var handler = new NSUrlSessionHandler();
        return new HttpClient(handler);
#else
        return new HttpClient();
#endif
    }

    private const int MinPostsBeforeFallback = 15;

    public async Task<List<RedditPost>> GetPostsForLevelAsync(int level)
    {
        var oauth = RedditOAuthConfig.Load();
        if (oauth.IsValid)
        {
            var token = await GetOrRefreshTokenAsync(oauth);
            if (!string.IsNullOrEmpty(token))
            {
                Console.WriteLine($"[Reddit] Level {level}: запрос oauth.reddit.com (OAuth) ...");
                var all = await ScrapeHotWithFlairAsync(HotUrlOAuth, level, bearerToken: token);
                Console.WriteLine($"[Reddit] Level {level}: с OAuth получено {all.Count} постов");
                if (all.Count >= MinPostsBeforeFallback)
                {
                    Console.WriteLine($"[Reddit] Level {level}: итого {all.Count} постов");
                    return all;
                }
                Console.WriteLine($"[Reddit] Level {level}: мало постов, добираем без OAuth");
            }
        }
        else
        {
            Console.WriteLine($"[Reddit] reddit_oauth.json не найден или пуст — используем публичный API (часто 403).");
            Console.WriteLine($"[Reddit] Положите файл сюда: {Path.Combine(FileSystem.AppDataDirectory, "reddit_oauth.json")}");
            Console.WriteLine("[Reddit] Содержимое: {\"ClientId\":\"...\", \"ClientSecret\":\"...\"}. Создайте script-приложение на https://www.reddit.com/prefs/apps");
        }

        Console.WriteLine($"[Reddit] Level {level}: запрос www.reddit.com ...");
        var fromWww = await ScrapeHotWithFlairAsync(HotUrlWww, level, bearerToken: null);
        var allList = new List<RedditPost>(fromWww);
        Console.WriteLine($"[Reddit] Level {level}: с www получено {fromWww.Count} постов");
        if (allList.Count < MinPostsBeforeFallback)
        {
            Console.WriteLine($"[Reddit] Level {level}: добираем с old.reddit.com ...");
            var fromOld = await ScrapeHotWithFlairAsync(HotUrlOld, level, bearerToken: null);
            var seen = new HashSet<string>(allList.Select(p => p.Url), StringComparer.OrdinalIgnoreCase);
            foreach (var p in fromOld)
                if (seen.Add(p.Url))
                    allList.Add(p);
        }
        Console.WriteLine($"[Reddit] Level {level}: итого {allList.Count} постов");
        return allList;
    }

    private async Task<string?> GetOrRefreshTokenAsync(RedditOAuthConfig oauth)
    {
        if (!string.IsNullOrEmpty(_accessToken) && DateTime.UtcNow < _tokenExpiry)
            return _accessToken;
        await _tokenLock.WaitAsync();
        try
        {
            if (!string.IsNullOrEmpty(_accessToken) && DateTime.UtcNow < _tokenExpiry)
                return _accessToken;
            var token = await FetchTokenAsync(oauth);
            if (!string.IsNullOrEmpty(token))
            {
                _accessToken = token;
                _tokenExpiry = DateTime.UtcNow.AddMinutes(55);
                Console.WriteLine("[Reddit] OAuth токен получен");
            }
            return _accessToken;
        }
        finally
        {
            _tokenLock.Release();
        }
    }

    private async Task<string?> FetchTokenAsync(RedditOAuthConfig oauth)
    {
        try
        {
            var auth = Convert.ToBase64String(Encoding.ASCII.GetBytes(oauth.ClientId! + ":" + oauth.ClientSecret!));
            using var req = new HttpRequestMessage(HttpMethod.Post, "https://www.reddit.com/api/v1/access_token");
            req.Headers.Authorization = new AuthenticationHeaderValue("Basic", auth);
            req.Content = new FormUrlEncodedContent(new[] { new KeyValuePair<string?, string?>("grant_type", "client_credentials") });
            var response = await _httpClient.SendAsync(req);
            var json = await response.Content.ReadAsStringAsync();
            if (!response.IsSuccessStatusCode)
            {
                Console.WriteLine($"[Reddit] OAuth token failed: {(int)response.StatusCode} {json}");
                return null;
            }
            using var doc = JsonDocument.Parse(json);
            if (doc.RootElement.TryGetProperty("access_token", out var tok))
                return tok.GetString();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[Reddit] OAuth token error: {ex.Message}");
        }
        return null;
    }

    private async Task<List<RedditPost>> ScrapeHotWithFlairAsync(string baseUrl, int level, string? bearerToken)
    {
        var all = new List<RedditPost>();
        string? after = null;
        int pages = 0;
        try
        {
            while (pages < MaxPages)
            {
                var url = baseUrl + "?limit=" + PageLimit + (after != null ? "&after=" + Uri.EscapeDataString(after) : "");
                using var req = new HttpRequestMessage(HttpMethod.Get, url);
                if (!string.IsNullOrEmpty(bearerToken))
                    req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", bearerToken);
                var response = await _httpClient.SendAsync(req);
                if (!response.IsSuccessStatusCode)
                {
                    Console.WriteLine($"[Reddit] HTTP {(int)response.StatusCode} {response.StatusCode}");
                    break;
                }
                var json = await response.Content.ReadAsStringAsync();
                var (posts, nextAfter) = ParseHotPage(json, level);
                if (pages == 0 && json.Length > 0)
                    Console.WriteLine($"[Reddit] Первая страница: {json.Length} символов, детей в data: {GetChildrenCount(json)}, подходящих по флейру Level {level}: {posts.Count}");
                all.AddRange(posts);
                after = nextAfter;
                pages++;
                if (string.IsNullOrEmpty(after)) break;
                await Task.Delay(DelayMs);
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[Reddit] Ошибка: {ex.Message}");
            Debug.WriteLine($"Reddit: {ex.Message}");
        }
        return all;
    }

    private static int GetChildrenCount(string json)
    {
        try
        {
            using var doc = JsonDocument.Parse(json);
            if (doc.RootElement.TryGetProperty("data", out var data) && data.TryGetProperty("children", out var ch))
                return ch.GetArrayLength();
        }
        catch { }
        return -1;
    }

    private (List<RedditPost> posts, string? after) ParseHotPage(string json, int level)
    {
        var posts = new List<RedditPost>();
        string? after = null;
        try
        {
            using var doc = JsonDocument.Parse(json);
            if (!doc.RootElement.TryGetProperty("data", out var data)) return (posts, null);
            if (data.TryGetProperty("after", out var afterEl)) after = afterEl.GetString();
            if (!data.TryGetProperty("children", out var children)) return (posts, after);

            foreach (var child in children.EnumerateArray())
            {
                if (!child.TryGetProperty("data", out var postData)) continue;
                var flair = postData.TryGetProperty("link_flair_text", out var f) ? f.GetString() ?? "" : "";
                if (!MatchesFlair(flair, level)) continue;
                var imageUrl = GetImageUrl(postData);
                if (string.IsNullOrEmpty(imageUrl)) continue;
                posts.Add(MakePost(postData, imageUrl, flair ?? ""));
            }
        }
        catch (Exception e)
        {
            Console.WriteLine($"[Reddit] Parse error: {e.Message}");
            Debug.WriteLine($"Reddit parse: {e.Message}");
        }
        return (posts, after);
    }

    /// <summary>Как в yolo_trainer: url_overridden_by_dest или url (прямая картинка), иначе preview.</summary>
    private static string? GetImageUrl(JsonElement postData)
    {
        var urlOverridden = postData.TryGetProperty("url_overridden_by_dest", out var uod) ? uod.GetString() : null;
        var url = postData.TryGetProperty("url", out var u) ? u.GetString() : null;
        var raw = !string.IsNullOrEmpty(urlOverridden) ? urlOverridden! : url ?? "";
        if (IsImageUrl(raw)) return raw;
        if (postData.TryGetProperty("preview", out var preview) &&
            preview.TryGetProperty("images", out var images) && images.GetArrayLength() > 0)
        {
            var first = images[0];
            if (first.TryGetProperty("source", out var source) && source.TryGetProperty("url", out var urlEl))
            {
                var decoded = WebUtility.HtmlDecode(urlEl.GetString());
                if (!string.IsNullOrEmpty(decoded) && IsImageUrl(decoded)) return decoded;
            }
        }
        return null;
    }

    private static bool MatchesFlair(string? flair, int level)
    {
        if (string.IsNullOrWhiteSpace(flair)) return false;
        var t = flair.Trim();
        return string.Equals(t, "Level " + level, StringComparison.OrdinalIgnoreCase)
            || (t.StartsWith("Level", StringComparison.OrdinalIgnoreCase) && t.Contains(level.ToString()));
    }

    private static RedditPost MakePost(JsonElement postData, string imageUrl, string flair)
    {
        return new RedditPost
        {
            Title = postData.TryGetProperty("title", out var t) ? t.GetString() ?? "" : "",
            Url = imageUrl,
            Thumbnail = postData.TryGetProperty("thumbnail", out var th) ? th.GetString() ?? "" : "",
            Flair = flair,
            Over18 = postData.TryGetProperty("over_18", out var o) && o.GetBoolean()
        };
    }

    private static bool IsImageUrl(string url)
    {
        if (string.IsNullOrEmpty(url)) return false;
        var lower = url.ToLowerInvariant();
        return lower.EndsWith(".jpg") || lower.EndsWith(".jpeg") || lower.EndsWith(".png") || lower.EndsWith(".gif") || lower.EndsWith(".webp")
            || lower.Contains("i.imgur.com") || lower.Contains("imgur.com")
            || lower.Contains("i.redd.it") || lower.Contains("preview.redd.it");
    }
}
