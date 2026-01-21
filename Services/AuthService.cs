using CasaCejaRemake.Data.Repositories;
using CasaCejaRemake.Models;
using System;
using System.Threading.Tasks;

namespace CasaCejaRemake.Services
{
    /// <summary>
    /// Servicio de autenticación para validar usuarios y gestionar sesión
    /// </summary>
    public class AuthService
    {
        private readonly IRepository<User> _userRepository;

        /// <summary>
        /// Usuario actualmente autenticado
        /// </summary>
        public User? CurrentUser { get; private set; }

        /// <summary>
        /// Indica si hay un usuario autenticado
        /// </summary>
        public bool IsAuthenticated => CurrentUser != null;

        /// <summary>
        /// Indica si el usuario actual es Admin (Nivel 1)
        /// </summary>
        public bool IsAdmin => CurrentUser?.UserType == 1;

        /// <summary>
        /// Indica si el usuario actual es Cajero (Nivel 2)
        /// </summary>
        public bool IsCajero => CurrentUser?.UserType == 2;

        /// <summary>
        /// Nombre del usuario actual
        /// </summary>
        public string? CurrentUserName => CurrentUser?.Name;

        /// <summary>
        /// Sucursal del usuario actual
        /// </summary>
        public int? CurrentBranchId => CurrentUser?.BranchId;

        /// <summary>
        /// Constructor que inyecta el repositorio de usuarios
        /// </summary>
        /// <param name="userRepository">Repositorio de usuarios</param>
        public AuthService(IRepository<User> userRepository)
        {
            _userRepository = userRepository ?? throw new ArgumentNullException(nameof(userRepository));
        }

        /// <summary>
        /// Intenta autenticar un usuario con sus credenciales
        /// </summary>
        /// <param name="username">Nombre de usuario</param>
        /// <param name="password">Contraseña</param>
        /// <returns>True si la autenticación fue exitosa, False en caso contrario</returns>
        public async Task<bool> LoginAsync(string username, string password)
        {
            Console.WriteLine($"🔐 Intentando login con usuario: '{username}'");
            
            // Validar parámetros
            if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password))
            {
                Console.WriteLine("❌ Usuario o contraseña vacíos");
                return false;
            }

            try
            {
                Console.WriteLine("📊 Buscando usuario en base de datos...");
                // Buscar usuario por nombre de usuario, contraseña y que esté activo
                var user = await _userRepository.FirstOrDefaultAsync(u =>
                    u.Username == username &&
                    u.Password == password &&
                    u.Active
                );

                if (user != null)
                {
                    Console.WriteLine($"✅ Usuario encontrado: {user.Name} (Tipo: {user.UserType})");
                    // Autenticación exitosa
                    CurrentUser = user;
                    
                    // Registrar el último login (opcional)
                    user.UpdatedAt = DateTime.Now;
                    await _userRepository.UpdateAsync(user);
                    
                    return true;
                }

                // Credenciales inválidas
                Console.WriteLine("❌ Credenciales inválidas - Usuario no encontrado");
                return false;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Error en login: {ex.Message}");
                // En caso de error, denegar acceso
                return false;
            }
        }

        /// <summary>
        /// Cierra la sesión del usuario actual
        /// </summary>
        public void Logout()
        {
            CurrentUser = null;
        }

        /// <summary>
        /// Valida si el usuario actual tiene el nivel de acceso requerido
        /// </summary>
        /// <param name="requiredLevel">Nivel requerido (1=Admin, 2=Cajero)</param>
        /// <returns>True si tiene el nivel requerido o superior</returns>
        public bool HasAccessLevel(int requiredLevel)
        {
            if (!IsAuthenticated)
                return false;

            // Admin (nivel 1) tiene acceso a todo
            // Cajero (nivel 2) solo a su nivel
            return CurrentUser!.UserType <= requiredLevel;
        }

        /// <summary>
        /// Obtiene la configuración de caja del usuario actual
        /// </summary>
        /// <returns>ID de la caja del usuario o null si no está configurada</returns>
        public string? GetUserCashRegisterId()
        {
            // Aquí podrías implementar lógica para obtener la caja asignada
            // Por ahora devuelve null, se implementará en fases posteriores
            return null;
        }

        /// <summary>
        /// Verifica si el usuario tiene permisos para la sucursal especificada
        /// </summary>
        /// <param name="branchId">ID de la sucursal</param>
        /// <returns>True si tiene acceso, False en caso contrario</returns>
        public bool HasBranchAccess(int branchId)
        {
            if (!IsAuthenticated)
                return false;

            // Admin tiene acceso a todas las sucursales
            if (IsAdmin)
                return true;

            // Cajero solo a su sucursal asignada
            return CurrentUser!.BranchId == branchId;
        }
    }
}