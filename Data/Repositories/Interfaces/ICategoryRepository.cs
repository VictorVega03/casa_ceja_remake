using CasaCejaRemake.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace CasaCejaRemake.Data.Repositories.Interfaces
{
    /// <summary>
    /// Repositorio especializado para categorías de producto (tabla categories).
    /// Hereda CRUD genérico de IRepository<Category>.
    /// </summary>
    public interface ICategoryRepository : IRepository<Category>
    {
        /// <summary>
        /// Obtiene todas las categorías activas.
        /// </summary>
        Task<List<Category>> GetActiveAsync();
    }
}
