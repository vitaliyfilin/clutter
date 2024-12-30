using System.Globalization;

namespace Clutter.Converters;

public sealed class BoolToLayoutOptionsConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is bool isIncoming)
        {
            return isIncoming ? LayoutOptions.Start : LayoutOptions.End;
        }

        return LayoutOptions.Center;
    }

    public object ConvertBack(object? value, Type targetType, object? parameter,
        CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}