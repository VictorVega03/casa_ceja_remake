namespace CasaCejaRemake.Helpers
{
    /// <summary>
    /// Constantes globales de la aplicación.
    /// </summary>
    public static class Constants
    {
        // ============ CARPETAS ============
        /// <summary>Nombre de la carpeta raíz de datos de la aplicación</summary>
        public const string APP_DATA_FOLDER = "CasaCeja";

        /// <summary>Nombre del archivo de base de datos</summary>
        public const string DB_FILE_NAME = "casaceja.db";

        /// <summary>Nombre del archivo de configuración local</summary>
        public const string CONFIG_FILE_NAME = "pos_config.json";

        /// <summary>Carpeta raíz de documentos exportados</summary>
        public const string DOCS_ROOT_FOLDER = "CasaCejaDocs";

        // ============ ROLES (KEYS) ============
        /// <summary>Clave del rol de administrador</summary>
        public const string ROLE_ADMIN_KEY = "admin";

        /// <summary>Clave del rol de cajero</summary>
        public const string ROLE_CASHIER_KEY = "cashier";

        // ============ DEFAULTS DE IMPRESIÓN ============
        /// <summary>Ancho de línea por defecto para tickets térmicos</summary>
        public const int DEFAULT_TICKET_LINE_WIDTH = 40;

        /// <summary>Tamaño de fuente por defecto</summary>
        public const int DEFAULT_FONT_SIZE = 9;

        /// <summary>Familia de fuente por defecto</summary>
        public const string DEFAULT_FONT_FAMILY = "Courier New";

        /// <summary>Formato de impresión por defecto: thermal o letter</summary>
        public const string DEFAULT_PRINT_FORMAT = "thermal";

        /// <summary>Pie de ticket por defecto</summary>
        public const string DEFAULT_TICKET_FOOTER = "Gracias por su compra";

        // ============ DEFAULTS DE CAJA ============
        /// <summary>ID de caja por defecto</summary>
        public const string DEFAULT_CASH_REGISTER_ID = "CAJA-01";

        // ============ VERSIÓN ============
        /// <summary>Versión actual de la aplicación</summary>
        public const string APP_VERSION = "1.0.0";
    }
}
