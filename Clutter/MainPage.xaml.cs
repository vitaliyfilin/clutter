using Clutter.Helpers;
using Clutter.Services;

namespace Clutter;

public partial class MainPage : ContentPage
{
    private uint _animationDuration = 7000; // Animation duration for the opacity effect

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

    private async Task FallAsync()
    {
        await Task.WhenAll(
            ClutterLabel.TranslateTo(0, Height, 500, Easing.CubicIn),
            OnlineButton.TranslateTo(0, Height, 500, Easing.CubicIn),
            ButtonFrame.TranslateTo(0, Height, 500, Easing.CubicIn)
        );
    }

    private async Task ExplodeAsync()
    {
         var buttonBounds = OnlineButton.Bounds;
        
        var pieces = new List<Label>();
        const int numberOfPieces = 100; // Increased the number of pieces for a more dramatic effect
        
        for (var i = 0; i < numberOfPieces; i++)
        {
            var piece = new Label
            {
                Text = "*",
                TextColor = Colors.Red,
                BackgroundColor = Colors.Transparent,
                WidthRequest = 20,
                HeightRequest = 20,
                Opacity = 0,
                TranslationX = buttonBounds.X + (buttonBounds.Width / 2) - 10, // Positioning the pieces at the center of the button
                TranslationY = buttonBounds.Y + (buttonBounds.Height / 2) - 10,
                HorizontalTextAlignment = TextAlignment.Center,
                VerticalTextAlignment = TextAlignment.Center,
            };
            pieces.Add(piece);
            ExplosionGrid.Children.Add(piece); 
        }
        
        var random = new Random();
        var tasks = new List<Task>();
        
        foreach (var piece in pieces)
        {
            var xTranslation = random.Next(-200, 200); // Randomize explosion direction horizontally
            var yTranslation = random.Next(-200, 200); // Randomize explosion direction vertically
            var scale = random.NextDouble() * 1.5 + 0.5; // Random scale between 0.5 and 2
        
            tasks.Add(Task.Run(async () =>
            {
                await piece.FadeTo(1, 100); // Fade in quickly for the explosion effect
                await piece.TranslateTo(piece.TranslationX + xTranslation, piece.TranslationY + yTranslation, 100, Easing.CubicIn); // Move the piece in a random direction
                await piece.ScaleTo(scale, 500, Easing.CubicInOut); // Scale the piece to simulate growth
                await piece.FadeTo(0, 500); // Fade out after explosion
                await Task.Yield();
            }));
        }
        
        await Task.WhenAll(tasks);
        
        foreach (var piece in pieces)
        {
            ExplosionGrid.Children.Remove(piece);
        }

    }

    protected override void OnDisappearing()
    {
        base.OnDisappearing();
        IsBusy = false;
    }

    private async void HandleAnimation()
    {
        await Task.Delay(3000);

        var radialGradient = new RadialGradientBrush
        {
            Center = new Point(0.5, 0.5), // Center of the gradient
            Radius = 1.5, // Adjust the radius as needed
        };

        radialGradient.GradientStops.Add(new GradientStop(Color.Parse("#FF6600"), 0f)); // Color at the start
        radialGradient.GradientStops.Add(new GradientStop(Color.Parse("#32CD32"), 0.2f)); // Color in the middle
        radialGradient.GradientStops.Add(new GradientStop(Color.Parse("#FF4500"), 0.3f)); // Color at the end
        radialGradient.GradientStops.Add(new GradientStop(Color.Parse("#DCFF3D"), 0.4f)); // Color at the end
        radialGradient.GradientStops.Add(new GradientStop(Color.Parse("#FD4EDE"), 0.5f)); // Color at the end
        radialGradient.GradientStops.Add(new GradientStop(Color.Parse("#FF00FF"), 0.6f)); // Color at the end
        radialGradient.GradientStops.Add(new GradientStop(Color.Parse("#FF4500"), 0.7f)); // Color at the end
        radialGradient.GradientStops.Add(new GradientStop(Color.Parse("#32CD32"), 0.8f)); // Color at the end
        radialGradient.GradientStops.Add(new GradientStop(Color.Parse("#FF6600"), 1.0f)); // Color at the end

        OnlineButton.Background = radialGradient;

        var fadeAnimation = new Animation(v => OnlineButton.Opacity = v, 0.7, 1.0);
        fadeAnimation.Commit(this, "FadeAnimation", 16, _animationDuration, Easing.Linear, (v, c) => { }, () => true);

        await Task.Yield();
    }

    private void OnButtonClicked()
    {
        // Brightness effect: animate the opacity from 1.0 to 1.5 and back
        var brightnessAnimation = new Animation(v => OnlineButton.Opacity = v, 1.0, 1.5, Easing.CubicInOut);
        brightnessAnimation.Commit(this, "BrightnessAnimation", 16, 500, Easing.Linear,
            (v, c) => OnlineButton.Opacity = 1.0, () => true);
    }
}