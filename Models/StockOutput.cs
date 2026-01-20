using SQLite;
using System;

namespace CasaCejaRemake.Models
{
    [Table("stock_outputs")]
    public class StockOutput
    {
        // ========== IDENTIFICADOR ==========
        [PrimaryKey, AutoIncrement]
        [Column("id")]
        public int Id { get; set; }

        // ========== FOLIO ==========
        [Column("folio")]
        [MaxLength(50)]
        [Indexed(Name = "IX_StockOutputs_Folio", Unique = true)]
        public string Folio { get; set; } = string.Empty;

        // ========== RELACIONES DE SUCURSALES ==========
        [Column("origin_branch_id")]
        [Indexed(Name = "IX_StockOutputs_Origin")]
        public int OriginBranchId { get; set; }

        [Column("destination_branch_id")]
        [Indexed(Name = "IX_StockOutputs_Destination")]
        public int DestinationBranchId { get; set; }

        // ========== USUARIO ==========
        [Column("user_id")]
        [Indexed(Name = "IX_StockOutputs_User")]
        public int UserId { get; set; }

        // ========== TOTAL DEL TRASPASO ========== 
        [Column("total_amount")]
        public decimal TotalAmount { get; set; }

        // ========== FECHAS ==========
        [Column("output_date")]
        [Indexed(Name = "IX_StockOutputs_Date")]
        public DateTime OutputDate { get; set; } = DateTime.Now;

        // ========== OBSERVACIONES ==========
        [Column("notes")]
        [MaxLength(500)]
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