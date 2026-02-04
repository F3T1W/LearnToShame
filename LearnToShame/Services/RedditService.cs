using System.Net.Http.Json;
using System.Text.Json;

namespace LearnToShame.Services;

public class RedditService
{
    private readonly HttpClient _httpClient;
    private const string BaseUrl = "https://www.reddit.com/r/PrejacLevelTraining/search.json";

    public RedditService()
    {
        _httpClient = new HttpClient();
        _httpClient.DefaultRequestHeaders.Add("User-Agent", "android:com.learntoshame.app:v1.0 (by /u/learning_dev)");
    }

    public async Task<List<RedditPost>> GetPostsForLevelAsync(int level)
    {
        // Search query: "flair:Level X" restrict_sr=1 sort=hot
        string query = $"flair:\"Level {level}\"";
        string url = $"{BaseUrl}?q={Uri.EscapeDataString(query)}&restrict_sr=1&sort=hot&limit=50";

        try 
        {
            var response = await _httpClient.GetAsync(url);
            if (!response.IsSuccessStatusCode) return new List<RedditPost>();

            var json = await response.Content.ReadAsStringAsync();
            return ParseRedditResponse(json);
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Error fetching reddit posts: {ex.Message}");
            return new List<RedditPost>();
        }
    }
    
    private List<RedditPost> ParseRedditResponse(string json)
    {
        var posts = new List<RedditPost>();
        try 
        {
            using var doc = JsonDocument.Parse(json);
            if (!doc.RootElement.TryGetProperty("data", out var data)) return posts;
            if (!data.TryGetProperty("children", out var children)) return posts;
            
            foreach (var child in children.EnumerateArray())
            {
                var postData = child.GetProperty("data");
                var post = new RedditPost
                {
                    Title = postData.TryGetProperty("title", out var t) ? t.GetString() ?? "" : "",
                    Url = postData.TryGetProperty("url", out var u) ? u.GetString() ?? "" : "",
                    Thumbnail = postData.TryGetProperty("thumbnail", out var th) ? th.GetString() ?? "" : "",
                    Flair = postData.TryGetProperty("link_flair_text", out var f) ? f.GetString() ?? "" : "",
                    Over18 = postData.TryGetProperty("over_18", out var o) && o.GetBoolean()
                };
                
                if (IsImageUrl(post.Url))
                {
                    posts.Add(post);
                }
            }
        }
        catch(Exception e)
        {
            Debug.WriteLine($"Error parsing reddit JSON: {e}");
        }
        
        return posts;
    }
    
    private bool IsImageUrl(string url)
    {
        if (string.IsNullOrEmpty(url)) return false;
        var lower = url.ToLowerInvariant();
        return lower.EndsWith(".jpg") || lower.EndsWith(".jpeg") || lower.EndsWith(".png") || lower.EndsWith(".gif") || lower.Contains("i.imgur.com") || lower.Contains("i.redd.it");
    }
}
