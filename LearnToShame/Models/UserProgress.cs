using SQLite;

namespace LearnToShame.Models;

public enum DeveloperLevel
{
    Intern = 1,
    Junior = 2,
    Middle = 3,
    Senior = 4,
    Lead = 5
}

public class UserProgress
{
    [PrimaryKey]
    public int Id { get; set; } = 1; // Singleton record
    public int CurrentPoints { get; set; }
    /// <summary>Уровень Roadmap (C#): растёт только когда все задачи текущего уровня выполнены.</summary>
    public DeveloperLevel CurrentLevel { get; set; } = DeveloperLevel.Intern;
    public int SessionsCompletedAtCurrentLevel { get; set; }

    /// <summary>Уровень контента с сабреддита (1–8): Level 1 — самые прикрытые, 8 — менее. Растёт при 5 сессиях подряд &lt; 1 мин на одном уровне.</summary>
    public int ContentLevel { get; set; } = 1;
    /// <summary>Подряд завершённых сессий &lt; 60 сек на текущем ContentLevel. При 5 — переход на следующий ContentLevel.</summary>
    public int FastSessionsInRow { get; set; }
}
