using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CasaCejaRemake.Data.Repositories;
using CasaCejaRemake.Data.Repositories.Interfaces;
using CasaCejaRemake.Helpers;
using CasaCejaRemake.Models;
using CasaCejaRemake.Services.Interfaces;

namespace CasaCejaRemake.Services
{
    /// <summary>
    /// Servicio para gestión de usuarios (CRUD).
    /// Usado desde el módulo Shared para Admin y POS.
    /// </summary>
    public class UserService : IUserService
    {
        private readonly IUserRepository _userRepository;
        private readonly IRoleService _roleService;

        public UserService(IUserRepository userRepository, IRoleService roleService)
        {
            _userRepository = userRepository ?? throw new ArgumentNullException(nameof(userRepository));
            _roleService = roleService ?? throw new ArgumentNullException(nameof(roleService));
        }

        /// <summary>
        /// Obtiene todos los usuarios activos con nombre de rol resuelto.
        /// </summary>
        public async Task<List<User>> GetAllUsersAsync()
        {
            var users = await _userRepository.FindAsync(u => u.Active);
            foreach (var user in users)
            {
                user.RoleName = _roleService.GetRoleName(user.UserType);
            }
            return users.OrderBy(u => u.Name).ToList();
        }

        /// <summary>
        /// Obtiene solo los cajeros activos (para modo POS).
        /// </summary>
        public async Task<List<User>> GetCashiersAsync()
        {
            var cashierRoleId = _roleService.GetCashierRoleId();
            var users = await _userRepository.FindAsync(u => u.Active && u.UserType == cashierRoleId);
            foreach (var user in users)
            {
                user.RoleName = _roleService.GetRoleName(user.UserType);
            }
            return users.OrderBy(u => u.Name).ToList();
        }

        /// <summary>
        /// Obtiene un usuario por su ID.
        /// </summary>
        public async Task<User?> GetByIdAsync(int id)
        {
            var user = await _userRepository.GetByIdAsync(id);
            if (user != null)
            {
                user.RoleName = _roleService.GetRoleName(user.UserType);
            }
            return user;
        }

        /// <summary>
        /// Crea un nuevo usuario con validaciones.
        /// </summary>
        /// <returns>El usuario creado, o null si hubo error de validación.</returns>
        public async Task<(bool Success, string Message)> CreateUserAsync(User user)
        {
            // Validaciones
            if (string.IsNullOrWhiteSpace(user.Name))
                return (false, "El nombre es requerido.");

            if (string.IsNullOrWhiteSpace(user.Username))
                return (false, "El nombre de usuario es requerido.");

            if (string.IsNullOrWhiteSpace(user.Password))
                return (false, "La contraseña es requerida.");

            if (user.Password.Length < 4)
                return (false, "La contraseña debe tener al menos 4 caracteres.");

            if (string.IsNullOrWhiteSpace(user.Email))
                return (false, "El correo electrónico es requerido.");

            if (string.IsNullOrWhiteSpace(user.Phone))
                return (false, "El teléfono es requerido.");

            // Verificar username único
            if (!await IsUsernameAvailableAsync(user.Username))
                return (false, $"El nombre de usuario '{user.Username}' ya está en uso.");

            // Verificar que el rol exista
            var role = _roleService.GetById(user.UserType);
            if (role == null)
                return (false, "El rol seleccionado no es válido.");

            // Establecer valores por defecto
            user.Active = true;
            user.CreatedAt = DateTime.Now;
            user.UpdatedAt = DateTime.Now;
            user.SyncStatus = 1;

            try
            {
                await _userRepository.AddAsync(user);
                Console.WriteLine($"[UserService] Usuario creado: {user.Username} (Rol: {role.Name})");
                return (true, "Usuario creado exitosamente.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[UserService] Error creando usuario: {ex.Message}");
                return (false, $"Error al crear usuario: {ex.Message}");
            }
        }

        /// <summary>
        /// Actualiza los datos de un usuario existente.
        /// </summary>
        public async Task<(bool Success, string Message)> UpdateUserAsync(User user)
        {
            // Validaciones
            if (string.IsNullOrWhiteSpace(user.Name))
                return (false, "El nombre es requerido.");

            if (string.IsNullOrWhiteSpace(user.Username))
                return (false, "El nombre de usuario es requerido.");

            if (string.IsNullOrWhiteSpace(user.Email))
                return (false, "El correo electrónico es requerido.");

            if (string.IsNullOrWhiteSpace(user.Phone))
                return (false, "El teléfono es requerido.");

            // Verificar username único (excluyendo el usuario actual)
            if (!await IsUsernameAvailableAsync(user.Username, user.Id))
                return (false, $"El nombre de usuario '{user.Username}' ya está en uso.");

            // Verificar que el rol exista
            var role = _roleService.GetById(user.UserType);
            if (role == null)
                return (false, "El rol seleccionado no es válido.");

            user.UpdatedAt = DateTime.Now;

            try
            {
                await _userRepository.UpdateAsync(user);
                Console.WriteLine($"[UserService] Usuario actualizado: {user.Username}");
                return (true, "Usuario actualizado exitosamente.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[UserService] Error actualizando usuario: {ex.Message}");
                return (false, $"Error al actualizar usuario: {ex.Message}");
            }
        }

        /// <summary>
        /// Desactiva un usuario (soft delete). Solo Admin.
        /// </summary>
        public async Task<(bool Success, string Message)> DeactivateUserAsync(int userId)
        {
            var user = await _userRepository.GetByIdAsync(userId);
            if (user == null)
                return (false, "Usuario no encontrado.");

            // No permitir desactivar al último administrador
            var adminRoleId = _roleService.GetAdminRoleId();
            if (user.UserType == adminRoleId)
            {
                var adminCount = await _userRepository.CountAsync(u => u.Active && u.UserType == adminRoleId);
                if (adminCount <= 1)
                    return (false, "No se puede desactivar al último administrador del sistema.");
            }

            user.Active = false;
            user.UpdatedAt = DateTime.Now;

            try
            {
                await _userRepository.UpdateAsync(user);
                Console.WriteLine($"[UserService] Usuario desactivado: {user.Username}");
                return (true, $"Usuario '{user.Name}' desactivado exitosamente.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[UserService] Error desactivando usuario: {ex.Message}");
                return (false, $"Error al desactivar usuario: {ex.Message}");
            }
        }

        /// <summary>
        /// Verifica si un nombre de usuario está disponible.
        /// </summary>
        /// <param name="username">Nombre de usuario a verificar.</param>
        /// <param name="excludeUserId">ID de usuario a excluir (para edición).</param>
        public async Task<bool> IsUsernameAvailableAsync(string username, int? excludeUserId = null)
        {
            var existing = await _userRepository.FirstOrDefaultAsync(u =>
                u.Username == username && u.Active);

            if (existing == null) return true;
            if (excludeUserId.HasValue && existing.Id == excludeUserId.Value) return true;

            return false;
        }

        /// <summary>
        /// Obtiene todos los roles disponibles.
        /// </summary>
        public List<Role> GetAvailableRoles()
        {
            return _roleService.GetAllRoles();
        }

        /// <summary>
        /// Obtiene el ID del rol de cajero.
        /// </summary>
        public int GetCashierRoleId()
        {
            return _roleService.GetCashierRoleId();
        }

        /// <summary>
        /// Verifica si un usuario es administrador.
        /// </summary>
        public async Task<bool> IsAdminAsync(int userId)
        {
            var user = await _userRepository.GetByIdAsync(userId);
            if (user == null) return false;
            return user.UserType == _roleService.GetAdminRoleId();
        }

        /// <summary>
        /// Autentica un usuario sin modificar la sesión actual.
        /// Usado para verificación de credenciales de administrador.
        /// </summary>
        public async Task<(bool Success, User? User)> AuthenticateAsync(string username, string password)
        {
            var user = await _userRepository.FirstOrDefaultAsync(u =>
                u.Username == username && u.Active);

            if (user == null)
                return (false, null);

            if (user.Password != password)
                return (false, null);

            return (true, user);
        }
    }
}
