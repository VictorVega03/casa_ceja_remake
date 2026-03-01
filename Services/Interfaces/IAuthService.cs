using System;
using System.Threading.Tasks;
using CasaCejaRemake.Models;

namespace CasaCejaRemake.Services.Interfaces
{
    /// <summary>
    /// Contrato público del servicio de autenticación.
    /// </summary>
    public interface IAuthService
    {
        // ─── Propiedades ───────────────────────────────────────────────────────────
        User? CurrentUser { get; }
        bool IsAuthenticated { get; }
        bool IsAdmin { get; }
        bool IsCajero { get; }
        string? CurrentUserName { get; }
        int CurrentBranchId { get; }
        RoleService RoleService { get; }

        // ─── Eventos ──────────────────────────────────────────────────────────────
        event EventHandler<User>? UserLoggedIn;
        event EventHandler? UserLoggedOut;

        // ─── Métodos ──────────────────────────────────────────────────────────────
        Task<bool> LoginAsync(string username, string password);
        void Logout();
        void SetCurrentUser(User user);
        bool SetCurrentBranch(int branchId);
        bool HasAccessLevel(int requiredLevel);
        string GetCurrentRoleName();
        string? GetUserCashRegisterId();
        bool HasBranchAccess(int branchId);
    }
}
