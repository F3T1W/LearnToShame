namespace LearnToShame.Services;

public class GamificationService
{
    private readonly DatabaseService _db;

    public GamificationService(DatabaseService db)
    {
        _db = db;
    }

    public async Task<bool> CanBuySessionAsync(int cost = 100)
    {
        var progress = await _db.GetUserProgressAsync();
        return progress.CurrentPoints >= cost;
    }

    public async Task PurchaseSessionAsync(int cost = 100)
    {
        var progress = await _db.GetUserProgressAsync();
        if (progress.CurrentPoints >= cost)
        {
            progress.CurrentPoints -= cost;
            await _db.UpdateUserProgressAsync(progress);
        }
    }
    
    public async Task CompleteTaskAsync(RoadmapTask task)
    {
        if (task.IsCompleted) return;
        
        task.IsCompleted = true;
        await _db.UpdateTaskAsync(task);
        
        var progress = await _db.GetUserProgressAsync();
        progress.CurrentPoints += task.PointsReward;
        await _db.UpdateUserProgressAsync(progress);
        
        await CheckLevelUpAsync(progress);
    }
    
    private async Task CheckLevelUpAsync(UserProgress progress)
    {
        var tasks = await _db.GetTasksByLevelAsync(progress.CurrentLevel);
        if (tasks.Count == 0) return;
        
        int completed = tasks.Count(t => t.IsCompleted);
        if (completed == tasks.Count && progress.CurrentLevel != DeveloperLevel.Lead)
        {
             // Level up!
             progress.CurrentLevel++;
             progress.SessionsCompletedAtCurrentLevel = 0;
             await _db.UpdateUserProgressAsync(progress);
        }
    }
}
