using System.Globalization;
using System.Text.RegularExpressions;
using LearnToShame.Models;
using LearnToShame.Services;

namespace LearnToShame.Helpers;

public sealed class TaskTextConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is not RoadmapTask task || parameter is not string part)
            return value?.ToString() ?? string.Empty;

        var loc = LocalizationService.Instance;
        var keyBase = ToKey(task.Title);

        if (part == "Title")
        {
            var k = "Task_" + keyBase + "_Title";
            var s = loc.GetString(k);
            return (string.IsNullOrEmpty(s) || s == k) ? task.Title : s;
        }

        if (part == "Description")
        {
            var k = "Task_" + keyBase + "_Desc";
            var format = loc.GetString(k);
            if (string.IsNullOrEmpty(format) || format == k) return task.Description ?? "";
            if (keyBase == "TestSample")
            {
                var num = Regex.Match(task.Description ?? "", @"\d+").Value;
                if (!string.IsNullOrEmpty(num))
                    return string.Format(format, num);
            }
            return format;
        }

        return value?.ToString() ?? string.Empty;
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        throw new NotImplementedException();

    private static string ToKey(string? title)
    {
        if (string.IsNullOrEmpty(title)) return "";
        return title.Replace(" ", "").Replace("'", "");
    }
}
