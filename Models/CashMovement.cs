using SQLite;
using System;

namespace CasaCejaRemake.Models
{
    /// <summary>
    /// Representa un movimiento de efectivo (gasto o ingreso) durante un turno de caja.
    /// </summary>
    [Table("cash_movements")]
    public class CashMovement
    {
        [PrimaryKey, AutoIncrement]
        [Column("id")]
        public int Id { get; set; }

        /// <summary>
        /// ID del corte de caja al que pertenece este movimiento.
        /// </summary>
        [Column("cash_close_id")]
        [Indexed(Name = "IX_CashMovement_CashCloseId")]
        public int CashCloseId { get; set; }

        /// <summary>
        /// Tipo de movimiento: "expense" (gasto) o "income" (ingreso).
        /// </summary>
        [Column("type")]
        [MaxLength(20)]
        public string Type { get; set; } = "expense";

        /// <summary>
        /// Concepto o descripción del movimiento.
        /// </summary>
        [Column("concept")]
        [MaxLength(200)]
        public string Concept { get; set; } = string.Empty;

        /// <summary>
        /// Monto del movimiento.
        /// </summary>
        [Column("amount")]
        public decimal Amount { get; set; }

        /// <summary>
        /// ID del usuario que registró el movimiento.
        /// </summary>
        [Column("user_id")]
        public int UserId { get; set; }

        /// <summary>
        /// Fecha y hora de creación.
        /// </summary>
        [Column("created_at")]
        public DateTime CreatedAt { get; set; } = DateTime.Now;

        // ========== PROPIEDADES CALCULADAS ==========

        /// <summary>
        /// Indica si es un gasto.
        /// </summary>
        [Ignore]
        public bool IsExpense => Type == "expense";

        /// <summary>
        /// Indica si es un ingreso.
        /// </summary>
        [Ignore]
        public bool IsIncome => Type == "income";

        /// <summary>
        /// Texto del tipo para mostrar en UI.
        /// </summary>
        [Ignore]
        public string TypeDisplay => IsExpense ? "Gasto" : "Ingreso";
    }
}
