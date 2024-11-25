using System.Globalization;

namespace Clutter.Converters;

public sealed class BoolToLayoutOptionsConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is bool isIncoming)
        {
            // If incoming, align to the left (Start). Otherwise, align to the right (End).
            return isIncoming ? LayoutOptions.Start : LayoutOptions.End;
        }

        return LayoutOptions.Start; // Default to Start if the value is not a bool
    }

    public object ConvertBack(object? value, Type targetType, object? parameter,
        CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}