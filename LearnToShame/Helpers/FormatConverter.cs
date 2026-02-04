using System.Globalization;

namespace LearnToShame.Helpers;

public sealed class FormatConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value == null || parameter is not string formatKey)
            return value?.ToString() ?? string.Empty;
        var format = formatKey == "RewardPts"
            ? LocalizedStrings.Instance.RewardPtsFormat
            : formatKey;
        return string.Format(format, value);
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        throw new NotImplementedException();
}
