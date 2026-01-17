using SQLite;
using System;

namespace casa_ceja_remake.Models
{
    [Table("sale_products")]
    public class SaleProduct
    {
        [PrimaryKey, AutoIncrement]
        [Column("id")]
        public int Id { get; set; }

        [Column("sale_id")]
        [Indexed(Name = "IX_SaleProduct_SaleId")]
        public int SaleId { get; set; }

        [Column("product_id")]
        public int ProductId { get; set; }

        [Column("barcode")]
        [MaxLength(50)]
        public string Barcode { get; set; } = string.Empty;

        [Column("product_name")]
        [MaxLength(200)]
        public string ProductName { get; set; } = string.Empty;

        [Column("quantity")]
        public int Quantity { get; set; } = 0;

        // ============ CAMPOS SQL (para queries rápidas) ============
        [Column("list_price")]
        public decimal ListPrice { get; set; } = 0; // Precio de lista original

        [Column("final_unit_price")]
        public decimal FinalUnitPrice { get; set; } = 0; // Precio final unitario

        [Column("line_total")]
        public decimal LineTotal { get; set; } = 0; // quantity * final_unit_price

        [Column("total_discount_amount")]
        public decimal TotalDiscountAmount { get; set; } = 0; // Total descontado

        [Column("price_type")]
        [MaxLength(20)]
        public string PriceType { get; set; } = "retail"; // retail, wholesale, special, dealer

        [Column("discount_info")]
        [MaxLength(100)]
        public string DiscountInfo { get; set; } = string.Empty; // "Mayoreo + 10% desc. Cuadernos"

        // ============ JSON COMPRIMIDO (auditoría completa) ============
        [Column("pricing_data")]
        public byte[] PricingData { get; set; } = Array.Empty<byte>(); // Detalle completo comprimido

        [Column("created_at")]
        public DateTime CreatedAt { get; set; } = DateTime.Now;
    }
}