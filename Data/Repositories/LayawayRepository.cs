using CasaCejaRemake.Data.Repositories.Interfaces;
using CasaCejaRemake.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace CasaCejaRemake.Data.Repositories
{
    /// <summary>
    /// Repositorio de apartados. Implementa queries especializadas sobre la tabla layaways.
    /// </summary>
    public class LayawayRepository : BaseRepository<Layaway>, ILayawayRepository
    {
        public LayawayRepository(DatabaseService databaseService) : base(databaseService) { }

        /// <inheritdoc/>
        public async Task<List<Layaway>> GetPendingByCustomerAsync(int customerId)
        {
            return await FindAsync(l =>
                l.CustomerId == customerId && (l.Status == 1 || l.Status == 3));
        }

        /// <inheritdoc/>
        public async Task<List<Layaway>> GetPendingByBranchAsync(int branchId)
        {
            return await FindAsync(l =>
                l.BranchId == branchId && (l.Status == 1 || l.Status == 3));
        }

        /// <inheritdoc/>
        public async Task<List<Layaway>> SearchAsync(int? customerId, int? status, int branchId)
        {
            return await FindAsync(l =>
                l.BranchId == branchId
                && (!customerId.HasValue || l.CustomerId == customerId.Value)
                && (!status.HasValue     || l.Status == status.Value));
        }

        /// <inheritdoc/>
        public async Task<Layaway?> GetByFolioAsync(string folio)
        {
            return await FirstOrDefaultAsync(l => l.Folio == folio);
        }
    }
}
