using CasaCejaRemake.Models;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace CasaCejaRemake.Data.Repositories.Interfaces
{
    /// <summary>
    /// Repositorio especializado para ventas (tabla sales).
    /// Hereda CRUD genérico de IRepository<Sale>.
    /// </summary>
    public interface ISaleRepository : IRepository<Sale>
    {
        /// <summary>
        /// Obtiene todas las ventas de una sucursal dentro de un rango de fechas.
        /// </summary>
        Task<List<Sale>> GetByBranchAndDateRangeAsync(int branchId, DateTime start, DateTime end);

        /// <summary>
        /// Obtiene todas las ventas del día indicado para una sucursal.
        /// </summary>
        Task<List<Sale>> GetDailySalesAsync(int branchId, DateTime date);

        /// <summary>
        /// Obtiene una página de ventas con filtros opcionales de fecha.
        /// </summary>
        Task<List<Sale>> GetPagedAsync(int branchId, int page, int pageSize, DateTime? start, DateTime? end);

        /// <summary>
        /// Cuenta las ventas que coinciden con los filtros indicados.
        /// </summary>
        Task<int> CountByFiltersAsync(int branchId, DateTime? start, DateTime? end);

        /// <summary>
        /// Obtiene todas las ventas realizadas a partir de la fecha indicada (para cálculo de corte).
        /// </summary>
        Task<List<Sale>> GetSalesSinceAsync(DateTime since);
    }
}
