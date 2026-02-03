using SQLite;
using System;

namespace CasaCejaRemake.Models
{
    [Table("cash_closes")]
    public class CashClose
    {
        [PrimaryKey, AutoIncrement]
        [Column("id")]
        public int Id { get; set; }

        [Column("folio")]
        [MaxLength(50)]
        [Indexed(Name = "IX_CashClose_Folio")]
        public string Folio { get; set; } = string.Empty;

        [Column("branch_id")]
        public int BranchId { get; set; }

        [Column("user_id")]
        public int UserId { get; set; }

        // ============ FONDO DE APERTURA ============
        [Column("opening_cash")]
        public decimal OpeningCash { get; set; } = 0;

        // ============ TOTALES POR MÉTODO DE PAGO ============
        [Column("total_cash")]
        public decimal TotalCash { get; set; } = 0;

        [Column("total_debit_card")]
        public decimal TotalDebitCard { get; set; } = 0;

        [Column("total_credit_card")]
        public decimal TotalCreditCard { get; set; } = 0;

        [Column("total_checks")]
        public decimal TotalChecks { get; set; } = 0;

        [Column("total_transfers")]
        public decimal TotalTransfers { get; set; } = 0;

        // ============ EFECTIVO DE APARTADOS Y CRÉDITOS ============
        [Column("layaway_cash")]
        public decimal LayawayCash { get; set; } = 0;

        [Column("credit_cash")]
        public decimal CreditCash { get; set; } = 0;

        // ============ TOTALES DE CRÉDITOS Y APARTADOS CREADOS ============
        /// <summary>
        /// Total de TODOS los créditos creados durante el turno (valor completo, no solo abonos).
        /// Esto suma al "Total del Corte" para medir productividad.
        /// </summary>
        [Column("credit_total_created")]
        public decimal CreditTotalCreated { get; set; } = 0;

        /// <summary>
        /// Total de TODOS los apartados creados durante el turno (valor completo, no solo abonos).
        /// Esto suma al "Total del Corte" para medir productividad.
        /// </summary>
        [Column("layaway_total_created")]
        public decimal LayawayTotalCreated { get; set; } = 0;

        // ============ GASTOS E INGRESOS (JSON TEXT) ============
        [Column("expenses")]
        public string Expenses { get; set; } = "[]"; // JSON: [{"description": "Comida", "amount": 100}]

        [Column("income")]
        public string Income { get; set; } = "[]"; // JSON: [{"description": "Venta externa", "amount": 500}]

        // ============ SOBRANTE/FALTANTE ============
        [Column("surplus")]
        public decimal Surplus { get; set; } = 0; // Positivo = sobrante, Negativo = faltante

        // ============ TOTALES ============
        [Column("expected_cash")]
        public decimal ExpectedCash { get; set; } = 0;

        [Column("total_sales")]
        public decimal TotalSales { get; set; } = 0;

        // ============ NOTAS ============
        [Column("notes")]
        public string Notes { get; set; } = string.Empty;

        // ============ FECHAS ============
        [Column("opening_date")]
        public DateTime OpeningDate { get; set; } = DateTime.Now;

        [Column("close_date")]
        [Indexed(Name = "IX_CashClose_CloseDate")]
        public DateTime CloseDate { get; set; } = DateTime.Now;

        [Column("created_at")]
        public DateTime CreatedAt { get; set; } = DateTime.Now;

        [Column("updated_at")]
        public DateTime UpdatedAt { get; set; } = DateTime.Now;

        // ============ SINCRONIZACIÓN ============
        [Column("sync_status")]
        public int SyncStatus { get; set; } = 1;

        [Column("last_sync")]
        public DateTime? LastSync { get; set; }

        // ========== PROPIEDADES CALCULADAS ==========
        
        /// <summary>
        /// Total del Corte: Suma de TODAS las ventas del turno (mide productividad).
        /// Incluye ventas directas + créditos creados + apartados creados.
        /// </summary>
        [Ignore]
        public decimal TotalDelCorte => TotalCash + TotalDebitCard + TotalCreditCard + 
                                        TotalChecks + TotalTransfers + 
                                        CreditTotalCreated + LayawayTotalCreated;

        /// <summary>
        /// Total de todos los métodos de pago de ventas directas (sin créditos/apartados).
        /// </summary>
        [Ignore]
        public decimal TotalPayments => TotalCash + TotalDebitCard + TotalCreditCard + 
                                        TotalChecks + TotalTransfers;

        /// <summary>
        /// Efectivo Total que pasó por la caja (para cálculos de arqueo).
        /// = Fondo + Efectivo Directo + Efectivo Abonos + Ingresos - Gastos
        /// </summary>
        [Ignore]
        public decimal EfectivoTotal => OpeningCash + TotalCash + LayawayCash + CreditCash;

        /// <summary>
        /// Indica si hay sobrante de efectivo.
        /// </summary>
        [Ignore]
        public bool HasSurplus => Surplus > 0;

        /// <summary>
        /// Indica si hay faltante de efectivo.
        /// </summary>
        [Ignore]
        public bool HasShortage => Surplus < 0;

        /// <summary>
        /// Indica si el corte está balanceado (sin sobrante ni faltante).
        /// </summary>
        [Ignore]
        public bool IsBalanced => Surplus == 0;

        /// <summary>
        /// Duración del turno en horas.
        /// </summary>
        [Ignore]
        public double ShiftDurationHours => (CloseDate - OpeningDate).TotalHours;

        /// <summary>
        /// Total de ventas por métodos electrónicos.
        /// </summary>
        [Ignore]
        public decimal TotalElectronicPayments => TotalDebitCard + TotalCreditCard + 
                                                   TotalChecks + TotalTransfers;
    }
}