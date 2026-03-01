using CasaCejaRemake.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace CasaCejaRemake.Data.Repositories.Interfaces
{
    /// <summary>
    /// Repositorio especializado para apartados (tabla layaways).
    /// Hereda CRUD genérico de IRepository<Layaway>.
    /// </summary>
    public interface ILayawayRepository : IRepository<Layaway>
    {
        /// <summary>
        /// Obtiene los apartados pendientes (status 1 ó 3) de un cliente específico.
        /// </summary>
        Task<List<Layaway>> GetPendingByCustomerAsync(int customerId);

        /// <summary>
        /// Obtiene todos los apartados pendientes (status 1 ó 3) de una sucursal.
        /// </summary>
        Task<List<Layaway>> GetPendingByBranchAsync(int branchId);

        /// <summary>
        /// Busca apartados con filtros opcionales de cliente y estado, dentro de una sucursal.
        /// </summary>
        Task<List<Layaway>> SearchAsync(int? customerId, int? status, int branchId);

        /// <summary>
        /// Busca un apartado por su folio (único).
        /// </summary>
        Task<Layaway?> GetByFolioAsync(string folio);
    }
}
