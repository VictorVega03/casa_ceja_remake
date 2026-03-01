using CasaCejaRemake.Data.Repositories.Interfaces;
using CasaCejaRemake.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace CasaCejaRemake.Data.Repositories
{
    /// <summary>
    /// Repositorio de categorías de producto. Implementa queries sobre la tabla categories.
    /// </summary>
    public class CategoryRepository : BaseRepository<Category>, ICategoryRepository
    {
        public CategoryRepository(DatabaseService databaseService) : base(databaseService) { }

        /// <inheritdoc/>
        public async Task<List<Category>> GetActiveAsync()
        {
            return await FindAsync(c => c.Active);
        }
    }
}
