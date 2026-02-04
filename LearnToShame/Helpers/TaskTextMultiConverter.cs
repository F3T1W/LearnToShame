using System.Globalization;
using System.Text.RegularExpressions;
using LearnToShame.Models;
using LearnToShame.Services;

namespace LearnToShame.Helpers;

/// <summary>
/// Конвертер для заголовка/описания карточки. Второй аргумент (LanguageCode) нужен, чтобы при смене языка привязка пересчитывалась.
/// </summary>
public sealed class TaskTextMultiConverter : IMultiValueConverter
{
    public object? Convert(object?[] values, Type targetType, object? parameter, CultureInfo culture)
    {
        if (values.Length < 1 || values[0] is not RoadmapTask task || parameter is not string part)
            return string.Empty;

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

        return string.Empty;
    }

    public object?[] ConvertBack(object? value, Type[] targetTypes, object? parameter, CultureInfo culture) =>
        throw new NotImplementedException();

    private static string ToKey(string? title)
    {
        if (string.IsNullOrEmpty(title)) return "";
        return title.Replace(" ", "").Replace("'", "");
    }
}
