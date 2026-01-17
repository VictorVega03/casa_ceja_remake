using SQLite;
using System;

namespace casa_ceja_remake.Models
{
    [Table("layaways")]
    public class Layaway
    {
        [PrimaryKey, AutoIncrement]
        [Column("id")]
        public int Id { get; set; }

        [Column("folio")]
        [MaxLength(50)]
        [Indexed(Name = "IX_Layaway_Folio")]
        public string Folio { get; set; } = string.Empty;

        [Column("customer_id")]
        public int CustomerId { get; set; } // ✅ Cliente del apartado

        [Column("branch_id")]
        public int BranchId { get; set; }

        [Column("user_id")]
        public int UserId { get; set; } // Cajero que hizo el apartado

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
        public int Status { get; set; } = 1; // 1=Activo, 2=Completado, 3=Cancelado

        [Column("delivery_date")]
        public DateTime DeliveryDate { get; set; }

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