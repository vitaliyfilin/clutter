using Clutter.Services;

namespace Clutter;

public partial class MainPage : ContentPage
{
    public MainPage()
    {
        InitializeComponent();
        NavigationPage.SetHasNavigationBar(this, false);
    }

    private async void OnGoOnlineClicked(object? sender, EventArgs e)
    {
        await NavigationService.NavigateToAsync(ServiceHelper.GetService<ChatPage>());
    }
}