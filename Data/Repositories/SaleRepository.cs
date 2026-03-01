using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace CasaCejaRemake.Data.Repositories
{
    public class SaleRepository : BaseRepository<Models.Sale>
    {
        public SaleRepository(DatabaseService databaseService)
            : base(databaseService) { }

        /// <summary>
        /// Trae todas las ventas de una sucursal desde una fecha dada.
        /// Reemplaza el patrón GetAllAsync() + filtro manual en memoria.
        /// </summary>
        public async Task<List<Models.Sale>> GetByBranchSinceDateAsync(int branchId, DateTime since)
        {
            return await FindAsync(s => s.BranchId == branchId && s.SaleDate >= since);
        }

        /// <summary>
        /// Trae las ventas de una sucursal del día indicado (filtra por CreatedAt >= date).
        /// </summary>
        public async Task<List<Models.Sale>> GetDailyByBranchAsync(int branchId, DateTime date)
        {
            var dayStart = date.Date;
            var dayEnd = dayStart.AddDays(1);
            return await FindAsync(s => s.BranchId == branchId && s.CreatedAt >= dayStart && s.CreatedAt < dayEnd);
        }

        /// <summary>
        /// Trae ventas paginadas de una sucursal, filtradas por rango de fechas.
        /// El filtro de fecha va en DB (FindAsync) y la paginación en memoria,
        /// porque sqlite-net-pcl no soporta Skip/Take nativamente en TableQuery.
        /// </summary>
        public async Task<List<Models.Sale>> GetPagedByBranchAsync(
            int branchId,
            int page,
            int pageSize,
            DateTime? start,
            DateTime? end)
        {
            List<Models.Sale> filtered;

            if (start.HasValue && end.HasValue)
            {
                var endOfDay = end.Value.Date.AddDays(1);
                filtered = await FindAsync(s =>
                    s.BranchId == branchId &&
                    s.SaleDate >= start.Value &&
                    s.SaleDate < endOfDay);
            }
            else if (start.HasValue)
            {
                filtered = await FindAsync(s =>
                    s.BranchId == branchId &&
                    s.SaleDate >= start.Value);
            }
            else if (end.HasValue)
            {
                var endOfDay = end.Value.Date.AddDays(1);
                filtered = await FindAsync(s =>
                    s.BranchId == branchId &&
                    s.SaleDate < endOfDay);
            }
            else
            {
                filtered = await FindAsync(s => s.BranchId == branchId);
            }

            // Paginación en memoria (ya filtrado por branch + fecha en DB)
            return filtered
                .OrderByDescending(s => s.SaleDate)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToList();
        }

        /// <summary>
        /// Cuenta las ventas de una sucursal, con filtro de fechas opcional.
        /// </summary>
        public async Task<int> CountByBranchAsync(int branchId, DateTime? start, DateTime? end)
        {
            if (start.HasValue && end.HasValue)
            {
                var endOfDay = end.Value.Date.AddDays(1);
                return await CountAsync(s =>
                    s.BranchId == branchId &&
                    s.SaleDate >= start.Value &&
                    s.SaleDate < endOfDay);
            }
            else if (start.HasValue)
            {
                return await CountAsync(s => s.BranchId == branchId && s.SaleDate >= start.Value);
            }
            else if (end.HasValue)
            {
                var endOfDay = end.Value.Date.AddDays(1);
                return await CountAsync(s => s.BranchId == branchId && s.SaleDate < endOfDay);
            }
            else
            {
                return await CountAsync(s => s.BranchId == branchId);
            }
        }
    }
}
