using System;

namespace CasaCejaRemake.Models
{
    /// Configuración general de la aplicación (nivel global).
    /// Se persiste como JSON en disco: app_config.json
    /// Solo Admin puede modificar estos valores.
    public class AppConfig
    {
        // ============ SERVIDOR ============
        /// URL base del servidor Laravel
        public string ServerUrl { get; set; } = "https://cm-papeleria.com";

        /// Token de autenticación del usuario actual.
        /// Se genera automáticamente al hacer login por primera vez.
        /// Null hasta que el usuario haga login.
        public string? UserToken { get; set; } = null;

        /// Timestamp Unix del último sync exitoso.
        /// Se actualiza con el server_time del servidor, no con el reloj local.
        /// 0 = nunca se ha sincronizado.
        public long LastSyncTimestamp { get; set; } = 0;

        // ============ SUCURSAL ============
        /// ID de la sucursal seleccionada.
        /// Null hasta que el admin configure una sucursal después del primer login.
        public int? CurrentBranchId { get; set; } = 1;

        /// Nombre de la sucursal actual
        public string? CurrentBranchName { get; set; } = "Casa Ceja Carranza";

        // ============ METADATA ============
        /// Fecha de última modificación
        public DateTime LastModified { get; set; } = DateTime.Now;
    }

    /// Configuración específica del terminal POS (nivel máquina).
    /// Se persiste como JSON en disco: pos_terminal_config.json
    /// Cada terminal/computadora tiene su propia configuración.
    public class PosTerminalConfig
    {
        // ============ IDENTIFICACIÓN ============
        /// Identificador único de esta terminal/caja (solo Admin puede cambiar)
        public string TerminalId { get; set; } = "CAJA-01";

        /// Nombre descriptivo de la terminal (opcional)
        public string TerminalName { get; set; } = "Terminal Principal";

        // ============ IMPRESORA ============
        /// Nombre del sistema de la impresora seleccionada para tickets
        public string PrinterName { get; set; } = string.Empty;

        /// Formato de impresión: "thermal" = ticket térmico, "letter" = hoja carta
        public string PrintFormat { get; set; } = "thermal";

        // ============ PARÁMETROS DEL TICKET ============
        /// Pie de página personalizado del ticket
        public string TicketFooter { get; set; } = "Gracias por su compra";

        /// Tamaño de letra para impresión (8, 9, 10, 11, 12)
        public int FontSize { get; set; } = 9;

        /// Familia de fuente: "Courier New", "Consolas", "Lucida Console"
        public string FontFamily { get; set; } = "Courier New";

        /// RFC del negocio (se muestra en tickets si tiene valor)
        public string Rfc { get; set; } = string.Empty;

        /// Ancho de línea en caracteres para ticket térmico (32, 40, 48)
        public int TicketLineWidth { get; set; } = 32;

        // ============ OPCIONES ============
        /// Imprimir automáticamente al finalizar venta
        public bool AutoPrint { get; set; } = true;

        /// Abrir cajón de dinero al imprimir (si está conectado)
        public bool OpenCashDrawer { get; set; } = false;

        // ============ METADATA ============
        /// Fecha de última modificación
        public DateTime LastModified { get; set; } = DateTime.Now;
    }
}