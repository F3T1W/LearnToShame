#if IOS || MACCATALYST

using System.Linq;
using UIKit;
using Microsoft.Maui.Platform;

namespace LearnToShame;

public partial class AppShellIOS
{
    partial void InitTabBarIcons()
    {
        HandlerChanged += OnHandlerChanged;
    }

    private void OnHandlerChanged(object? sender, EventArgs e)
    {
        if (Handler != null)
        {
            HandlerChanged -= OnHandlerChanged;
            ApplyTabBarIcons();
        }
    }

    private void ApplyTabBarIcons()
    {
        var platformView = (Handler as IPlatformViewHandler)?.PlatformView as UIView;
        if (platformView == null)
            return;
        var vc = FindViewController(platformView);
        if (vc == null)
            return;
        var tabBarController = vc.ChildViewControllers?.FirstOrDefault(c => c is UITabBarController) as UITabBarController
            ?? vc.ChildViewControllers?.FirstOrDefault() as UITabBarController;
        if (tabBarController?.TabBar?.Items == null || tabBarController.TabBar.Items.Length < 2)
            return;
        var items = tabBarController.TabBar.Items;
        var mapImg = UIImage.GetSystemImage("map");
        var cartImg = UIImage.GetSystemImage("cart.fill");
        if (mapImg != null)
            items[0].Image = mapImg.ImageWithRenderingMode(UIImageRenderingMode.AlwaysTemplate);
        if (cartImg != null)
            items[1].Image = cartImg.ImageWithRenderingMode(UIImageRenderingMode.AlwaysTemplate);
    }

    private static UIViewController? FindViewController(UIView view)
    {
        for (var r = view.NextResponder; r != null; r = r.NextResponder)
        {
            if (r is UIViewController vc)
                return vc;
        }
        return null;
    }
}

#endif
