using SQLite;
using System;

namespace casa_ceja_remake.Models
{
    [Table("credits")]
    public class Credit
    {
        [PrimaryKey, AutoIncrement]
        [Column("id")]
        public int Id { get; set; }

        [Column("folio")]
        [MaxLength(50)]
        [Indexed(Name = "IX_Credit_Folio")]
        public string Folio { get; set; } = string.Empty;

        [Column("sale_id")]
        public int SaleId { get; set; } // Referencia a la venta original

        [Column("customer_id")]
        public int CustomerId { get; set; } // ✅ Cliente que debe

        [Column("branch_id")]
        public int BranchId { get; set; }

        [Column("user_id")]
        public int UserId { get; set; } // Cajero que autorizó el crédito

        // ============ MONTOS ============
        [Column("total_amount")]
        public decimal TotalAmount { get; set; } = 0;

        [Column("paid_amount")]
        public decimal PaidAmount { get; set; } = 0;

        [Column("remaining_amount")]
        public decimal RemainingAmount { get; set; } = 0;

        // ============ CAMPOS PARA GRID ============
        [Column("customer_name")]
        [MaxLength(200)]
        public string CustomerName { get; set; } = string.Empty; // Para mostrar en grid

        [Column("status")]
        public int Status { get; set; } = 1; // 1=Activo, 2=Pagado, 3=Vencido, 4=Cancelado

        [Column("due_date")]
        public DateTime DueDate { get; set; }

        // ============ FECHAS ============
        [Column("created_at")]
        public DateTime CreatedAt { get; set; } = DateTime.Now;

        [Column("updated_at")]
        public DateTime UpdatedAt { get; set; } = DateTime.Now;

        // ============ SINCRONIZACIÓN ============
        [Column("sync_status")]
        public int SyncStatus { get; set; } = 1;

        [Column("last_sync")]
        public DateTime? LastSync { get; set; }
    }
}