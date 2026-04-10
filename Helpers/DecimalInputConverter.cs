using Avalonia.Data.Converters;
using System;
using System.Globalization;

namespace CasaCejaRemake.Helpers
{
    public class DecimalInputConverter : IValueConverter
    {
        public static readonly DecimalInputConverter Instance = new();

        public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (value is decimal d)
                return d.ToString("F2", culture);
            if (value is string s && decimal.TryParse(s, out var parsed))
                return parsed.ToString("F2", culture);
            return "0.00";
        }

        public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (value is string s)
            {
                s = s.Replace(",", ".").Trim();
                if (decimal.TryParse(s, NumberStyles.Any, CultureInfo.InvariantCulture, out var result))
                    return Math.Round(result, 2);
                return 0m;
            }
            return 0m;
        }
    }
}
