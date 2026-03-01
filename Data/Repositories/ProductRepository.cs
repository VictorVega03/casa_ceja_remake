using CasaCejaRemake.Data.Repositories.Interfaces;
using CasaCejaRemake.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace CasaCejaRemake.Data.Repositories
{
    /// <summary>
    /// Repositorio de productos. Implementa búsqueda especializada sobre la tabla products.
    /// </summary>
    public class ProductRepository : BaseRepository<Product>, IProductRepository
    {
        public ProductRepository(DatabaseService databaseService) : base(databaseService) { }

        /// <inheritdoc/>
        public async Task<List<Product>> SearchAsync(string term, int? categoryId, int? unitId, bool onlyActive = true)
        {
            var termLower = (term ?? string.Empty).ToLowerInvariant();

            return await FindAsync(p =>
                (!onlyActive || p.Active)
                && (!categoryId.HasValue || p.CategoryId == categoryId.Value)
                && (!unitId.HasValue     || p.UnitId == unitId.Value)
                && (string.IsNullOrEmpty(termLower)
                    || p.Name.ToLower().Contains(termLower)
                    || p.Barcode.ToLower().Contains(termLower)));
        }

        /// <inheritdoc/>
        public async Task<Product?> GetByBarcodeAsync(string barcode)
        {
            return await FirstOrDefaultAsync(p => p.Active && p.Barcode == barcode);
        }

        /// <inheritdoc/>
        public async Task<int> GetActiveCountAsync()
        {
            return await CountAsync(p => p.Active);
        }
    }
}
