using SQLite;
using System;

namespace CasaCejaRemake.Models
{
    [Table("layaway_products")]
    public class LayawayProduct
    {
        // ========== IDENTIFICADOR ==========
        [PrimaryKey, AutoIncrement]
        [Column("id")]
        public int Id { get; set; }

        // ========== RELACIONES ==========
        [Column("layaway_id")]
        [Indexed(Name = "IX_LayawayProducts_Layaway")]
        public int LayawayId { get; set; }

        [Column("product_id")]
        [Indexed(Name = "IX_LayawayProducts_Product")]
        public int ProductId { get; set; }

        // ========== INFORMACIÓN DEL PRODUCTO (INMUTABLE) ==========
        [Column("barcode")]
        [MaxLength(50)]
        public string Barcode { get; set; } = string.Empty;

        [Column("product_name")]
        [MaxLength(200)]
        public string ProductName { get; set; } = string.Empty;

        // ========== CANTIDADES Y PRECIOS ==========
        [Column("quantity")]
        public int Quantity { get; set; }

        [Column("unit_price")]
        public decimal UnitPrice { get; set; }

        [Column("line_total")]
        public decimal LineTotal { get; set; }

        // ========== PRICING DATA (JSON COMPRIMIDO) ==========
        // Contiene información completa de precios al momento del apartado:
        // - precio_menudeo, precio_mayoreo, precio_especial, precio_vendedor
        // - descuento aplicado, categoría con descuento
        // - iva, precio base sin iva, etc.
        [Column("pricing_data")]
        public byte[]? PricingData { get; set; }

        // ========== TIMESTAMP ==========
        [Column("created_at")]
        public DateTime CreatedAt { get; set; } = DateTime.Now;
    }
}
