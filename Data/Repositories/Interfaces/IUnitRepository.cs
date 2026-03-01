using CasaCejaRemake.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace CasaCejaRemake.Data.Repositories.Interfaces
{
    /// <summary>
    /// Repositorio especializado para unidades de medida (tabla units).
    /// Hereda CRUD genérico de IRepository<Unit>.
    /// </summary>
    public interface IUnitRepository : IRepository<Unit>
    {
        /// <summary>
        /// Obtiene todas las unidades de medida activas.
        /// </summary>
        Task<List<Unit>> GetActiveAsync();
    }
}
