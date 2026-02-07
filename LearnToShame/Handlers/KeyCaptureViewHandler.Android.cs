#if ANDROID

using Microsoft.Maui.Handlers;

namespace LearnToShame.Handlers;

internal partial class KeyCaptureViewHandler : ViewHandler<Views.KeyCaptureView, Android.Views.View>
{
    public static IPropertyMapper<Views.KeyCaptureView, KeyCaptureViewHandler> Mapper =
        new PropertyMapper<Views.KeyCaptureView, KeyCaptureViewHandler>(ViewHandler.ViewMapper);

    public KeyCaptureViewHandler() : base(Mapper) { }

    protected override Android.Views.View CreatePlatformView() => new Android.Views.View(Context);
}

#endif
