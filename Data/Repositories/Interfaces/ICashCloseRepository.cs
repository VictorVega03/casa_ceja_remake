using CasaCejaRemake.Models;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace CasaCejaRemake.Data.Repositories.Interfaces
{
    /// <summary>
    /// Repositorio especializado para cortes de caja (tabla cash_closes).
    /// Hereda CRUD genérico de IRepository<CashClose>.
    /// </summary>
    public interface ICashCloseRepository : IRepository<CashClose>
    {
        /// <summary>
        /// Obtiene el corte de caja actualmente abierto para una sucursal.
        /// Un corte está "abierto" cuando close_date == opening_date (no fue cerrado aún).
        /// Retorna null si no existe ninguno abierto.
        /// </summary>
        Task<CashClose?> GetOpenAsync(int branchId);

        /// <summary>
        /// Obtiene los últimos N cortes de caja cerrados para una sucursal, ordenados por fecha descendente.
        /// </summary>
        Task<List<CashClose>> GetHistoryAsync(int branchId, int limit);

        /// <summary>
        /// Obtiene todos los cortes de caja de una sucursal dentro de un rango de fechas.
        /// </summary>
        Task<List<CashClose>> GetByDateRangeAsync(int branchId, DateTime start, DateTime end);
    }
}
