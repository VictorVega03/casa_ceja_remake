using System;
using System.Globalization;
using Avalonia.Data.Converters;
using Avalonia.Media;

namespace CasaCejaRemake.Converters
{
    /// <summary>
    /// Convierte el PriceType de un CartItem a un color de fondo.
    /// </summary>
    public class PriceTypeToBackgroundConverter : IValueConverter
    {
        public static readonly PriceTypeToBackgroundConverter Instance = new();

        public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (value is string priceType)
            {
                return priceType switch
                {
                    "category" => new SolidColorBrush(Color.Parse("#2E7D32")),  // Verde oscuro
                    "special" => new SolidColorBrush(Color.Parse("#F9A825")),   // Amarillo oscuro
                    "dealer" => new SolidColorBrush(Color.Parse("#1565C0")),    // Azul oscuro
                    _ => null  // Usa el estilo por defecto
                };
            }
            return null;
        }

        public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
