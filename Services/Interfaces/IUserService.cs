using System.Collections.Generic;
using System.Threading.Tasks;
using CasaCejaRemake.Models;

namespace CasaCejaRemake.Services.Interfaces
{
    /// <summary>
    /// Contrato público del servicio de gestión de usuarios.
    /// </summary>
    public interface IUserService
    {
        Task<List<User>> GetAllUsersAsync();
        Task<List<User>> GetCashiersAsync();
        Task<User?> GetByIdAsync(int id);
        Task<(bool Success, string Message)> CreateUserAsync(User user);
        Task<(bool Success, string Message)> UpdateUserAsync(User user);
        Task<(bool Success, string Message)> DeactivateUserAsync(int userId);
        Task<bool> IsUsernameAvailableAsync(string username, int? excludeUserId = null);
        List<Role> GetAvailableRoles();
        int GetCashierRoleId();
        Task<bool> IsAdminAsync(int userId);
        Task<(bool Success, User? User)> AuthenticateAsync(string username, string password);
    }
}
