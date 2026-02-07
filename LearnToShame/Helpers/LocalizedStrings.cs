using System.ComponentModel;
using System.Runtime.CompilerServices;
using LearnToShame.Services;

namespace LearnToShame.Helpers;

public sealed class LocalizedStrings : INotifyPropertyChanged
{
    public static readonly LocalizedStrings Instance = new();
    private readonly LocalizationService _loc = LocalizationService.Instance;

    private LocalizedStrings()
    {
        _loc.CultureChanged += (_, _) => OnPropertyChanged(null);
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    private void OnPropertyChanged([CallerMemberName] string? name = null) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name ?? string.Empty));

    public string AppName => _loc.GetString("AppName");
    public string Roadmap => _loc.GetString("Roadmap");
    public string Shop => _loc.GetString("Shop");
    public string PointsShop => _loc.GetString("PointsShop");
    public string YourPoints => _loc.GetString("YourPoints");
    public string BuySessionButton => _loc.GetString("BuySessionButton");
    public string SessionsHelp => _loc.GetString("SessionsHelp");
    public string CurrentLevel => _loc.GetString("CurrentLevel");
    public string Tasks => _loc.GetString("Tasks");
    public string ShowPerPage => _loc.GetString("ShowPerPage");
    public string Back => _loc.GetString("Back");
    public string Forward => _loc.GetString("Forward");
    public string TrainingSession => _loc.GetString("TrainingSession");
    public string Finish => _loc.GetString("Finish");
    public string Language => _loc.GetString("Language");
    public string Yes => _loc.GetString("Yes");
    public string No => _loc.GetString("No");
    public string Buy => _loc.GetString("Buy");
    public string Cancel => _loc.GetString("Cancel");
    public string OK => _loc.GetString("OK");
    public string PickContent => _loc.GetString("PickContent");

    public string RewardPtsFormat => _loc.GetString("RewardPts");
    public string PageOfFormat => _loc.GetString("PageOf");
    public string Alert_CompleteTaskTitle => _loc.GetString("Alert_CompleteTaskTitle");
    public string Alert_CompleteTaskMessageFormat => _loc.GetString("Alert_CompleteTaskMessage");
    public string Alert_ShopTitle => _loc.GetString("Alert_ShopTitle");
    public string Alert_BuyMessageFormat => _loc.GetString("Alert_BuyMessage");
    public string Alert_NotEnoughPoints => _loc.GetString("Alert_NotEnoughPoints");
    public string Alert_SessionCompleteTitle => _loc.GetString("Alert_SessionCompleteTitle");
    public string Alert_SessionCompleteMessageFormat => _loc.GetString("Alert_SessionCompleteMessage");

    public string GetLevelName(string levelKey) => _loc.GetString(levelKey);

    /// <summary>Меняется при смене языка — используется в MultiBinding, чтобы пересчитывать конвертеры карточек.</summary>
    public string LanguageCode => _loc.CurrentLanguage;
}
