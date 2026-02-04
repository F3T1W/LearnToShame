namespace LearnToShame.Helpers;

public class ButtonPressAnimationBehavior : Behavior<Button>
{
    protected override void OnAttachedTo(Button button)
    {
        base.OnAttachedTo(button);
        button.Clicked += OnClicked;
    }

    protected override void OnDetachingFrom(Button button)
    {
        button.Clicked -= OnClicked;
        base.OnDetachingFrom(button);
    }

    private async void OnClicked(object? sender, EventArgs e)
    {
        if (sender is not View view) return;
        await view.FadeToAsync(0.65, 50, Easing.CubicOut);
        await view.FadeToAsync(1, 80, Easing.CubicOut);
    }
}
