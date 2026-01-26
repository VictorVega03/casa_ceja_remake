using SQLite;
using System;

namespace CasaCejaRemake.Models
{
    [Table("credits")]
    public class Credit
    {
        // ========== IDENTIFICADOR ==========
        [PrimaryKey, AutoIncrement]
        [Column("id")]
        public int Id { get; set; }

        // ========== FOLIO ==========
        [Column("folio")]
        [MaxLength(50)]
        [Indexed(Name = "IX_Credits_Folio", Unique = true)]
        public string Folio { get; set; } = string.Empty;

        // ========== RELACIONES ==========
        [Column("customer_id")]
        [Indexed(Name = "IX_Credits_Customer")]
        public int CustomerId { get; set; }

        [Column("branch_id")]
        [Indexed(Name = "IX_Credits_Branch")]
        public int BranchId { get; set; }

        [Column("user_id")]
        [Indexed(Name = "IX_Credits_User")]
        public int UserId { get; set; }

        // ========== INFORMACIÓN FINANCIERA ==========
        [Column("total")]
        public decimal Total { get; set; }

        [Column("total_paid")]
        public decimal TotalPaid { get; set; }

        // ========== INFORMACIÓN DE CRÉDITO ==========
        [Column("months_to_pay")]
        public int MonthsToPay { get; set; }

        [Column("credit_date")]
        [Indexed(Name = "IX_Credits_CreditDate")]
        public DateTime CreditDate { get; set; } = DateTime.Now;

        [Column("due_date")]
        [Indexed(Name = "IX_Credits_DueDate")]
        public DateTime DueDate { get; set; }

        // ========== ESTADO ==========
        // 1 = Pending (Pendiente)
        // 2 = Paid (Pagado)
        // 3 = Overdue (Vencido)
        // 4 = Cancelled (Cancelado)
        [Column("status")]
        [Indexed(Name = "IX_Credits_Status")]
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

        // ========== TICKET DATA (para reimpresión) ==========
        [Column("ticket_data")]
        public byte[]? TicketData { get; set; }
       
         // Propiedades calculadas
        [Ignore]
        public decimal RemainingBalance => Total - TotalPaid;

        [Ignore]
        public bool IsPaid => Status == 2 || RemainingBalance <= 0;

        [Ignore]
        public bool IsOverdue => Status == 3 || (DateTime.Now > DueDate && Status == 1);

        [Ignore]
        public string StatusName => Status switch
        {
            1 => "Pendiente",
            2 => "Pagado",
            3 => "Vencido",
            4 => "Cancelado",
            _ => "Desconocido"
        };
    
    }
}