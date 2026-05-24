using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CasaCejaRemake.Data.Repositories;
using CasaCejaRemake.Helpers;
using CasaCejaRemake.Models;

namespace CasaCejaRemake.Services
{
    
    /// Servicio para gestión de usuarios (CRUD).
    /// Usado desde el módulo Shared para Admin y POS.
    
    public class UserService
    {
        private readonly IRepository<User> _userRepository;
        private readonly RoleService _roleService;
        private readonly SyncService? _syncService;
        private readonly ApiClient? _apiClient;

        public UserService(IRepository<User> userRepository, RoleService roleService, SyncService? syncService = null, ApiClient? apiClient = null)
        {
            _userRepository = userRepository ?? throw new ArgumentNullException(nameof(userRepository));
            _roleService    = roleService    ?? throw new ArgumentNullException(nameof(roleService));
            _syncService    = syncService;
            _apiClient      = apiClient;
        }

        public ApiClient? ApiClient => _apiClient;

        
        /// Obtiene todos los usuarios activos con nombre de rol resuelto.
        
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
        /// Obtiene los cajeros activos de una sucursal específica (para modo POS).
        /// Filtra por branch_id = la sucursal donde fue creado el cajero.
        /// </summary>
        public async Task<List<User>> GetCashiersAsync(int branchId)
        {
            var cashierRoleId = _roleService.GetCashierRoleId();
            var users = await _userRepository.FindAsync(u =>
                u.Active && u.UserType == cashierRoleId && u.BranchId == branchId);
            foreach (var user in users)
            {
                user.RoleName = _roleService.GetRoleName(user.UserType);
            }
            return users.OrderBy(u => u.Name).ToList();
        }

        
        /// Obtiene un usuario por su ID.
        
        public async Task<User?> GetByIdAsync(int id)
        {
            var user = await _userRepository.GetByIdAsync(id);
            if (user != null)
            {
                user.RoleName = _roleService.GetRoleName(user.UserType);
            }
            return user;
        }

        
        /// Crea un nuevo usuario con validaciones.
        
        /// <returns>El usuario creado, o null si hubo error de validación.</returns>
        public async Task<(bool Success, string Message)> CreateUserAsync(User user, bool isAdminMode = false)
        {
            // Validaciones locales
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

            // Verificar username único localmente
            if (!await IsUsernameAvailableAsync(user.Username))
                return (false, $"El nombre de usuario '{user.Username}' ya está en uso.");

            // Verificar que el rol exista
            var role = _roleService.GetById(user.UserType);
            if (role == null)
                return (false, "El rol seleccionado no es válido.");

            if (isAdminMode)
            {
                if (_apiClient == null)
                    return (false, "Sin conexión al servidor. Verifica tu red e intenta de nuevo.");

                // En modo admin: enviar contraseña en texto plano — el servidor hashea
                var plainPassword = user.Password;
                var payload = new
                {
                    name      = user.Name,
                    email     = user.Email,
                    phone     = user.Phone,
                    username  = user.Username,
                    password  = plainPassword,
                    user_type = user.UserType,
                    branch_id = user.BranchId,
                    active    = true,
                };

                try
                {
                    var response = await _apiClient.PostAsync<User>("/api/v1/admin/users", payload);
                    if (response.IsNetworkError)
                        return (false, "Sin conexión al servidor. Verifica tu red e intenta de nuevo.");

                    if (response.IsServerError)
                        return (false, response.ServerMessage ?? "No se pudo crear el usuario en el servidor.");

                    if (!response.IsSuccess || response.Data == null)
                        return (false, response.ServerMessage ?? "No se pudo crear el usuario en el servidor.");

                    // Servidor confirmó — guardar local con Id del servidor y contraseña hasheada
                    user.Id         = response.Data.Id;
                    user.Active     = true;
                    user.CreatedAt  = DateTime.Now;
                    user.UpdatedAt  = DateTime.Now;
                    user.SyncStatus = 2;
                    user.Password   = BCrypt.Net.BCrypt.HashPassword(plainPassword);

                    if (_userRepository is Data.Repositories.BaseRepository<User> baseRepo)
                        await baseRepo.UpsertAsync(user);
                    else
                        await _userRepository.AddAsync(user);

                    Console.WriteLine($"[UserService] Usuario admin creado: {user.Username} (Id servidor: {user.Id})");
                    return (true, "Usuario creado exitosamente.");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[UserService] Error creando usuario admin: {ex.Message}");
                    return (false, $"Error al crear usuario: {ex.Message}");
                }
            }
            else
            {
                // Modo offline-first: guardar local con SyncStatus=1, luego push
                user.Active     = true;
                user.CreatedAt  = DateTime.Now;
                user.UpdatedAt  = DateTime.Now;
                user.SyncStatus = 1;
                user.Password   = BCrypt.Net.BCrypt.HashPassword(user.Password);

                try
                {
                    await _userRepository.AddAsync(user);
                    Console.WriteLine($"[UserService] Usuario creado: {user.Username} (Rol: {role.Name})");

                    if (_syncService != null)
                        _ = Task.Run(() => _syncService.PushUserAsync(user));

                    return (true, "Usuario creado exitosamente.");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[UserService] Error creando usuario: {ex.Message}");
                    return (false, $"Error al crear usuario: {ex.Message}");
                }
            }
        }

        
        /// Actualiza los datos de un usuario existente.
        
        public async Task<(bool Success, string Message)> UpdateUserAsync(User user, bool isAdminMode = false, string? newPlainPassword = null)
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

            if (isAdminMode)
            {
                if (_apiClient == null)
                    return (false, "Sin conexión al servidor. Verifica tu red e intenta de nuevo.");

                try
                {
                    var existingLocalUser = await _userRepository.GetByIdAsync(user.Id);

                    // PUT campos del usuario (sin contraseña)
                    var putPayload = new
                    {
                        name      = user.Name,
                        email     = user.Email,
                        phone     = user.Phone,
                        username  = user.Username,
                        user_type = user.UserType,
                        branch_id = user.BranchId,
                        active    = user.Active,
                    };

                    var putResponse = await _apiClient.PutAsync<User>($"/api/v1/admin/users/{user.Id}", putPayload);
                    if (putResponse.IsNetworkError)
                        return (false, "Sin conexión al servidor. Verifica tu red e intenta de nuevo.");

                    if (putResponse.IsServerError)
                        return (false, putResponse.ServerMessage ?? "No se pudo actualizar el usuario en el servidor.");

                    if (!putResponse.IsSuccess)
                        return (false, putResponse.ServerMessage ?? "No se pudo actualizar el usuario en el servidor.");

                    // Si se proporcionó nueva contraseña, cambiarla vía PATCH
                    if (!string.IsNullOrWhiteSpace(newPlainPassword))
                    {
                        var patchPayload = new { password = newPlainPassword, password_confirmation = newPlainPassword };
                        var patchResponse = await _apiClient.PatchAsync<object>($"/api/v1/admin/users/{user.Id}/password", patchPayload);
                        if (patchResponse?.IsSuccess != true)
                            return (false, "Usuario actualizado, pero no se pudo cambiar la contraseña en el servidor.");

                        user.Password = BCrypt.Net.BCrypt.HashPassword(newPlainPassword);
                    }
                    else if (existingLocalUser != null)
                    {
                        user.Password = existingLocalUser.Password;
                    }

                    user.SyncStatus = 2;
                    await _userRepository.UpdateAsync(user);
                    Console.WriteLine($"[UserService] Usuario admin actualizado: {user.Username}");
                    return (true, "Usuario actualizado exitosamente.");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[UserService] Error actualizando usuario admin: {ex.Message}");
                    return (false, $"Error al actualizar usuario: {ex.Message}");
                }
            }
            else
            {
                try
                {
                    user.SyncStatus = 1;
                    await _userRepository.UpdateAsync(user);
                    Console.WriteLine($"[UserService] Usuario actualizado: {user.Username}");

                    if (_syncService != null)
                        _ = Task.Run(() => _syncService.PushUserAsync(user));

                    return (true, "Usuario actualizado exitosamente.");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[UserService] Error actualizando usuario: {ex.Message}");
                    return (false, $"Error al actualizar usuario: {ex.Message}");
                }
            }
        }

        
        /// Desactiva un usuario (soft delete). Solo Admin.
        
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

        
        /// Verifica si un nombre de usuario está disponible.
        
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

        
        /// Obtiene todos los roles disponibles.
        
        public List<Role> GetAvailableRoles()
        {
            return _roleService.GetAllRoles();
        }

        
        /// Obtiene el ID del rol de cajero.
        
        public int GetCashierRoleId()
        {
            return _roleService.GetCashierRoleId();
        }

        /// Verifica si un usuario es administrador.
        public async Task<bool> IsAdminAsync(int userId)
        {
            var user = await _userRepository.GetByIdAsync(userId);
            if (user == null) return false;
            return user.UserType == _roleService.GetAdminRoleId();
        }

        /// Autentica un usuario sin modificar la sesión actual.
        /// Usado para verificación de credenciales de administrador.
       // DESPUÉS
        public async Task<(bool Success, User? User)> AuthenticateAsync(string username, string password)
        {
            var user = await _userRepository.FirstOrDefaultAsync(u =>
                u.Username == username && u.Active);

            if (user == null)
                return (false, null);

            // Verificar con BCrypt — con fallback a texto plano para migración
            bool passwordValid = false;
            try
            {
                passwordValid = BCrypt.Net.BCrypt.Verify(password, user.Password);
            }
            catch
            {
                passwordValid = user.Password == password;
            }

            if (!passwordValid)
                return (false, null);

            return (true, user);
        }
    }
}
