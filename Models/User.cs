using SQLite;
using System;

namespace casa_ceja_remake.Models
{
    [Table("users")]
    public class User
    {
        [PrimaryKey, AutoIncrement]
        [Column("id")]
        public int Id { get; set; }

        [Column("name")]
        [MaxLength(100)]
        public string Name { get; set; } = string.Empty;

        [Column("username")]
        [MaxLength(50)]
        [Indexed(Name = "IX_User_Username", Unique = true)]
        public string Username { get; set; } = string.Empty;

        [Column("password")]
        [MaxLength(255)]
        public string Password { get; set; } = string.Empty;

        [Column("user_type")]
        public int UserType { get; set; } // 1 = Admin, 2 = Cashier

        [Column("branch_id")]
        public int? BranchId { get; set; }

        [Column("active")]
        public bool Active { get; set; } = true;

        [Column("created_at")]
        public DateTime CreatedAt { get; set; } = DateTime.Now;

        [Column("updated_at")]
        public DateTime UpdatedAt { get; set; } = DateTime.Now;

        [Column("sync_status")]
        public int SyncStatus { get; set; } = 1; // 1 = Pending, 2 = Synced

        [Column("last_sync")]
        public DateTime? LastSync { get; set; }

        // Navigation properties (not mapped to DB)
        [Ignore]
        public string UserTypeText => UserType == 1 ? "Admin" : "Cashier";
    }
}