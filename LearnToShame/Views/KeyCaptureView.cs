using System.Windows.Input;

namespace LearnToShame.Views;

/// <summary>Невидимый контрол для перехвата клавиш A, D и стрелок влево/вправо (для сессии на Mac).</summary>
public class KeyCaptureView : View
{
    public static readonly BindableProperty PrevCommandProperty = BindableProperty.Create(
        nameof(PrevCommand), typeof(ICommand), typeof(KeyCaptureView), null);
    public static readonly BindableProperty NextCommandProperty = BindableProperty.Create(
        nameof(NextCommand), typeof(ICommand), typeof(KeyCaptureView), null);

    public ICommand? PrevCommand
    {
        get => (ICommand?)GetValue(PrevCommandProperty);
        set => SetValue(PrevCommandProperty, value);
    }
    public ICommand? NextCommand
    {
        get => (ICommand?)GetValue(NextCommandProperty);
        set => SetValue(NextCommandProperty, value);
    }

    internal void InvokePrev() => PrevCommand?.Execute(null);
    internal void InvokeNext() => NextCommand?.Execute(null);
}
