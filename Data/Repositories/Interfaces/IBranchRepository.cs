using CasaCejaRemake.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace CasaCejaRemake.Data.Repositories.Interfaces
{
    /// <summary>
    /// Repositorio especializado para sucursales (tabla branches).
    /// Hereda CRUD genérico de IRepository<Branch>.
    /// </summary>
    public interface IBranchRepository : IRepository<Branch>
    {
        /// <summary>
        /// Obtiene todas las sucursales activas.
        /// </summary>
        Task<List<Branch>> GetActiveAsync();
    }
}
