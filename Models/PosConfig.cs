using System;

namespace CasaCejaRemake.Models
{
    /// <summary>
    /// Configuración general de la aplicación (nivel global).
    /// Se persiste como JSON en disco: app_config.json
    /// Solo Admin puede modificar estos valores.
    /// </summary>
    public class AppConfig
    {
        // ============ SUCURSAL ============
        /// <summary>ID de la sucursal seleccionada (solo Admin puede cambiar)</summary>
        public int BranchId { get; set; } = 1;

        /// <summary>Nombre de la sucursal (se actualiza desde la BD)</summary>
        public string BranchName { get; set; } = "Sucursal Principal";

        // ============ METADATA ============
        /// <summary>Fecha de última modificación</summary>
        public DateTime LastModified { get; set; } = DateTime.Now;
    }

    /// <summary>
    /// Configuración específica del terminal POS (nivel máquina).
    /// Se persiste como JSON en disco: pos_terminal_config.json
    /// Cada terminal/computadora tiene su propia configuración.
    /// </summary>
    public class PosTerminalConfig
    {
        // ============ IDENTIFICACIÓN ============
        /// <summary>Identificador único de esta terminal/caja (solo Admin puede cambiar)</summary>
        public string TerminalId { get; set; } = "CAJA-01";

        /// <summary>Nombre descriptivo de la terminal (opcional)</summary>
        public string TerminalName { get; set; } = "Terminal Principal";

        // ============ IMPRESORA ============
        /// <summary>Nombre del sistema de la impresora seleccionada para tickets</summary>
        public string PrinterName { get; set; } = string.Empty;

        /// <summary>Formato de impresión: "thermal" = ticket térmico, "letter" = hoja carta</summary>
        public string PrintFormat { get; set; } = "thermal";

        // ============ PARÁMETROS DEL TICKET ============
        /// <summary>Pie de página personalizado del ticket</summary>
        public string TicketFooter { get; set; } = "Gracias por su compra";

        /// <summary>Tamaño de letra para impresión (8, 9, 10, 11, 12)</summary>
        public int FontSize { get; set; } = 9;

        /// <summary>Familia de fuente: "Courier New", "Consolas", "Lucida Console"</summary>
        public string FontFamily { get; set; } = "Courier New";

        /// <summary>RFC del negocio (se muestra en tickets si tiene valor)</summary>
        public string Rfc { get; set; } = string.Empty;

        /// <summary>Ancho de línea en caracteres para ticket térmico (32, 40, 48)</summary>
        public int TicketLineWidth { get; set; } = 32;

        // ============ OPCIONES ============
        /// <summary>Imprimir automáticamente al finalizar venta</summary>
        public bool AutoPrint { get; set; } = true;

        /// <summary>Abrir cajón de dinero al imprimir (si está conectado)</summary>
        public bool OpenCashDrawer { get; set; } = false;

        // ============ METADATA ============
        /// <summary>Fecha de última modificación</summary>
        public DateTime LastModified { get; set; } = DateTime.Now;
    }
}
