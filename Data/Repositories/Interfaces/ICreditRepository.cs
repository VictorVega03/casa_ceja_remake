using CasaCejaRemake.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace CasaCejaRemake.Data.Repositories.Interfaces
{
    /// <summary>
    /// Repositorio especializado para créditos (tabla credits).
    /// Hereda CRUD genérico de IRepository<Credit>.
    /// </summary>
    public interface ICreditRepository : IRepository<Credit>
    {
        /// <summary>
        /// Obtiene los créditos pendientes (status 1 ó 3) de un cliente específico.
        /// </summary>
        Task<List<Credit>> GetPendingByCustomerAsync(int customerId);

        /// <summary>
        /// Obtiene todos los créditos pendientes (status 1 ó 3) de una sucursal.
        /// </summary>
        Task<List<Credit>> GetPendingByBranchAsync(int branchId);

        /// <summary>
        /// Busca créditos con filtros opcionales de cliente y estado, dentro de una sucursal.
        /// </summary>
        Task<List<Credit>> SearchAsync(int? customerId, int? status, int branchId);

        /// <summary>
        /// Busca un crédito por su folio (único).
        /// </summary>
        Task<Credit?> GetByFolioAsync(string folio);
    }
}
