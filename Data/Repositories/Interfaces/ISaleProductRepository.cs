using CasaCejaRemake.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace CasaCejaRemake.Data.Repositories.Interfaces
{
    /// <summary>
    /// Repositorio especializado para productos de venta (tabla sale_products).
    /// Hereda CRUD genérico de IRepository<SaleProduct>.
    /// </summary>
    public interface ISaleProductRepository : IRepository<SaleProduct>
    {
        /// <summary>
        /// Obtiene todos los productos pertenecientes a una venta específica.
        /// </summary>
        Task<List<SaleProduct>> GetBySaleIdAsync(int saleId);
    }
}
