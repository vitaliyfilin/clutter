namespace Clutter;

public partial class App
{
    public App()
    {
        InitializeComponent();
        
        var mainPage = new MainPage();

        var navigationPage = new NavigationPage(mainPage)
        {
            BarBackgroundColor = Color.Parse("#f1f1f1"),  
            BarTextColor = Colors.Black
        };

        MainPage = navigationPage;
    }
}