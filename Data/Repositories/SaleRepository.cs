using CasaCejaRemake.Data.Repositories.Interfaces;
using CasaCejaRemake.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace CasaCejaRemake.Data.Repositories
{
    /// <summary>
    /// Repositorio de ventas. Implementa las queries especializadas sobre la tabla sales.
    /// </summary>
    public class SaleRepository : BaseRepository<Sale>, ISaleRepository
    {
        public SaleRepository(DatabaseService databaseService) : base(databaseService) { }

        /// <inheritdoc/>
        public async Task<List<Sale>> GetByBranchAndDateRangeAsync(int branchId, DateTime start, DateTime end)
        {
            return await FindAsync(s => s.BranchId == branchId
                                     && s.SaleDate >= start
                                     && s.SaleDate <= end);
        }

        /// <inheritdoc/>
        public async Task<List<Sale>> GetDailySalesAsync(int branchId, DateTime date)
        {
            var startOfDay = date.Date;
            var endOfDay = date.Date.AddDays(1).AddTicks(-1);
            return await FindAsync(s => s.BranchId == branchId
                                     && s.SaleDate >= startOfDay
                                     && s.SaleDate <= endOfDay);
        }

        /// <inheritdoc/>
        public async Task<List<Sale>> GetPagedAsync(int branchId, int page, int pageSize, DateTime? start, DateTime? end)
        {
            var all = await FindAsync(s =>
                s.BranchId == branchId
                && (!start.HasValue || s.SaleDate >= start.Value)
                && (!end.HasValue   || s.SaleDate <= end.Value));

            // Ordenar por fecha descendente y paginar en memoria
            // (sqlite-net-pcl no soporta OrderBy + Skip + Take en SQL)
            return all.OrderByDescending(s => s.SaleDate)
                      .Skip((page - 1) * pageSize)
                      .Take(pageSize)
                      .ToList();
        }

        /// <inheritdoc/>
        public async Task<int> CountByFiltersAsync(int branchId, DateTime? start, DateTime? end)
        {
            return await CountAsync(s =>
                s.BranchId == branchId
                && (!start.HasValue || s.SaleDate >= start.Value)
                && (!end.HasValue   || s.SaleDate <= end.Value));
        }

        /// <inheritdoc/>
        public async Task<List<Sale>> GetSalesSinceAsync(DateTime since)
        {
            return await FindAsync(s => s.SaleDate >= since);
        }
    }
}
