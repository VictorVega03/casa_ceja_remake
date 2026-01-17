using SQLite;
using System;

namespace casa_ceja_remake.Models
{
    [Table("categories")]
    public class Category
    {
        [PrimaryKey, AutoIncrement]
        [Column("id")]
        public int Id { get; set; }

        [Column("name")]
        [MaxLength(100)]
        [Indexed(Name = "IX_Category_Name", Unique = true)]
        public string Name { get; set; } = string.Empty;

        [Column("discount")]
        public decimal Discount { get; set; } = 0;

        [Column("discount_enabled")]
        public bool DiscountEnabled { get; set; } = false;

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
    }
}