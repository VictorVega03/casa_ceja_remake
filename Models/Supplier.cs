using SQLite;
using System;

namespace CasaCejaRemake.Models
{
    [Table("suppliers")]
    public class Supplier
    {
        // ========== IDENTIFICADOR ==========
        [PrimaryKey, AutoIncrement]
        [Column("id")]
        public int Id { get; set; }

        // ========== INFORMACIÓN BÁSICA ==========
        [Column("name")]
        [MaxLength(200)]
        [Indexed(Name = "IX_Suppliers_Name")]
        public string Name { get; set; } = string.Empty;

        // ========== INFORMACIÓN DE CONTACTO ==========     
        [Column("phone")]
        [MaxLength(20)]
        public string? Phone { get; set; }

        [Column("email")]
        [MaxLength(100)]
        public string? Email { get; set; }

        // ========== DIRECCIÓN ==========
        [Column("address")]
        [MaxLength(300)]
        public string? Address { get; set; }
        
        // ========== ESTADO ==========
        [Column("active")]
        public bool Active { get; set; } = true;

        // ========== TIMESTAMPS ==========
        [Column("created_at")]
        public DateTime CreatedAt { get; set; } = DateTime.Now;

        [Column("updated_at")]
        public DateTime UpdatedAt { get; set; } = DateTime.Now;

        // ========== SINCRONIZACIÓN ==========
        [Column("sync_status")]
        public int SyncStatus { get; set; } = 1; // 1=Pending, 2=Synced, 3=Error

        [Column("last_sync")]
        public DateTime? LastSync { get; set; }
    }
}