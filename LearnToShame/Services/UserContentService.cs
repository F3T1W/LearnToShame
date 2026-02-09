using System.Linq;
using System.Text.Json;

namespace LearnToShame.Services;

/// <summary>Trigger Method: Pre-Trigger (arousal) and Trigger (focus at orgasm). Two separate image lists.</summary>
public class UserContentService
{
    private static readonly string PreTriggerPath = Path.Combine(FileSystem.AppDataDirectory, "user_pre_trigger.json");
    private static readonly string TriggerPath = Path.Combine(FileSystem.AppDataDirectory, "user_trigger.json");
    private static readonly string ImagesDir = Path.Combine(FileSystem.AppDataDirectory, "UserImages");

    private static void MigrateLegacyIfNeeded()
    {
        var legacyPath = Path.Combine(FileSystem.AppDataDirectory, "user_images.json");
        if (!File.Exists(legacyPath) || File.Exists(PreTriggerPath)) return;
        try
        {
            var json = File.ReadAllText(legacyPath);
            File.WriteAllText(PreTriggerPath, json);
            File.Delete(legacyPath);
        }
        catch { }
    }

    private static List<string> LoadAndFilter(string path)
    {
        MigrateLegacyIfNeeded();
        try
        {
            if (!File.Exists(path)) return new List<string>();
            var json = File.ReadAllText(path);
            var list = JsonSerializer.Deserialize<List<string>>(json) ?? new List<string>();
            var existing = list.Where(p => !string.IsNullOrEmpty(p) && File.Exists(p)).ToList();
            if (existing.Count != list.Count)
            {
                try { File.WriteAllText(path, JsonSerializer.Serialize(existing)); } catch { }
            }
            return existing;
        }
        catch
        {
            return new List<string>();
        }
    }

    public List<string> GetPreTriggerPaths() => LoadAndFilter(PreTriggerPath);
    public List<string> GetTriggerPaths() => LoadAndFilter(TriggerPath);

    /// <summary>Legacy: combined list (Pre + Trigger) for backward compat.</summary>
    public List<string> GetUserImagePaths()
    {
        var pre = GetPreTriggerPaths();
        var trigger = GetTriggerPaths();
        return pre.Concat(trigger).Distinct().ToList();
    }

    public async Task<int> PickAndSaveImagesAsync(ContentRole role)
    {
        var options = new PickOptions
        {
            PickerTitle = role == ContentRole.PreTrigger ? "Select Pre-Trigger images" : "Select Trigger images",
            FileTypes = FilePickerFileType.Images
        };
        var results = await FilePicker.Default.PickMultipleAsync(options);
        var list = results?.ToList() ?? new List<FileResult>();
        if (list.Count == 0) return 0;

        if (!Directory.Exists(ImagesDir))
            Directory.CreateDirectory(ImagesDir);

        var newPaths = new List<string>();
        foreach (var f in list)
        {
            if (string.IsNullOrEmpty(f.FullPath)) continue;
            try
            {
                using var stream = await f.OpenReadAsync();
                var ext = Path.GetExtension(f.FileName);
                if (string.IsNullOrEmpty(ext)) ext = ".jpg";
                var destName = $"{Guid.NewGuid():N}{ext}";
                var destPath = Path.Combine(ImagesDir, destName);
                using (var dest = File.Create(destPath))
                    await stream.CopyToAsync(dest);
                newPaths.Add(destPath);
            }
            catch { }
        }

        if (newPaths.Count == 0) return 0;
        var path = role == ContentRole.PreTrigger ? PreTriggerPath : TriggerPath;
        File.WriteAllText(path, JsonSerializer.Serialize(newPaths));
        return newPaths.Count;
    }

    public void ClearPreTrigger()
    {
        if (File.Exists(PreTriggerPath)) File.Delete(PreTriggerPath);
    }

    public void ClearTrigger()
    {
        if (File.Exists(TriggerPath)) File.Delete(TriggerPath);
    }

    public void Clear()
    {
        ClearPreTrigger();
        ClearTrigger();
        if (Directory.Exists(ImagesDir))
        {
            try { Directory.Delete(ImagesDir, true); } catch { }
        }
    }

    public int CountPreTrigger => GetPreTriggerPaths().Count;
    public int CountTrigger => GetTriggerPaths().Count;
    public int Count => GetPreTriggerPaths().Count + GetTriggerPaths().Count;
}

public enum ContentRole
{
    PreTrigger,
    Trigger
}
