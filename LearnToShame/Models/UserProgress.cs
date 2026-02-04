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
    public DeveloperLevel CurrentLevel { get; set; } = DeveloperLevel.Intern;
    public int SessionsCompletedAtCurrentLevel { get; set; }
}
