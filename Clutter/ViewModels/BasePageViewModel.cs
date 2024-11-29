using Clutter.Helpers;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace Clutter.ViewModels;

public partial class BasePageViewModel : ObservableObject
{
    [ICommand]
    private static async void GoBack(string message)
    {
        await NavigationHelper.NavigateBackAsync();
    }
}