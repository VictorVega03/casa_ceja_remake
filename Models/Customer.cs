using SQLite;
using System;

namespace CasaCejaRemake.Models
{
    [Table("customers")]
    public class Customer
    {
        [PrimaryKey, AutoIncrement]
        [Column("id")]
        public int Id { get; set; }

        [Column("name")]
        [MaxLength(100)]
        [Indexed(Name = "IX_Customer_Name")]
        public string Name { get; set; } = string.Empty;

        [Column("rfc")]
        [MaxLength(13)]
        public string Rfc { get; set; } = string.Empty;      

        [Column("street")]
        [MaxLength(255)]
        public string Street { get; set; } = string.Empty;

        [Column("exterior_number")]
        [MaxLength(20)]
        public string ExteriorNumber { get; set; } = string.Empty;

        [Column("interior_number")]
        [MaxLength(20)]
        public string? InteriorNumber { get; set; }

        [Column("neighborhood")]
        [MaxLength(100)]
        public string Neighborhood { get; set; } = string.Empty;

        [Column("postal_code")]
        [MaxLength(10)]
        public string PostalCode { get; set; } = string.Empty;

        [Column("city")]
        [MaxLength(100)]
        public string City { get; set; } = string.Empty;

        [Column("email")]
        [MaxLength(100)]
        public string Email { get; set; } = string.Empty;

        [Column("phone")]
        [MaxLength(20)]
        public string Phone { get; set; } = string.Empty;

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

         // ========== PROPIEDADES CALCULADAS ==========
        [Ignore]
        public string FullAddress => string.IsNullOrWhiteSpace(Street) ? 
            City : $"{Street} {ExteriorNumber}{(string.IsNullOrWhiteSpace(InteriorNumber) ? 
            "" : $" Int. {InteriorNumber}")}, {Neighborhood}, {City}";

        [Ignore]
        public string DisplayName => string.IsNullOrWhiteSpace(Name) ? Phone : Name;    
    }
}