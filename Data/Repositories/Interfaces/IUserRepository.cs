using CasaCejaRemake.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace CasaCejaRemake.Data.Repositories.Interfaces
{
    /// <summary>
    /// Repositorio especializado para usuarios (tabla users).
    /// Hereda CRUD genérico de IRepository<User>.
    /// </summary>
    public interface IUserRepository : IRepository<User>
    {
        /// <summary>
        /// Busca un usuario activo por su nombre de usuario (login).
        /// </summary>
        Task<User?> GetByUsernameAsync(string username);

        /// <summary>
        /// Obtiene todos los usuarios activos cuyo rol es cajero (UserType == 2).
        /// </summary>
        Task<List<User>> GetCashiersAsync();

        /// <summary>
        /// Verifica si el nombre de usuario está disponible.
        /// Excluye opcionalmente al usuario con el ID indicado (para edición).
        /// </summary>
        Task<bool> IsUsernameAvailableAsync(string username, int? excludeUserId = null);
    }
}
