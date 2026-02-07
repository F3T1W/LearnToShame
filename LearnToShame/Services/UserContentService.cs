using System.Linq;
using System.Text.Json;

namespace LearnToShame.Services;

/// <summary>Контент для сессий — только то, что пользователь сам выбрал с устройства (папка/файлы). Без скачивания с Reddit/Pinterest.</summary>
public class UserContentService
{
    private static readonly string StoragePath = Path.Combine(FileSystem.AppDataDirectory, "user_images.json");
    private static readonly string ImagesDir = Path.Combine(FileSystem.AppDataDirectory, "UserImages");

    /// <summary>Возвращает пути выбранных изображений; только те, по которым файл реально существует.</summary>
    public List<string> GetUserImagePaths()
    {
        try
        {
            if (!File.Exists(StoragePath)) return new List<string>();
            var json = File.ReadAllText(StoragePath);
            var list = JsonSerializer.Deserialize<List<string>>(json) ?? new List<string>();
            var existing = list.Where(p => !string.IsNullOrEmpty(p) && File.Exists(p)).ToList();
            // Почистить JSON от несуществующих путей, чтобы счётчик и сессии не врали
            if (existing.Count != list.Count)
            {
                try { File.WriteAllText(StoragePath, JsonSerializer.Serialize(existing)); } catch { /* ignore */ }
            }
            return existing;
        }
        catch
        {
            return new List<string>();
        }
    }

    /// <summary>Открывает выбор файлов, копирует их в папку приложения и сохраняет пути к копиям.</summary>
    public async Task<int> PickAndSaveImagesAsync()
    {
        var options = new PickOptions
        {
            PickerTitle = "Select images for training",
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
            catch
            {
                // пропустить файл при ошибке копирования
            }
        }

        if (newPaths.Count == 0) return 0;
        // Заменяем список полностью: «выбранные фото» = теперь только то, что только что выбрали
        File.WriteAllText(StoragePath, JsonSerializer.Serialize(newPaths));
        return newPaths.Count;
    }

    /// <summary>Очистить сохранённый список и удалить копии изображений.</summary>
    public void Clear()
    {
        if (File.Exists(StoragePath))
            File.Delete(StoragePath);
        if (Directory.Exists(ImagesDir))
        {
            try
            {
                Directory.Delete(ImagesDir, true);
            }
            catch { /* игнорировать */ }
        }
    }

    public int Count => GetUserImagePaths().Count;
}
