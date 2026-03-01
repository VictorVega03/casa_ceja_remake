using System.Collections.Generic;
using System.Threading.Tasks;
using CasaCejaRemake.Models;

namespace CasaCejaRemake.Services.Interfaces
{
    /// <summary>
    /// Contrato público del servicio de apartados.
    /// </summary>
    public interface ILayawayService
    {
        Task<(bool Success, Layaway? Layaway, string? Error)> CreateLayawayAsync(
            List<CartItem> items,
            int customerId,
            int daysToPickup,
            decimal initialPayment,
            PaymentMethod paymentMethod,
            int userId,
            int branchId,
            string? notes);
        Task<List<Layaway>> GetPendingByCustomerAsync(int customerId);
        Task<List<Layaway>> GetPendingByBranchAsync(int branchId);
        Task<List<Layaway>> SearchAsync(int? customerId, int? status, int branchId);
        Task<Layaway?> GetByIdAsync(int id);
        Task<Layaway?> GetByFolioAsync(string folio);
        Task<List<LayawayProduct>> GetProductsAsync(int layawayId);
        Task<List<LayawayPayment>> GetPaymentsAsync(int layawayId);
        Task<bool> AddPaymentAsync(int layawayId, decimal amount, PaymentMethod method, int userId, string? notes);
        Task<bool> AddPaymentWithMixedAsync(int layawayId, decimal amount, string paymentJson, int userId, string? notes);
        Task<bool> MarkAsDeliveredAsync(int layawayId, int userId);
        Task UpdateStatusAsync(int layawayId);
        Task<TicketData?> RecoverTicketAsync(int layawayId);
    }
}
