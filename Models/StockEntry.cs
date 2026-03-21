using SQLite;
using System;

namespace CasaCejaRemake.Models
{
    [Table("stock_entries")]
    public class StockEntry
    {
        // ========== IDENTIFICADOR ==========
        [PrimaryKey, AutoIncrement]
        [Column("id")]
        public int Id { get; set; }

        // ========== FOLIOS ==========
        [Column("folio")]
        [MaxLength(50)]
        [Indexed(Name = "IX_StockEntries_Folio", Unique = true)]
        public string Folio { get; set; } = string.Empty;
       
        /// Folio de la salida de origen cuando la entrada viene de un traspaso.
        /// Null cuando la entrada viene de un proveedor (compra directa).
        [Column("folio_output")]
        [MaxLength(50)]
        public string? FolioOutput { get; set; }

        // ========== RELACIONES ==========
        [Column("branch_id")]
        [Indexed(Name = "IX_StockEntries_Branch")]
        public int BranchId { get; set; }

        [Column("supplier_id")]
        [Indexed(Name = "IX_StockEntries_Supplier")]
        public int SupplierId { get; set; }

        [Column("user_id")]
        [Indexed(Name = "IX_StockEntries_User")]
        public int UserId { get; set; }

        // ========== TOTALES ==========
        [Column("total_amount")]
        public decimal TotalAmount { get; set; }

        // ========== FECHAS ==========
        [Column("entry_date")]
        [Indexed(Name = "IX_StockEntries_Date")]
        public DateTime EntryDate { get; set; } = DateTime.Now;

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