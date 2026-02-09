using SQLite;

namespace LearnToShame.Models;

public class TrainingSession
{
    [PrimaryKey, AutoIncrement]
    public int Id { get; set; }
    public DateTime Date { get; set; }
    public double DurationSeconds { get; set; }
    public DeveloperLevel Level { get; set; }
    /// <summary>Уровень контента (1–8) на момент сессии.</summary>
    public int ContentLevel { get; set; } = 1;
    /// <summary>True если пользователь переключился на Trigger перед FINISH (правильный метод).</summary>
    public bool TriggerPhaseUsed { get; set; }
    /// <summary>Секунд на Pre-Trigger до переключения. -1 если не записывалось (старые сессии).</summary>
    public double PreTriggerSeconds { get; set; } = -1;
    /// <summary>Секунд на Trigger до FINISH. -1 если не записывалось.</summary>
    public double TriggerSeconds { get; set; } = -1;
}
