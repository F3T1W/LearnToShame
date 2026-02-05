#if ANDROID

using Android.Widget;
using Android.Graphics;
using Microsoft.Maui.Handlers;
using Microsoft.Maui.Platform;

namespace LearnToShame.Handlers;

public partial class SfSymbolViewHandler : ViewHandler<SfSymbolView, TextView>
{
    public static IPropertyMapper<SfSymbolView, SfSymbolViewHandler> Mapper = new PropertyMapper<SfSymbolView, SfSymbolViewHandler>(ViewHandler.ViewMapper)
    {
        [nameof(SfSymbolView.SymbolName)] = (h, v) => { },
        [nameof(SfSymbolView.FallbackGlyph)] = MapFallbackGlyph,
    };

    public SfSymbolViewHandler() : base(Mapper) { }

    protected override TextView CreatePlatformView()
    {
        var textView = new TextView(Context)
        {
            Gravity = Android.Views.GravityFlags.Center,
            TextSize = 22
        };
        return textView;
    }

    private static void MapFallbackGlyph(SfSymbolViewHandler handler, SfSymbolView view)
    {
        handler.PlatformView.Text = view.FallbackGlyph ?? "•";
    }
}

#endif
