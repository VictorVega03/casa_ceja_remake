using System.Collections.Generic;
using System.Threading.Tasks;
using CasaCejaRemake.Models;

namespace CasaCejaRemake.Services.Interfaces
{
    /// <summary>
    /// Contrato público del servicio de roles de usuario.
    /// </summary>
    public interface IRoleService
    {
        // ─── Propiedad ────────────────────────────────────────────────────────────
        IReadOnlyList<Role> Roles { get; }

        // ─── Métodos ──────────────────────────────────────────────────────────────
        Task LoadRolesAsync();
        Role? GetByKey(string key);
        Role? GetById(int id);
        int GetAdminRoleId();
        int GetCashierRoleId();
        bool IsAdminRole(int userType);
        bool IsCashierRole(int userType);
        int GetAccessLevel(int userType);
        bool HasAccessLevel(int userType, int requiredLevel);
        string GetRoleName(int userType);
        List<Role> GetAllRoles();
    }
}
