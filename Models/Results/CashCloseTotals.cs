namespace CasaCejaRemake.Models.Results
{
    /// <summary>
    /// Totales calculados para el corte de caja.
    /// Basado en las reglas de negocio documentadas en reglas_corte.md
    /// </summary>
    public class CashCloseTotals
    {
        // ==================== VENTAS DIRECTAS POR MÉTODO DE PAGO ====================
        /// <summary>Ventas directas pagadas en efectivo (Total de venta, no AmountPaid)</summary>
        public decimal TotalCash { get; set; }
        /// <summary>Ventas directas pagadas con tarjeta débito</summary>
        public decimal TotalDebitCard { get; set; }
        /// <summary>Ventas directas pagadas con tarjeta crédito</summary>
        public decimal TotalCreditCard { get; set; }
        /// <summary>Ventas directas pagadas con transferencia</summary>
        public decimal TotalTransfers { get; set; }
        /// <summary>Ventas directas pagadas con cheque</summary>
        public decimal TotalChecks { get; set; }
        /// <summary>Total de ventas directas (todos los métodos)</summary>
        public decimal TotalSales { get; set; }
        /// <summary>Número de ventas directas</summary>
        public int SalesCount { get; set; }

        // ==================== CRÉDITOS ====================
        /// <summary>Total de TODOS los créditos CREADOS en el turno (valor completo)</summary>
        public decimal CreditTotalCreated { get; set; }
        /// <summary>Efectivo recibido por abonos/enganches de créditos</summary>
        public decimal CreditCash { get; set; }
        /// <summary>Número de créditos creados</summary>
        public int CreditCount { get; set; }

        // ==================== APARTADOS ====================
        /// <summary>Total de TODOS los apartados CREADOS en el turno (valor completo)</summary>
        public decimal LayawayTotalCreated { get; set; }
        /// <summary>Efectivo recibido por abonos de apartados</summary>
        public decimal LayawayCash { get; set; }
        /// <summary>Número de apartados creados</summary>
        public int LayawayCount { get; set; }

        // ==================== MOVIMIENTOS DE CAJA ====================
        /// <summary>Total de gastos registrados (sale de caja)</summary>
        public decimal TotalExpenses { get; set; }
        /// <summary>Total de ingresos extra registrados (entra a caja)</summary>
        public decimal TotalIncome { get; set; }

        // ==================== PROPIEDADES CALCULADAS ====================
        /// <summary>TOTAL DEL CORTE = Ventas Directas + Créditos Creados + Apartados Creados</summary>
        public decimal TotalDelCorte => TotalSales + CreditTotalCreated + LayawayTotalCreated;

        /// <summary>
        /// EFECTIVO ESPERADO = Fondo + Efectivo Ventas + Efectivo Abonos + Ingresos - Gastos
        /// </summary>
        public decimal CalcularEfectivoEsperado(decimal fondoApertura)
        {
            return fondoApertura + TotalCash + CreditCash + LayawayCash + TotalIncome - TotalExpenses;
        }
    }
}
