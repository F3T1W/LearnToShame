using SQLite;

namespace LearnToShame.Models;

public class TrainingSession
{
    [PrimaryKey, AutoIncrement]
    public int Id { get; set; }
    public DateTime Date { get; set; }
    public double DurationSeconds { get; set; }
    public DeveloperLevel Level { get; set; }
    /// <summary>Уровень контента с сабреддита (1–8) на момент сессии.</summary>
    public int ContentLevel { get; set; } = 1;
}
