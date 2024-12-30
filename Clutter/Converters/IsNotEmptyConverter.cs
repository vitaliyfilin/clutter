using System.Globalization;

namespace Clutter.Converters;

public sealed class IsNotEmptyConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        var text = value as string;
        return !string.IsNullOrWhiteSpace(text);
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}