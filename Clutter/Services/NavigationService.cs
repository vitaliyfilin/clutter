using CommunityToolkit.Maui.Views;

namespace Clutter.Services;

public static class NavigationService
{
    public static async Task NavigateToAsync(Page? page)
    {
        if (Application.Current?.MainPage is NavigationPage navPage)
        {
            await navPage.PushAsync(page);
        }
    }

    public static async Task ShowPopupAsync(Popup popup)
    {
        if (Application.Current?.MainPage is NavigationPage navPage)
        {
            await navPage.ShowPopupAsync(popup);
        }
    }

    public static async Task NavigateBackAsync()
    {
        if (Application.Current?.MainPage is NavigationPage navPage)
        {
            await navPage.PopAsync();
        }
    }
}