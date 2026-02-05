namespace LearnToShame.Views;

/// <summary>
/// Shows an SF Symbol on iOS; on Android shows a fallback glyph (e.g. emoji) or placeholder.
/// </summary>
public class SfSymbolView : View
{
    public static readonly BindableProperty SymbolNameProperty = BindableProperty.Create(
        nameof(SymbolName), typeof(string), typeof(SfSymbolView), default(string));

    /// <summary>SF Symbol name on iOS (e.g. "map", "cart.fill"). Ignored on Android.</summary>
    public string? SymbolName
    {
        get => (string?)GetValue(SymbolNameProperty);
        set => SetValue(SymbolNameProperty, value);
    }

    public static readonly BindableProperty FallbackGlyphProperty = BindableProperty.Create(
        nameof(FallbackGlyph), typeof(string), typeof(SfSymbolView), "•");

    /// <summary>Shown on Android when SF Symbols are not available (e.g. "🗺", "🛒").</summary>
    public string? FallbackGlyph
    {
        get => (string?)GetValue(FallbackGlyphProperty);
        set => SetValue(FallbackGlyphProperty, value);
    }
}
