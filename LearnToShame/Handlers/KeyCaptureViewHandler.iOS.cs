#if IOS || MACCATALYST

using Foundation;
using ObjCRuntime;
using UIKit;
using Microsoft.Maui.Handlers;

namespace LearnToShame.Handlers;

internal partial class KeyCaptureViewHandler : ViewHandler<Views.KeyCaptureView, KeyCaptureUIView>
{
    public static IPropertyMapper<Views.KeyCaptureView, KeyCaptureViewHandler> Mapper =
        new PropertyMapper<Views.KeyCaptureView, KeyCaptureViewHandler>(ViewHandler.ViewMapper);

    public KeyCaptureViewHandler() : base(Mapper) { }

    protected override KeyCaptureUIView CreatePlatformView()
    {
        var view = new KeyCaptureUIView(VirtualView);
        return view;
    }

    protected override void ConnectHandler(KeyCaptureUIView platformView)
    {
        base.ConnectHandler(platformView);
        platformView.BecomeFirstResponder();
    }
}

internal class KeyCaptureUIView : UIView
{
    private readonly WeakReference<Views.KeyCaptureView> _virtualView;

    public KeyCaptureUIView(Views.KeyCaptureView virtualView) : base()
    {
        _virtualView = new WeakReference<Views.KeyCaptureView>(virtualView);
        UserInteractionEnabled = false;
    }

    public override bool CanBecomeFirstResponder => true;

    // Один селектор keyPressed: (с параметром) — на Mac Catalyst так надёжнее срабатывают все клавиши.
    private static readonly NSString EmptyTitle = new NSString("");
    private static readonly Selector KeyPressedSelector = new Selector("keyPressed:");

    // Клавиши по символу: EN (A/D) и RU (ф/в — те же физические клавиши). Стрелки на Mac системой перехватываются.
    public override UIKeyCommand[] KeyCommands => new[]
    {
        UIKeyCommand.Create(new NSString("a"), (UIKeyModifierFlags)0, KeyPressedSelector, EmptyTitle),
        UIKeyCommand.Create(new NSString("A"), (UIKeyModifierFlags)0, KeyPressedSelector, EmptyTitle),
        UIKeyCommand.Create(new NSString("ф"), (UIKeyModifierFlags)0, KeyPressedSelector, EmptyTitle),  // RU, та же клавиша что A
        UIKeyCommand.Create(new NSString("Ф"), (UIKeyModifierFlags)0, KeyPressedSelector, EmptyTitle),
        UIKeyCommand.Create(new NSString("d"), (UIKeyModifierFlags)0, KeyPressedSelector, EmptyTitle),
        UIKeyCommand.Create(new NSString("D"), (UIKeyModifierFlags)0, KeyPressedSelector, EmptyTitle),
        UIKeyCommand.Create(new NSString("в"), (UIKeyModifierFlags)0, KeyPressedSelector, EmptyTitle),  // RU, та же клавиша что D
        UIKeyCommand.Create(new NSString("В"), (UIKeyModifierFlags)0, KeyPressedSelector, EmptyTitle),
    };

    [Export("keyPressed:")]
    private void KeyPressed(UIKeyCommand command)
    {
        if (!_virtualView.TryGetTarget(out var v)) return;
        var input = command.Input?.ToString() ?? "";
        var isNext = input.Equals("d", StringComparison.OrdinalIgnoreCase)
                     || input.Equals("в", StringComparison.OrdinalIgnoreCase);  // EN и RU (одна клавиша)
        if (isNext)
            v.InvokeNext();
        else
            v.InvokePrev();
    }

    // На Mac Catalyst стрелки часто приходят через pressesBegan, а не UIKeyCommand — перехватываем здесь.
    public override void PressesBegan(NSSet<UIPress> presses, UIPressesEvent evt)
    {
        var handled = false;
        if (presses?.Count > 0 && _virtualView.TryGetTarget(out var v))
        {
            foreach (UIPress press in presses)
            {
                var key = press.Key;
                if (key == null) continue;
                // UIKeyboardHIDUsage: KeyboardLeftArrow = 80, KeyboardRightArrow = 79
                var code = (int)key.KeyCode;
                if (code == 80) { v.InvokePrev(); handled = true; break; }   // Left
                if (code == 79) { v.InvokeNext(); handled = true; break; }  // Right
            }
        }
        if (!handled)
            base.PressesBegan(presses, evt);
    }
}

#endif
