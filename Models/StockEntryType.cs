namespace CasaCejaRemake.Models
{
    /// <summary>
    /// Tipos de entrada de stock. Determina el origen y las reglas de negocio aplicables.
    /// </summary>
    public static class StockEntryType
    {
        /// <summary>
        /// Compra a proveedor o ingreso manual.
        /// Se registra localmente y funciona offline. Siempre tiene SupplierId.
        /// </summary>
        public const string Purchase = "PURCHASE";

        /// <summary>
        /// Traspaso recibido automáticamente al procesar una salida en otra sucursal.
        /// Solo puede venir del servidor — nunca se crea manualmente.
        /// Requiere conexión para confirmar.
        /// </summary>
        public const string Transfer = "TRANSFER";
    }
}
