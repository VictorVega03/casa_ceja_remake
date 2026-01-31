using System;
using System.Globalization;
using Avalonia.Data.Converters;
using Avalonia.Media;

namespace CasaCejaRemake.Helpers
{
    /// <summary>
    /// Converts bool to SolidColorBrush
    /// </summary>
    public class BoolToColorConverter : IValueConverter
    {
        public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (value is bool boolValue && parameter is string colorPair)
            {
                var colors = colorPair.Split('|');
                if (colors.Length == 2)
                {
                    var selectedColor = colors[0];
                    var deselectedColor = colors[1];
                    return boolValue 
                        ? SolidColorBrush.Parse(selectedColor) 
                        : SolidColorBrush.Parse(deselectedColor);
                }
            }
            return new SolidColorBrush(Colors.Transparent);
        }

        public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
