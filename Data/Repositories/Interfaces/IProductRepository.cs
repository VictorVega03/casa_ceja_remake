using CasaCejaRemake.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace CasaCejaRemake.Data.Repositories.Interfaces
{
    /// <summary>
    /// Repositorio especializado para productos (tabla products).
    /// Hereda CRUD genérico de IRepository<Product>.
    /// </summary>
    public interface IProductRepository : IRepository<Product>
    {
        /// <summary>
        /// Busca productos por término (nombre o código de barras) con filtros opcionales de categoría y unidad.
        /// </summary>
        Task<List<Product>> SearchAsync(string term, int? categoryId, int? unitId, bool onlyActive = true);

        /// <summary>
        /// Busca un producto activo por su código de barras exacto.
        /// </summary>
        Task<Product?> GetByBarcodeAsync(string barcode);

        /// <summary>
        /// Cuenta el total de productos activos.
        /// </summary>
        Task<int> GetActiveCountAsync();
    }
}
