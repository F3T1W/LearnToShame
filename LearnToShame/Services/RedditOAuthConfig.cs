using System.Text.Json;

namespace LearnToShame.Services;

/// <summary>Учётные данные Reddit OAuth (script app). Файл reddit_oauth.json в папке данных приложения: {"ClientId":"...", "ClientSecret":"..."}.</summary>
public class RedditOAuthConfig
{
    public string? ClientId { get; set; }
    public string? ClientSecret { get; set; }

    public bool IsValid => !string.IsNullOrWhiteSpace(ClientId) && !string.IsNullOrWhiteSpace(ClientSecret);

    private static string ConfigPath => Path.Combine(FileSystem.AppDataDirectory, "reddit_oauth.json");

    public static RedditOAuthConfig Load()
    {
        try
        {
            var path = ConfigPath;
            if (!File.Exists(path)) return new RedditOAuthConfig();
            var json = File.ReadAllText(path);
            var config = JsonSerializer.Deserialize<RedditOAuthConfig>(json);
            return config ?? new RedditOAuthConfig();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[Reddit] Не удалось загрузить reddit_oauth.json: {ex.Message}");
            return new RedditOAuthConfig();
        }
    }

    public static void Save(string clientId, string clientSecret)
    {
        var path = ConfigPath;
        var config = new RedditOAuthConfig { ClientId = clientId, ClientSecret = clientSecret };
        var json = JsonSerializer.Serialize(config, new JsonSerializerOptions { WriteIndented = true });
        File.WriteAllText(path, json);
    }
}
