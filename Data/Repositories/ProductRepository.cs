using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace CasaCejaRemake.Data.Repositories
{
    public class ProductRepository : BaseRepository<Models.Product>
    {
        public ProductRepository(DatabaseService databaseService)
            : base(databaseService) { }

        /// <summary>
        /// Busca productos combinando término de texto, categoría y unidad.
        /// sqlite-net-pcl tiene soporte limitado para predicados compuestos con Contains,
        /// por eso filtramos la categoría/unidad/active en DB y el término de texto en memoria.
        /// </summary>
        public async Task<List<Models.Product>> SearchAsync(
            string term,
            int? categoryId,
            int? unitId,
            bool onlyActive = true)
        {
            // Filtro principal en DB (simple, compatible con sqlite-net-pcl)
            List<Models.Product> results;

            if (categoryId.HasValue && categoryId > 0 && unitId.HasValue && unitId > 0)
            {
                results = await FindAsync(p =>
                    (!onlyActive || p.Active) &&
                    p.CategoryId == categoryId &&
                    p.UnitId == unitId);
            }
            else if (categoryId.HasValue && categoryId > 0)
            {
                results = await FindAsync(p =>
                    (!onlyActive || p.Active) &&
                    p.CategoryId == categoryId);
            }
            else if (unitId.HasValue && unitId > 0)
            {
                results = await FindAsync(p =>
                    (!onlyActive || p.Active) &&
                    p.UnitId == unitId);
            }
            else
            {
                results = await FindAsync(p => !onlyActive || p.Active);
            }

            // Filtro de texto en memoria (Contains no es soportado nativamente por sqlite-net-pcl)
            if (!string.IsNullOrWhiteSpace(term))
            {
                results = results
                    .Where(p =>
                        p.Name.Contains(term, StringComparison.OrdinalIgnoreCase) ||
                        p.Barcode.Contains(term, StringComparison.OrdinalIgnoreCase))
                    .ToList();
            }

            return results;
        }

        /// <summary>
        /// Busca un producto activo por su código de barras exacto.
        /// </summary>
        public async Task<Models.Product?> GetByBarcodeAsync(string barcode)
        {
            if (string.IsNullOrWhiteSpace(barcode))
                return null;

            return await FirstOrDefaultAsync(p => p.Barcode == barcode && p.Active);
        }
    }
}
