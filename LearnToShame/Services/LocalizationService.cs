using System.Globalization;
using LearnToShame.Resources;

namespace LearnToShame.Services;

public class LocalizationService
{
    public static readonly LocalizationService Instance = new();
    private const string PreferenceKey = "AppLanguage";

    private string _languageCode = "en";
    private Dictionary<string, string> _dict = AppStrings.En;

    public string CurrentLanguage => _languageCode;

    public event EventHandler? CultureChanged;

    private LocalizationService()
    {
        LoadSavedLanguage();
    }

    private void LoadSavedLanguage()
    {
        _languageCode = Preferences.Default.Get(PreferenceKey, "en");
        _dict = AppStrings.GetDictionary(_languageCode);
        ApplyCulture();
    }

    public string GetString(string key) =>
        _dict.TryGetValue(key, out var value) ? value : key;

    public string GetString(string key, params object[] args)
    {
        var format = GetString(key);
        return string.Format(format, args);
    }

    public void SetLanguage(string languageCode)
    {
        _languageCode = languageCode switch { "ru" => "ru", "uz" => "uz", "ja" => "ja", _ => "en" };
        _dict = AppStrings.GetDictionary(_languageCode);
        Preferences.Default.Set(PreferenceKey, _languageCode);
        ApplyCulture();
        CultureChanged?.Invoke(this, EventArgs.Empty);
    }

    private void ApplyCulture()
    {
        var culture = _languageCode switch
        {
            "ru" => new CultureInfo("ru"),
            "uz" => new CultureInfo("uz"),
            "ja" => new CultureInfo("ja"),
            _ => new CultureInfo("en")
        };
        Thread.CurrentThread.CurrentCulture = culture;
        Thread.CurrentThread.CurrentUICulture = culture;
        CultureInfo.DefaultThreadCurrentCulture = culture;
        CultureInfo.DefaultThreadCurrentUICulture = culture;
    }

    public static readonly (string Code, string DisplayName)[] Languages =
    {
        ("en", "English"),
        ("ru", "Русский"),
        ("uz", "O'zbekcha"),
        ("ja", "日本語")
    };
}
