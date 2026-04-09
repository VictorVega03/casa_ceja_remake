using SQLite;
using System;

namespace CasaCejaRemake.Models
{
    /// <summary>
    /// Existencias de un producto en una sucursal específica.
    /// El catálogo de productos es universal; el stock es por sucursal.
    /// Las entradas suman y las salidas restan de esta tabla.
    /// </summary>
    [Table("product_stock")]
    public class ProductStock
    {
        // ========== IDENTIFICADOR ==========
        [PrimaryKey, AutoIncrement]
        [Column("id")]
        public int Id { get; set; }

        // ========== RELACIONES ==========
        [Column("product_id")]
        [Indexed(Name = "IX_ProductStock_Product")]
        public int ProductId { get; set; }

        [Column("branch_id")]
        [Indexed(Name = "IX_ProductStock_Branch")]
        public int BranchId { get; set; }

        // ========== EXISTENCIA ==========
        [Column("quantity")]
        public int Quantity { get; set; } = 0;

        // ========== TIMESTAMPS ==========
        [Column("updated_at")]
        public DateTime UpdatedAt { get; set; } = DateTime.Now;

        // ========== SINCRONIZACIÓN ==========
        [Column("sync_status")]
        public int SyncStatus { get; set; } = 1; // 1=Pending, 2=Synced

        [Column("last_sync")]
        public DateTime? LastSync { get; set; }
    }
}
