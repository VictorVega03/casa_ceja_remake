using CasaCejaRemake.Data.Repositories.Interfaces;
using CasaCejaRemake.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace CasaCejaRemake.Data.Repositories
{
    /// <summary>
    /// Repositorio de clientes. Implementa búsqueda especializada sobre la tabla customers.
    /// </summary>
    public class CustomerRepository : BaseRepository<Customer>, ICustomerRepository
    {
        public CustomerRepository(DatabaseService databaseService) : base(databaseService) { }

        /// <inheritdoc/>
        public async Task<List<Customer>> SearchByTermAsync(string term)
        {
            var termLower = (term ?? string.Empty).ToLowerInvariant();

            if (string.IsNullOrEmpty(termLower))
                return await FindAsync(c => c.Active);

            return await FindAsync(c =>
                c.Active
                && (c.Name.ToLower().Contains(termLower)
                    || c.Phone.Contains(termLower, System.StringComparison.OrdinalIgnoreCase)));
        }

        /// <inheritdoc/>
        public async Task<Customer?> GetByPhoneAsync(string phone)
        {
            return await FirstOrDefaultAsync(c => c.Active && c.Phone == phone);
        }

        /// <inheritdoc/>
        public async Task<bool> ExistsByPhoneAsync(string phone)
        {
            return await ExistsAsync(c => c.Active && c.Phone == phone);
        }

        /// <inheritdoc/>
        public async Task<List<Customer>> GetAllActiveAsync()
        {
            return await FindAsync(c => c.Active);
        }

        /// <inheritdoc/>
        public async Task<int> CountActiveAsync()
        {
            return await CountAsync(c => c.Active);
        }
    }
}
