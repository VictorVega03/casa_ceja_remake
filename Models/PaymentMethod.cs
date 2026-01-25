namespace CasaCejaRemake.Models
{
    /// <summary>
    /// Metodos de pago disponibles en el sistema
    /// </summary>
    public enum PaymentMethod
    {
        Efectivo = 1,
        TarjetaDebito = 2,
        TarjetaCredito = 3,
        Transferencia = 4,
        Mixto = 5
    }

    /// <summary>
    /// Extension methods para PaymentMethod
    /// </summary>
    public static class PaymentMethodExtensions
    {
        public static string GetDisplayName(this PaymentMethod method)
        {
            return method switch
            {
                PaymentMethod.Efectivo => "Efectivo",
                PaymentMethod.TarjetaDebito => "Tarjeta Debito",
                PaymentMethod.TarjetaCredito => "Tarjeta Credito",
                PaymentMethod.Transferencia => "Transferencia",
                PaymentMethod.Mixto => "Mixto",
                _ => method.ToString()
            };
        }

        public static string GetShortName(this PaymentMethod method)
        {
            return method switch
            {
                PaymentMethod.Efectivo => "EF",
                PaymentMethod.TarjetaDebito => "TD",
                PaymentMethod.TarjetaCredito => "TC",
                PaymentMethod.Transferencia => "TR",
                PaymentMethod.Mixto => "MX",
                _ => "??"
            };
        }
    }
}
