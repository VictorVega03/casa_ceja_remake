namespace CasaCejaRemake.Models.Results
{
    /// <summary>
    /// Resultado de una operación de impresión.
    /// Permite distinguir por qué falló sin depender solo de bool.
    /// </summary>
    public class PrintResult
    {
        public bool Success { get; private set; }
        public string? ErrorMessage { get; private set; }
        public PrintFailReason FailReason { get; private set; }

        public static PrintResult Ok() =>
            new() { Success = true };

        public static PrintResult Fail(PrintFailReason reason, string message) =>
            new() { Success = false, FailReason = reason, ErrorMessage = message };
    }

    /// <summary>
    /// Categoría del fallo para que la UI decida qué mensaje mostrar.
    /// </summary>
    public enum PrintFailReason
    {
        None = 0,
        /// <summary>No hay impresora guardada en la configuración del terminal.</summary>
        NoPrinterConfigured,
        /// <summary>La impresión automática está desactivada por el usuario.</summary>
        AutoPrintDisabled,
        /// <summary>El formato configurado no es térmico (carta u otro).</summary>
        FormatNotThermal,
        /// <summary>Error al comunicarse con el driver/spooler.</summary>
        DriverError,
        /// <summary>Error de I/O (archivo temporal, permisos, etc.).</summary>
        IoError,
        /// <summary>Sistema operativo no soportado.</summary>
        UnsupportedOs
    }
}
