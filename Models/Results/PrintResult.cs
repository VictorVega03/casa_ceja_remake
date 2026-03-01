namespace CasaCejaRemake.Models.Results
{
    /// <summary>
    /// Resultado de una operación de impresión.
    /// Permite distinguir por qué falló sin depender solo de bool.
    /// </summary>
    public class PrintResult
    {
        public bool Success { get; set; }
        public PrintFailReason FailReason { get; set; }
        public string? ErrorMessage { get; set; }

        public static PrintResult Ok() => new() { Success = true };
        public static PrintResult Fail(PrintFailReason reason, string? message = null)
            => new() { Success = false, FailReason = reason, ErrorMessage = message };
    }

    /// <summary>
    /// Categoría del fallo para que la UI decida qué mensaje mostrar.
    /// </summary>
    public enum PrintFailReason
    {
        None = 0,
        /// <summary>No hay impresora configurada.</summary>
        NoPrinterConfigured,
        /// <summary>La impresora configurada no existe o no está disponible.</summary>
        PrinterNotFound,
        /// <summary>El texto a imprimir es nulo o vacío.</summary>
        EmptyContent,
        /// <summary>Error al escribir el archivo temporal.</summary>
        FileWriteError,
        /// <summary>Error al enviar el trabajo a la impresora.</summary>
        PrintJobError,
        /// <summary>Sistema operativo no soportado.</summary>
        UnsupportedOs
    }
}
