using LearnToShame.Helpers;
using LearnToShame.Views;

namespace LearnToShame;

/// <summary>
/// Shell with native TabBar for iOS — gets Liquid Glass styling on iOS 26+.
/// Used only on iOS; Android keeps AppShell with custom bottom bar.
/// </summary>
public partial class AppShellIOS : Shell
{
    private readonly Tab _roadmapTab;
    private readonly Tab _shopTab;

    partial void InitTabBarIcons();

    public AppShellIOS(RoadmapHostPage roadmapHostPage, ShopHostPage shopHostPage)
    {
        FlyoutBehavior = FlyoutBehavior.Disabled;
        Items.Add(new TabBar
        {
            Items =
            {
                (_roadmapTab = new Tab
                {
                    Title = "", // только иконка, название — сверху в хост-странице
                    Icon = "icon_map",
                    Items = { new ShellContent { Content = roadmapHostPage, Route = "Roadmap" } }
                }),
                (_shopTab = new Tab
                {
                    Title = "",
                    Icon = "icon_cart",
                    Items = { new ShellContent { Content = shopHostPage, Route = "Shop" } }
                })
            }
        });
        LocalizationService.Instance.CultureChanged += (_, _) => UpdateTabTitles();
        Routing.RegisterRoute(nameof(SessionPage), typeof(SessionPage));
#if IOS || MACCATALYST
        InitTabBarIcons();
#endif
    }

    private void UpdateTabTitles()
    {
        _roadmapTab.Title = "";
        _shopTab.Title = "";
    }
}
