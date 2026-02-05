#if IOS || MACCATALYST

using UIKit;
using Microsoft.Maui.Handlers;

namespace LearnToShame.Handlers;

public partial class SfSymbolViewHandler : ViewHandler<SfSymbolView, UIImageView>
{
    public static IPropertyMapper<SfSymbolView, SfSymbolViewHandler> Mapper = new PropertyMapper<SfSymbolView, SfSymbolViewHandler>(ViewHandler.ViewMapper)
    {
        [nameof(SfSymbolView.SymbolName)] = MapSymbolName,
    };

    public SfSymbolViewHandler() : base(Mapper) { }

    protected override UIImageView CreatePlatformView()
    {
        var imageView = new UIImageView
        {
            ContentMode = UIViewContentMode.ScaleAspectFit,
            TintColor = UIColor.Label
        };
        return imageView;
    }

    private static void MapSymbolName(SfSymbolViewHandler handler, SfSymbolView view)
    {
        if (string.IsNullOrEmpty(view.SymbolName))
        {
            handler.PlatformView.Image = null;
            return;
        }
        var img = UIImage.GetSystemImage(view.SymbolName);
        handler.PlatformView.Image = img?.ImageWithRenderingMode(UIImageRenderingMode.AlwaysTemplate);
    }
}

#endif
