using SQLite;
using System;

namespace CasaCejaRemake.Models
{
    [Table("sales")]
    public class Sale
    {
        [PrimaryKey, AutoIncrement]
        [Column("id")]
        public int Id { get; set; }

        [Column("folio")]
        [MaxLength(50)]
        [Indexed(Name = "IX_Sale_Folio")]
        public string Folio { get; set; } = string.Empty;

        [Column("branch_id")]
        public int BranchId { get; set; }

        [Column("user_id")]
        public int UserId { get; set; } // Usuario que hizo la venta (cajero)

        // ============ CAMPOS SQL (para queries rápidas) ============
        [Column("subtotal")]
        public decimal Subtotal { get; set; } = 0;

        [Column("discount")]
        public decimal Discount { get; set; } = 0;

        [Column("total")]
        public decimal Total { get; set; } = 0;

        [Column("amount_paid")]
        public decimal AmountPaid { get; set; } = 0;

        [Column("change_given")]
        public decimal ChangeGiven { get; set; } = 0;

        [Column("payment_method")]
        public string PaymentMethod { get; set; } = "{}"; // JSON: {"cash": 500, "card": 100}

        [Column("payment_summary")]
        [MaxLength(100)]
        public string PaymentSummary { get; set; } = string.Empty; // "Efectivo $500" - Para mostrar en grid

        // ============ JSON COMPRIMIDO (auditoría completa) ============
        [Column("ticket_data")]
        public byte[] TicketData { get; set; } = Array.Empty<byte>(); // Ticket completo comprimido

        // ============ FECHAS ============
        [Column("sale_date")]
        [Indexed(Name = "IX_Sale_Date")]
        public DateTime SaleDate { get; set; } = DateTime.Now;

        [Column("created_at")]
        public DateTime CreatedAt { get; set; } = DateTime.Now;

        // ============ FOLIO DEL CORTE ============
        [Column("cash_close_folio")]
        [MaxLength(50)]
        [Indexed(Name = "IX_Sale_CashCloseFolio")]
        public string CashCloseFolio { get; set; } = string.Empty;

        // ============ SINCRONIZACIÓN ============
        [Column("sync_status")]
        public int SyncStatus { get; set; } = 1;

        [Column("last_sync")]
        public DateTime? LastSync { get; set; }
    }
}