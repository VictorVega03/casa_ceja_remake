using CasaCejaRemake.Data.Repositories.Interfaces;
using CasaCejaRemake.Models;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace CasaCejaRemake.Data.Repositories
{
    /// <summary>
    /// Repositorio de pagos de apartado. Implementa queries sobre la tabla layaway_payments.
    /// </summary>
    public class LayawayPaymentRepository : BaseRepository<LayawayPayment>, ILayawayPaymentRepository
    {
        public LayawayPaymentRepository(DatabaseService databaseService) : base(databaseService) { }

        /// <inheritdoc/>
        public async Task<List<LayawayPayment>> GetByLayawayIdAsync(int layawayId)
        {
            return await FindAsync(lp => lp.LayawayId == layawayId);
        }

        /// <inheritdoc/>
        public async Task<List<LayawayPayment>> GetPaymentsSinceAsync(DateTime since)
        {
            return await FindAsync(lp => lp.PaymentDate >= since);
        }
    }
}
