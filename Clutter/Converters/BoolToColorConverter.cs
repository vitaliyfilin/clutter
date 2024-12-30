using System.Globalization;

namespace Clutter.Converters;

public sealed class BoolToColorConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is bool isIncoming)
        {
            return isIncoming ? Color.Parse("#343145") : Color.Parse("#1B55FC");
        }

        return Colors.Transparent;
    }

    public object ConvertBack(object? value, Type targetType, object? parameter,
        CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}