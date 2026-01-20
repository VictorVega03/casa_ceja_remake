using SQLite;
using System;

namespace CasaCejaRemake.Models
{
    [Table("credit_products")]
    public class CreditProduct
    {
        // ========== IDENTIFICADOR ==========
        [PrimaryKey, AutoIncrement]
        [Column("id")]
        public int Id { get; set; }

        // ========== RELACIONES ==========
        [Column("credit_id")]
        [Indexed(Name = "IX_CreditProducts_Credit")]
        public int CreditId { get; set; }

        [Column("product_id")]
        [Indexed(Name = "IX_CreditProducts_Product")]
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
        // Contiene información completa de precios al momento de la venta:
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