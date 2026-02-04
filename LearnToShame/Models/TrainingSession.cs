using SQLite;

namespace LearnToShame.Models;

public class TrainingSession
{
    [PrimaryKey, AutoIncrement]
    public int Id { get; set; }
    public DateTime Date { get; set; }
    public double DurationSeconds { get; set; }
    public DeveloperLevel Level { get; set; }
}
