using CasaCejaRemake.Data.Repositories.Interfaces;
using CasaCejaRemake.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace CasaCejaRemake.Data.Repositories
{
    /// <summary>
    /// Repositorio de movimientos de caja. Implementa queries sobre la tabla cash_movements.
    /// </summary>
    public class CashMovementRepository : BaseRepository<CashMovement>, ICashMovementRepository
    {
        public CashMovementRepository(DatabaseService databaseService) : base(databaseService) { }

        /// <inheritdoc/>
        public async Task<List<CashMovement>> GetByCashCloseIdAsync(int cashCloseId)
        {
            return await FindAsync(m => m.CashCloseId == cashCloseId);
        }
    }
}
