#if WINDOWS

using Microsoft.UI.Xaml.Controls;
using Microsoft.Maui.Handlers;

namespace LearnToShame.Handlers;

internal partial class KeyCaptureViewHandler : ViewHandler<Views.KeyCaptureView, Border>
{
    public static IPropertyMapper<Views.KeyCaptureView, KeyCaptureViewHandler> Mapper =
        new PropertyMapper<Views.KeyCaptureView, KeyCaptureViewHandler>(ViewHandler.ViewMapper);

    public KeyCaptureViewHandler() : base(Mapper) { }

    protected override Border CreatePlatformView() => new Border();
}

#endif
