using System.Collections.Generic;
using System.Threading.Tasks;
using CasaCejaRemake.Models;

namespace CasaCejaRemake.Services.Interfaces
{
    /// <summary>
    /// Contrato público del servicio de clientes.
    /// </summary>
    public interface ICustomerService
    {
        Task<List<Customer>> SearchAsync(string term);
        Task<Customer?> GetByIdAsync(int id);
        Task<Customer?> GetByPhoneAsync(string phone);
        Task<bool> ExistsByPhoneAsync(string phone);
        Task<int> CreateAsync(Customer customer);
        Task UpdateAsync(Customer customer);
        Task<bool> DeactivateAsync(int id);
        Task<List<Customer>> GetAllActiveAsync();
        Task<int> CountActiveAsync();
    }
}
