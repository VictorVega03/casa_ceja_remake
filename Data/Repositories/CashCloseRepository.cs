using CasaCejaRemake.Data.Repositories.Interfaces;
using CasaCejaRemake.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace CasaCejaRemake.Data.Repositories
{
    /// <summary>
    /// Repositorio de cortes de caja. Implementa queries especializadas sobre la tabla cash_closes.
    /// </summary>
    public class CashCloseRepository : BaseRepository<CashClose>, ICashCloseRepository
    {
        public CashCloseRepository(DatabaseService databaseService) : base(databaseService) { }

        /// <inheritdoc/>
        /// <remarks>
        /// Un corte está "abierto" cuando su close_date es igual a su opening_date
        /// (convención del sistema: el corte aún no fue cerrado formalmente).
        /// </remarks>
        public async Task<CashClose?> GetOpenAsync(int branchId)
        {
            return await FirstOrDefaultAsync(cc =>
                cc.BranchId == branchId && cc.CloseDate == cc.OpeningDate);
        }

        /// <inheritdoc/>
        public async Task<List<CashClose>> GetHistoryAsync(int branchId, int limit)
        {
            // Obtenemos todos los cerrados (close_date != opening_date) y tomamos los N más recientes
            var all = await FindAsync(cc =>
                cc.BranchId == branchId && cc.CloseDate != cc.OpeningDate);

            return all.OrderByDescending(cc => cc.CloseDate)
                      .Take(limit)
                      .ToList();
        }

        /// <inheritdoc/>
        public async Task<List<CashClose>> GetByDateRangeAsync(int branchId, DateTime start, DateTime end)
        {
            return await FindAsync(cc =>
                cc.BranchId == branchId
                && cc.CloseDate >= start
                && cc.CloseDate <= end);
        }
    }
}
