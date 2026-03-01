using CasaCejaRemake.Data.Repositories.Interfaces;
using CasaCejaRemake.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace CasaCejaRemake.Data.Repositories
{
    /// <summary>
    /// Repositorio de créditos. Implementa queries especializadas sobre la tabla credits.
    /// </summary>
    public class CreditRepository : BaseRepository<Credit>, ICreditRepository
    {
        public CreditRepository(DatabaseService databaseService) : base(databaseService) { }

        /// <inheritdoc/>
        public async Task<List<Credit>> GetPendingByCustomerAsync(int customerId)
        {
            return await FindAsync(c =>
                c.CustomerId == customerId && (c.Status == 1 || c.Status == 3));
        }

        /// <inheritdoc/>
        public async Task<List<Credit>> GetPendingByBranchAsync(int branchId)
        {
            return await FindAsync(c =>
                c.BranchId == branchId && (c.Status == 1 || c.Status == 3));
        }

        /// <inheritdoc/>
        public async Task<List<Credit>> SearchAsync(int? customerId, int? status, int branchId)
        {
            return await FindAsync(c =>
                c.BranchId == branchId
                && (!customerId.HasValue || c.CustomerId == customerId.Value)
                && (!status.HasValue     || c.Status == status.Value));
        }

        /// <inheritdoc/>
        public async Task<Credit?> GetByFolioAsync(string folio)
        {
            return await FirstOrDefaultAsync(c => c.Folio == folio);
        }
    }
}
