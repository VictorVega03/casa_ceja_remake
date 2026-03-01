using CasaCejaRemake.Data.Repositories.Interfaces;
using CasaCejaRemake.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace CasaCejaRemake.Data.Repositories
{
    /// <summary>
    /// Repositorio de productos de venta. Implementa queries sobre la tabla sale_products.
    /// </summary>
    public class SaleProductRepository : BaseRepository<SaleProduct>, ISaleProductRepository
    {
        public SaleProductRepository(DatabaseService databaseService) : base(databaseService) { }

        /// <inheritdoc/>
        public async Task<List<SaleProduct>> GetBySaleIdAsync(int saleId)
        {
            return await FindAsync(sp => sp.SaleId == saleId);
        }
    }
}
