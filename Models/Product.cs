using SQLite;
using System;

namespace CasaCejaRemake.Models
{
    [Table("products")]
    public class Product
    {
        [PrimaryKey, AutoIncrement]
        [Column("id")]
        public int Id { get; set; }

        [Column("barcode")]
        [MaxLength(50)]
        [Indexed(Name = "IX_Product_Barcode", Unique = true)]
        public string Barcode { get; set; } = string.Empty;

        [Column("name")]
        [MaxLength(200)]
        [Indexed(Name = "IX_Product_Name")]
        public string Name { get; set; } = string.Empty;

        [Column("category_id")]
        public int CategoryId { get; set; }

        [Column("unit_id")]
        public int UnitId { get; set; }

        [Column("presentation")]
        [MaxLength(100)]
        public string Presentation { get; set; } = string.Empty;

        [Column("iva")]
        public decimal Iva { get; set; } = 0;

        [Column("price_retail")]
        public decimal PriceRetail { get; set; } = 0;

        [Column("price_wholesale")]
        public decimal PriceWholesale { get; set; } = 0;

        [Column("wholesale_quantity")]
        public int WholesaleQuantity { get; set; } = 1;

        [Column("price_special")]
        public decimal PriceSpecial { get; set; } = 0;

        [Column("price_dealer")]
        public decimal PriceDealer { get; set; } = 0;

        [Column("active")]
        public bool Active { get; set; } = true;

        [Column("created_at")]
        public DateTime CreatedAt { get; set; } = DateTime.Now;

        [Column("updated_at")]
        public DateTime UpdatedAt { get; set; } = DateTime.Now;

        [Column("sync_status")]
        public int SyncStatus { get; set; } = 1;

        [Column("last_sync")]
        public DateTime? LastSync { get; set; }

        // Navigation properties
        [Ignore]
        public string CategoryName { get; set; } = string.Empty;

        [Ignore]
        public string UnitName { get; set; } = string.Empty;
    }
}