using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CasaCejaRemake.Data;
using CasaCejaRemake.Helpers;
using CasaCejaRemake.Models;
using CasaCejaRemake.Services.Interfaces;

namespace CasaCejaRemake.Services
{
    /// <summary>
    /// Servicio para gestión de roles de usuario.
    /// Carga los roles desde la base de datos y los mantiene en memoria
    /// para consultas rápidas. Reemplaza las constantes estáticas.
    /// </summary>
    public class RoleService : IRoleService
    {
        private readonly DatabaseService _databaseService;
        private List<Role> _roles = new();

        /// <summary>Roles cargados en memoria.</summary>
        public IReadOnlyList<Role> Roles => _roles.AsReadOnly();

        public RoleService(DatabaseService databaseService)
        {
            _databaseService = databaseService;
        }

        /// <summary>
        /// Carga todos los roles activos desde la base de datos.
        /// Llamar una vez al iniciar la aplicación.
        /// </summary>
        public async Task LoadRolesAsync()
        {
            try
            {
                var allRoles = await _databaseService.Table<Role>()
                    .Where(r => r.Active)
                    .ToListAsync();

                _roles = allRoles;
                Console.WriteLine($"[RoleService] {_roles.Count} roles cargados desde la BD");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[RoleService] Error cargando roles: {ex.Message}");
                _roles = new List<Role>();
            }
        }

        /// <summary>
        /// Obtiene un rol por su clave interna (ej: "admin", "cashier").
        /// </summary>
        public Role? GetByKey(string key)
        {
            return _roles.FirstOrDefault(r =>
                r.Key.Equals(key, StringComparison.OrdinalIgnoreCase));
        }

        /// <summary>
        /// Obtiene un rol por su ID.
        /// </summary>
        public Role? GetById(int id)
        {
            return _roles.FirstOrDefault(r => r.Id == id);
        }

        /// <summary>
        /// Obtiene el ID del rol de administrador.
        /// </summary>
        public int GetAdminRoleId()
        {
            return GetByKey(Constants.ROLE_ADMIN_KEY)?.Id ?? 1;
        }

        /// <summary>
        /// Obtiene el ID del rol de cajero.
        /// </summary>
        public int GetCashierRoleId()
        {
            return GetByKey(Constants.ROLE_CASHIER_KEY)?.Id ?? 2;
        }

        /// <summary>
        /// Verifica si un UserType (role ID) corresponde al rol de admin.
        /// </summary>
        public bool IsAdminRole(int userType)
        {
            var role = GetById(userType);
            if (role == null) return false;
            return role.Key.Equals(Constants.ROLE_ADMIN_KEY, StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>
        /// Verifica si un UserType (role ID) corresponde al rol de cajero.
        /// </summary>
        public bool IsCashierRole(int userType)
        {
            var role = GetById(userType);
            if (role == null) return false;
            return role.Key.Equals(Constants.ROLE_CASHIER_KEY, StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>
        /// Obtiene el nivel de acceso de un rol dado su ID.
        /// Menor número = mayor acceso.
        /// </summary>
        public int GetAccessLevel(int userType)
        {
            var role = GetById(userType);
            return role?.AccessLevel ?? int.MaxValue;
        }

        /// <summary>
        /// Verifica si un userType tiene acceso al nivel requerido.
        /// El acceso se cumple si el nivel del usuario es menor o igual al requerido.
        /// </summary>
        public bool HasAccessLevel(int userType, int requiredLevel)
        {
            return GetAccessLevel(userType) <= requiredLevel;
        }

        /// <summary>
        /// Obtiene el nombre legible del rol dado su ID.
        /// </summary>
        public string GetRoleName(int userType)
        {
            var role = GetById(userType);
            return role?.Name ?? "Desconocido";
        }

        /// <summary>
        /// Obtiene todos los roles activos (útil para UI de administración).
        /// </summary>
        public List<Role> GetAllRoles()
        {
            return _roles.ToList();
        }
    }
}
