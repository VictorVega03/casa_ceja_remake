using CasaCejaRemake.Data.Repositories.Interfaces;
using CasaCejaRemake.Models;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace CasaCejaRemake.Data.Repositories
{
    /// <summary>
    /// Repositorio de pagos de crédito. Implementa queries sobre la tabla credit_payments.
    /// </summary>
    public class CreditPaymentRepository : BaseRepository<CreditPayment>, ICreditPaymentRepository
    {
        public CreditPaymentRepository(DatabaseService databaseService) : base(databaseService) { }

        /// <inheritdoc/>
        public async Task<List<CreditPayment>> GetByCreditIdAsync(int creditId)
        {
            return await FindAsync(cp => cp.CreditId == creditId);
        }

        /// <inheritdoc/>
        public async Task<List<CreditPayment>> GetPaymentsSinceAsync(DateTime since)
        {
            return await FindAsync(cp => cp.PaymentDate >= since);
        }
    }
}
