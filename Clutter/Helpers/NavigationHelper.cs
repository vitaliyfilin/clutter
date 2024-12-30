namespace Clutter.Helpers;

public static class NavigationHelper
{
    public static async Task NavigateToAsync(Page? page)
    {
        if (Application.Current?.MainPage is NavigationPage navPage)
        {
            await navPage.PushAsync(page);
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