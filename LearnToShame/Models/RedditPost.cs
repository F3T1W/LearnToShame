namespace LearnToShame.Models;

public class RedditPost
{
    public string Title { get; set; } = string.Empty;
    public string Url { get; set; } = string.Empty;
    public string Thumbnail { get; set; } = string.Empty;
    public string Flair { get; set; } = string.Empty;
    public bool Over18 { get; set; }
}
