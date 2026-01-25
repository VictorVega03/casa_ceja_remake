using SQLite;
using System;

namespace CasaCejaRemake.Models
{
    [Table("layaways")]
    public class Layaway
    {
        // ========== IDENTIFICADOR ==========
        [PrimaryKey, AutoIncrement]
        [Column("id")]
        public int Id { get; set; }

        // ========== FOLIO ==========
        [Column("folio")]
        [MaxLength(50)]
        [Indexed(Name = "IX_Layaways_Folio", Unique = true)]
        public string Folio { get; set; } = string.Empty;

        // ========== RELACIONES ==========
        [Column("customer_id")]
        [Indexed(Name = "IX_Layaways_Customer")]
        public int CustomerId { get; set; }

        [Column("branch_id")]
        [Indexed(Name = "IX_Layaways_Branch")]
        public int BranchId { get; set; }

        [Column("user_id")] // Cajero que registra el apartado
        [Indexed(Name = "IX_Layaways_User")]
        public int UserId { get; set; }

        [Column("delivery_user_id")] // Cajero que entrega la mercancía
        public int? DeliveryUserId { get; set; }

        // ========== INFORMACIÓN FINANCIERA ==========
        [Column("total")]
        public decimal Total { get; set; }

        [Column("total_paid")]
        public decimal TotalPaid { get; set; }

        // ========== FECHAS DE APARTADO ==========
        [Column("layaway_date")]
        [Indexed(Name = "IX_Layaways_LayawayDate")]
        public DateTime LayawayDate { get; set; } = DateTime.Now;

        [Column("pickup_date")] // Fecha ESTIMADA de entrega
        [Indexed(Name = "IX_Layaways_PickupDate")]
        public DateTime PickupDate { get; set; }

        [Column("delivery_date")] // Fecha REAL de entrega
        public DateTime? DeliveryDate { get; set; }

        // ========== ESTADO ==========
        // 1 = Pending (Pendiente de entrega)
        // 2 = Delivered (Entregado)
        // 3 = Expired (Expirado sin recoger)
        // 4 = Cancelled (Cancelado)
        [Column("status")]
        [Indexed(Name = "IX_Layaways_Status")]
        public int Status { get; set; } = 1;

        // ========== OBSERVACIONES ==========
        [Column("notes")]
        [MaxLength(500)]
        public string? Notes { get; set; }

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

        
        // Propiedades calculadas
        [Ignore]
        public decimal RemainingBalance => Total - TotalPaid;

        [Ignore]
        public bool IsFullyPaid => RemainingBalance <= 0;

        [Ignore]
        public bool IsDelivered => Status == 2;

        [Ignore]
        public bool IsExpired => Status == 3 || (DateTime.Now > PickupDate && Status == 1);

        [Ignore]
        public bool CanDeliver => IsFullyPaid && Status == 1;

        [Ignore]
        public string StatusName => Status switch
        {
            1 => "Pendiente",
            2 => "Entregado",
            3 => "Vencido",
            4 => "Cancelado",
            _ => "Desconocido"
        };
    
    }
}