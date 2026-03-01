using CasaCejaRemake.Data.Repositories.Interfaces;
using CasaCejaRemake.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace CasaCejaRemake.Data.Repositories
{
    /// <summary>
    /// Repositorio de sucursales. Implementa queries sobre la tabla branches.
    /// </summary>
    public class BranchRepository : BaseRepository<Branch>, IBranchRepository
    {
        public BranchRepository(DatabaseService databaseService) : base(databaseService) { }

        /// <inheritdoc/>
        public async Task<List<Branch>> GetActiveAsync()
        {
            return await FindAsync(b => b.Active);
        }
    }
}
