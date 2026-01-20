using SQLite;
using System;

namespace CasaCejaRemake.Models
{
    [Table("entry_products")]
    public class EntryProduct
    {
        // ========== IDENTIFICADOR ==========
        [PrimaryKey, AutoIncrement]
        [Column("id")]
        public int Id { get; set; }

        // ========== RELACIONES ==========
        [Column("entry_id")]
        [Indexed(Name = "IX_EntryProducts_Entry")]
        public int EntryId { get; set; }

        [Column("product_id")]
        [Indexed(Name = "IX_EntryProducts_Product")]
        public int ProductId { get; set; }

        // ========== INFORMACIÓN DEL PRODUCTO ==========
        [Column("barcode")]
        [MaxLength(50)]
        public string Barcode { get; set; } = string.Empty;

        [Column("product_name")]
        [MaxLength(200)]
        public string ProductName { get; set; } = string.Empty;

        // ========== CANTIDADES Y COSTOS ==========
        [Column("quantity")]
        public int Quantity { get; set; }

        [Column("unit_cost")]
        public decimal UnitCost { get; set; }

        [Column("line_total")]
        public decimal LineTotal { get; set; }

        // ========== TIMESTAMP ==========
        [Column("created_at")]
        public DateTime CreatedAt { get; set; } = DateTime.Now;
    }
}