using System;
using System.Globalization;
using Avalonia.Data.Converters;

namespace CasaCejaRemake.Helpers
{
    /// <summary>
    /// Convierte bool a opacidad (1.0 para true, 0.3 para false)
    /// </summary>
    public class BoolToOpacityConverter : IValueConverter
    {
        public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (value is bool boolValue)
            {
                return boolValue ? 1.0 : 0.3;
            }
            return 1.0;
        }

        public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
