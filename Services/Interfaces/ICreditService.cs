using System.Collections.Generic;
using System.Threading.Tasks;
using CasaCejaRemake.Models;

namespace CasaCejaRemake.Services.Interfaces
{
    /// <summary>
    /// Contrato público del servicio de créditos.
    /// </summary>
    public interface ICreditService
    {
        Task<(bool Success, Credit? Credit, string? Error)> CreateCreditAsync(
            List<CartItem> items,
            int customerId,
            int monthsToPay,
            decimal initialPayment,
            PaymentMethod paymentMethod,
            int userId,
            int branchId,
            string? notes);
        Task<List<Credit>> GetPendingByCustomerAsync(int customerId);
        Task<List<Credit>> GetPendingByBranchAsync(int branchId);
        Task<List<Credit>> SearchAsync(int? customerId, int? status, int branchId);
        Task<Credit?> GetByIdAsync(int id);
        Task<Credit?> GetByFolioAsync(string folio);
        Task<List<CreditProduct>> GetProductsAsync(int creditId);
        Task<List<CreditPayment>> GetPaymentsAsync(int creditId);
        Task<bool> AddPaymentAsync(int creditId, decimal amount, PaymentMethod method, int userId, string? notes);
        Task<bool> AddPaymentWithMixedAsync(int creditId, decimal amount, string paymentJson, int userId, string? notes);
        Task UpdateStatusAsync(int creditId);
        Task<TicketData?> RecoverTicketAsync(int creditId);
    }
}
