using SQLite;
using System;

namespace CasaCejaRemake.Models
{
    [Table("credit_payments")]
    public class CreditPayment
    {
        // ========== IDENTIFICADOR ==========
        [PrimaryKey, AutoIncrement]
        [Column("id")]
        public int Id { get; set; }

        // ========== FOLIO ==========
        [Column("folio")]
        [MaxLength(255)]
        [Indexed(Name = "IX_CreditPayments_Folio", Unique = true)]
        public string Folio { get; set; } = string.Empty;

        // ========== RELACIONES ==========
        [Column("credit_id")]
        [Indexed(Name = "IX_CreditPayments_Credit")]
        public int CreditId { get; set; }

        [Column("user_id")]
        [Indexed(Name = "IX_CreditPayments_User")]
        public int UserId { get; set; }

        // ========== INFORMACIÓN DEL ABONO ==========
        [Column("amount_paid")]
        public decimal AmountPaid { get; set; }

        // ========== MÉTODO DE PAGO (JSON) ==========
        [Column("payment_method")]
        [MaxLength(500)]
        public string PaymentMethod { get; set; } = string.Empty;

        [Column("payment_date")]
        [Indexed(Name = "IX_CreditPayments_PaymentDate")]
        public DateTime PaymentDate { get; set; } = DateTime.Now;

        // ========== FOLIO DEL CORTE ==========
        [Column("cash_close_folio")]
        [MaxLength(255)]
        [Indexed(Name = "IX_CreditPayments_CashCloseFolio")]
        public string CashCloseFolio { get; set; } = string.Empty;

        // ========== OBSERVACIONES ==========
        [Column("notes")]
        [MaxLength(300)]
        public string? Notes { get; set; }

        // ========== TIMESTAMPS ==========
        [Column("created_at")]
        public DateTime CreatedAt { get; set; } = DateTime.Now;

        [Column("updated_at")]
        public DateTime UpdatedAt { get; set; } = DateTime.Now;

        // ========== SINCRONIZACIÓN ==========
        [Column("sync_status")]
        public int SyncStatus { get; set; } = 1;

        [Column("last_sync")]
        public DateTime? LastSync { get; set; }
    }
}