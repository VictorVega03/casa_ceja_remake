using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using CasaCejaRemake.Models;
using CasaCejaRemake.Models.Results;

namespace CasaCejaRemake.Services.Interfaces
{
    /// <summary>
    /// Contrato público del servicio de corte de caja.
    /// </summary>
    public interface ICashCloseService
    {
        Task<CashCloseResult> OpenCashAsync(decimal openingAmount, int userId, int branchId);
        Task<CashClose?> GetOpenCashAsync(int branchId);
        Task<CashCloseTotals> CalculateTotalsAsync(int cashCloseId, DateTime openingDate);
        Task<CashCloseResult> CloseCashAsync(CashClose cashClose, decimal declaredAmount);
        Task<CashMovementResult> AddMovementAsync(int cashCloseId, string type, string concept, decimal amount, int userId);
        Task<List<CashMovement>> GetMovementsAsync(int cashCloseId);
        Task<List<CashClose>> GetHistoryAsync(int branchId, int limit = 30);
    }
}
