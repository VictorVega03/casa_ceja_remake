using CasaCejaRemake.Data.Repositories.Interfaces;
using CasaCejaRemake.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace CasaCejaRemake.Data.Repositories
{
    /// <summary>
    /// Repositorio de usuarios. Implementa queries especializadas sobre la tabla users.
    /// </summary>
    public class UserRepository : BaseRepository<User>, IUserRepository
    {
        public UserRepository(DatabaseService databaseService) : base(databaseService) { }

        /// <inheritdoc/>
        public async Task<User?> GetByUsernameAsync(string username)
        {
            return await FirstOrDefaultAsync(u => u.Active && u.Username == username);
        }

        /// <inheritdoc/>
        /// <remarks>
        /// Los cajeros tienen UserType == 2 según la convención del sistema.
        /// </remarks>
        public async Task<List<User>> GetCashiersAsync()
        {
            return await FindAsync(u => u.Active && u.UserType == 2);
        }

        /// <inheritdoc/>
        public async Task<bool> IsUsernameAvailableAsync(string username, int? excludeUserId = null)
        {
            if (excludeUserId.HasValue)
            {
                return !await ExistsAsync(u =>
                    u.Username == username && u.Id != excludeUserId.Value);
            }

            return !await ExistsAsync(u => u.Username == username);
        }
    }
}
