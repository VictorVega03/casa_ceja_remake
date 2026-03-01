using CasaCejaRemake.Data.Repositories.Interfaces;
using CasaCejaRemake.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace CasaCejaRemake.Data.Repositories
{
    /// <summary>
    /// Repositorio de unidades de medida. Implementa queries sobre la tabla units.
    /// </summary>
    public class UnitRepository : BaseRepository<Unit>, IUnitRepository
    {
        public UnitRepository(DatabaseService databaseService) : base(databaseService) { }

        /// <inheritdoc/>
        public async Task<List<Unit>> GetActiveAsync()
        {
            return await FindAsync(u => u.Active);
        }
    }
}
