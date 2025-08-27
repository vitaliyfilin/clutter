using Clutter.Helpers;

namespace Clutter;

public sealed partial class MainPage
{
    private const int Length = 500;

    public MainPage()
    {
        InitializeComponent();
        NavigationPage.SetHasNavigationBar(this, false);
    }

    private async void OnGoOnlineClicked(object? sender, EventArgs e)
    {
        await FallAsync();
        await NavigationHelper.NavigateToAsync(ServiceHelper.GetService<ChatPage>());
        IsBusy = true;
    }

#pragma warning disable CA1822
    private async Task FallAsync()
#pragma warning restore CA1822
    {
        await Task.WhenAll(
            ClutterLabel.TranslateTo(0, Height, Length, Easing.CubicIn),
            OnlineButton.TranslateTo(0, Height, Length, Easing.CubicIn),
            ButtonFrame.TranslateTo(0, Height, Length, Easing.CubicIn)
        );
    }

    protected override void OnDisappearing()
    {
        base.OnDisappearing();
        IsBusy = false;
    }
}