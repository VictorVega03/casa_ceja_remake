using SQLite;
using System;

namespace CasaCejaRemake.Models
{
    [Table("branches")]
    public class Branch
    {
        [PrimaryKey, AutoIncrement]
        [Column("id")]
        public int Id { get; set; }

        [Column("name")]
        [MaxLength(100)]
        public string Name { get; set; } = string.Empty;

        [Column("address")]
        [MaxLength(255)]
        public string Address { get; set; } = string.Empty;

        [Column("email")]
        [MaxLength(40)]
        public string Email { get; set; } = string.Empty;

        [Column("razon_social")]
        [MaxLength(100)]
        public string RazonSocial { get; set; } = string.Empty;

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