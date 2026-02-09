using SQLite;

namespace LearnToShame.Services;

public class DatabaseService
{
    private SQLiteAsyncConnection? _database;

    public DatabaseService()
    {
    }

    async Task Init()
    {
        if (_database is not null)
            return;

        var dbPath = Path.Combine(FileSystem.AppDataDirectory, "LearnToShame.db3");
        _database = new SQLiteAsyncConnection(dbPath);
        
        await _database.CreateTableAsync<UserProgress>();
        await _database.CreateTableAsync<RoadmapTask>();
        await _database.CreateTableAsync<TrainingSession>();

        await MigrateUserProgressContentLevelAsync();

        // Seed initial data if needed
        var progress = await _database.Table<UserProgress>().FirstOrDefaultAsync();
        if (progress == null)
        {
            await _database.InsertAsync(new UserProgress());
            await SeedTasks();
        }
    }

    private async Task MigrateUserProgressContentLevelAsync()
    {
        if (_database is null) return;
        try
        {
            await _database.ExecuteAsync("ALTER TABLE UserProgress ADD COLUMN ContentLevel INTEGER DEFAULT 1");
        }
        catch { /* column already exists */ }
        try
        {
            await _database.ExecuteAsync("ALTER TABLE UserProgress ADD COLUMN FastSessionsInRow INTEGER DEFAULT 0");
        }
        catch { /* column already exists */ }

        try
        {
            await _database.ExecuteAsync("ALTER TABLE TrainingSession ADD COLUMN ContentLevel INTEGER DEFAULT 1");
        }
        catch { /* column already exists */ }

        // Заполнить дефолты для существующих строк (SQLite оставляет NULL в новых столбцах)
        await _database.ExecuteAsync("UPDATE UserProgress SET ContentLevel = 1 WHERE ContentLevel IS NULL OR ContentLevel < 1");
        await _database.ExecuteAsync("UPDATE UserProgress SET FastSessionsInRow = 0 WHERE FastSessionsInRow IS NULL");

        try
        {
            await _database.ExecuteAsync("ALTER TABLE TrainingSession ADD COLUMN TriggerPhaseUsed INTEGER DEFAULT 0");
        }
        catch { /* column already exists */ }

        try
        {
            await _database.ExecuteAsync("ALTER TABLE TrainingSession ADD COLUMN PreTriggerSeconds REAL DEFAULT -1");
        }
        catch { /* column already exists */ }
        try
        {
            await _database.ExecuteAsync("ALTER TABLE TrainingSession ADD COLUMN TriggerSeconds REAL DEFAULT -1");
        }
        catch { /* column already exists */ }
    }

    private async Task SeedTasks()
    {
        if (_database is null) return;

        var tasks = new List<RoadmapTask>
        {
            new RoadmapTask { Title = "LeetCode Easy", Description = "Solve 50 easy problems", Level = DeveloperLevel.Intern, PointsReward = 50 },
            new RoadmapTask { Title = "Read Book", Description = "Read 'Head First C#'", Level = DeveloperLevel.Intern, PointsReward = 100 },
            new RoadmapTask { Title = "Build TODO App", Description = "Create a TODO app with SQLite", Level = DeveloperLevel.Junior, PointsReward = 150 },
            new RoadmapTask { Title = "LeetCode Medium", Description = "Solve 100 medium problems", Level = DeveloperLevel.Junior, PointsReward = 200 },
            new RoadmapTask { Title = "Microservices", Description = "Build a microservices project", Level = DeveloperLevel.Middle, PointsReward = 300 },
            new RoadmapTask { Title = "Cloud Deploy", Description = "Deploy app to Azure/AWS", Level = DeveloperLevel.Middle, PointsReward = 250 },
            new RoadmapTask { Title = "Optimization", Description = "Optimize high-load system", Level = DeveloperLevel.Senior, PointsReward = 400 },
            new RoadmapTask { Title = "Code Review", Description = "Conduct 50 code reviews", Level = DeveloperLevel.Senior, PointsReward = 300 },
            new RoadmapTask { Title = "Team Lead", Description = "Lead a team of 3 developers", Level = DeveloperLevel.Lead, PointsReward = 500 },
            new RoadmapTask { Title = "AI Integration", Description = "Integrate ML.NET model", Level = DeveloperLevel.Lead, PointsReward = 450 }
        };
        for (var i = 1; i <= 60; i++)
        {
            tasks.Add(new RoadmapTask { Title = "Test Sample", Description = $"Test Sample {i}", Level = DeveloperLevel.Intern, PointsReward = 10 });
        }
        await _database.InsertAllAsync(tasks);
    }

    public async Task<UserProgress> GetUserProgressAsync()
    {
        await Init();
        if (_database is null) throw new InvalidOperationException("Database not initialized");
        return await _database.Table<UserProgress>().FirstAsync();
    }

    public async Task UpdateUserProgressAsync(UserProgress progress)
    {
        await Init();
        if (_database is null) throw new InvalidOperationException("Database not initialized");
        await _database.UpdateAsync(progress);
    }

    public async Task<List<RoadmapTask>> GetTasksAsync()
    {
        await Init();
        if (_database is null) throw new InvalidOperationException("Database not initialized");
        var list = await _database.Table<RoadmapTask>().ToListAsync();
        // One-time: add 60 test tasks for pagination if DB had only the original 10
        if (list.Count == 10)
        {
            for (var i = 1; i <= 60; i++)
                await _database.InsertAsync(new RoadmapTask { Title = "Test Sample", Description = $"Test Sample {i}", Level = DeveloperLevel.Intern, PointsReward = 10 });
            list = await _database.Table<RoadmapTask>().ToListAsync();
        }
        return list;
    }
    
    public async Task<List<RoadmapTask>> GetTasksByLevelAsync(DeveloperLevel level)
    {
        await Init();
        if (_database is null) throw new InvalidOperationException("Database not initialized");
        return await _database.Table<RoadmapTask>().Where(t => t.Level == level).ToListAsync();
    }

    public async Task UpdateTaskAsync(RoadmapTask task)
    {
        await Init();
        if (_database is null) throw new InvalidOperationException("Database not initialized");
        await _database.UpdateAsync(task);
    }

    /// <summary>Сбрасывает все задачи в «не выполнено» и обнуляет очки/уровень Roadmap.</summary>
    public async Task ResetCompletedTasksAsync()
    {
        await Init();
        if (_database is null) throw new InvalidOperationException("Database not initialized");
        var tasks = await _database.Table<RoadmapTask>().ToListAsync();
        foreach (var t in tasks)
        {
            if (t.IsCompleted)
            {
                t.IsCompleted = false;
                await _database.UpdateAsync(t);
            }
        }
        var progress = await GetUserProgressAsync();
        progress.CurrentPoints = 0;
        progress.CurrentLevel = DeveloperLevel.Intern;
        progress.SessionsCompletedAtCurrentLevel = 0;
        await UpdateUserProgressAsync(progress);
    }

    public async Task AddSessionAsync(TrainingSession session)
    {
        await Init();
        if (_database is null) throw new InvalidOperationException("Database not initialized");
        await _database.InsertAsync(session);
    }

    /// <summary>Сессии по дате (старые первые) для графика статистики.</summary>
    public async Task<List<TrainingSession>> GetSessionsForStatsAsync(int limit = 100)
    {
        await Init();
        if (_database is null) throw new InvalidOperationException("Database not initialized");
        var list = await _database.Table<TrainingSession>().OrderBy(s => s.Date).ToListAsync();
        if (list.Count > limit)
            list = list.Skip(list.Count - limit).ToList();
        return list;
    }
}
