using System;
using System.Globalization;
using Avalonia.Data.Converters;
using Avalonia.Media;

namespace CasaCejaRemake.Helpers
{
    /// <summary>
    /// Convierte un decimal a un SolidColorBrush según si es negativo o no.
    /// Negativo → rojo (#EF5350), cero o positivo → blanco.
    /// </summary>
    public class DecimalToColorConverter : IValueConverter
    {
        public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (value is decimal d)
                return d < 0
                    ? new SolidColorBrush(Color.Parse("#EF5350"))
                    : new SolidColorBrush(Colors.White);

            return new SolidColorBrush(Colors.White);
        }

        public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
            => throw new NotImplementedException();
    }
}
