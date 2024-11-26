using System.Globalization;
using Microsoft.Maui.Graphics.Converters;

namespace Clutter.Converters;

public class BoolToTextColorConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        // if (value is bool isSystemMessage)
        // {
        //     // Return different colors based on the value of the boolean
        //     return isSystemMessage ? "LightGray" : "Black";
        // }
        //
        // // Default color if the input is not a boolean
        var converter = new ColorTypeConverter();
        var color = (Color)(converter.ConvertFromInvariantString("Black")!);
        return color;
        return "Black";
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}