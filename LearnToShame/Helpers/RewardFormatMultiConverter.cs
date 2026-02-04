using System.Globalization;

namespace LearnToShame.Helpers;

/// <summary>
/// Форматирует награду. Второй аргумент (LanguageCode) нужен, чтобы при смене языка привязка пересчитывалась.
/// </summary>
public sealed class RewardFormatMultiConverter : IMultiValueConverter
{
    public object? Convert(object?[] values, Type targetType, object? parameter, CultureInfo culture)
    {
        if (values.Length < 1 || values[0] == null)
            return string.Empty;
        var format = LocalizedStrings.Instance.RewardPtsFormat;
        return string.Format(format, values[0]);
    }

    public object?[] ConvertBack(object? value, Type[] targetTypes, object? parameter, CultureInfo culture) =>
        throw new NotImplementedException();
}
