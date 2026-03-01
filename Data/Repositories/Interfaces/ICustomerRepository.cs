using CasaCejaRemake.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace CasaCejaRemake.Data.Repositories.Interfaces
{
    /// <summary>
    /// Repositorio especializado para clientes (tabla customers).
    /// Hereda CRUD genérico de IRepository<Customer>.
    /// </summary>
    public interface ICustomerRepository : IRepository<Customer>
    {
        /// <summary>
        /// Busca clientes activos cuyo nombre o teléfono contenga el término indicado.
        /// </summary>
        Task<List<Customer>> SearchByTermAsync(string term);

        /// <summary>
        /// Busca un cliente activo por su número de teléfono exacto.
        /// </summary>
        Task<Customer?> GetByPhoneAsync(string phone);

        /// <summary>
        /// Verifica si ya existe un cliente activo con el teléfono indicado.
        /// </summary>
        Task<bool> ExistsByPhoneAsync(string phone);

        /// <summary>
        /// Obtiene todos los clientes activos.
        /// </summary>
        Task<List<Customer>> GetAllActiveAsync();

        /// <summary>
        /// Cuenta todos los clientes activos.
        /// </summary>
        Task<int> CountActiveAsync();
    }
}
