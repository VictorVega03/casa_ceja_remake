using CasaCejaRemake.Data.Repositories;
using CasaCejaRemake.Data.Repositories.Interfaces;
using CasaCejaRemake.Models;
using CasaCejaRemake.Services.Interfaces;
using System;
using System.Threading.Tasks;

namespace CasaCejaRemake.Services
{
    public class AuthService : IAuthService
    {
        private readonly IUserRepository _userRepository;
        private readonly IRoleService _roleService;
        private int _currentBranchId = 1;

        public User? CurrentUser { get; private set; }
        public bool IsAuthenticated => CurrentUser != null;

        /// <summary>Verifica si el usuario actual es Admin usando roles dinámicos de la BD.</summary>
        public bool IsAdmin => CurrentUser != null && _roleService.IsAdminRole(CurrentUser.UserType);

        /// <summary>Verifica si el usuario actual es Cajero usando roles dinámicos de la BD.</summary>
        public bool IsCajero => CurrentUser != null && _roleService.IsCashierRole(CurrentUser.UserType);

        public string? CurrentUserName => CurrentUser?.Name;

        /// <summary>Servicio de roles (expuesto para que otros servicios puedan consultarlo).</summary>
        public RoleService RoleService => (RoleService)_roleService;

        /// <summary>
        /// Sucursal actual: Siempre usa _currentBranchId que se sincroniza con ConfigService.
        /// La verificación de permisos para cambiar sucursal se hace en otro lugar.
        /// </summary>
        public int CurrentBranchId 
        { 
            get => _currentBranchId;
            private set => _currentBranchId = value;
        }

        public event EventHandler<User>? UserLoggedIn;
        public event EventHandler? UserLoggedOut;

        public AuthService(IUserRepository userRepository, IRoleService roleService)
        {
            _userRepository = userRepository ?? throw new ArgumentNullException(nameof(userRepository));
            _roleService = roleService ?? throw new ArgumentNullException(nameof(roleService));
        }

        public async Task<bool> LoginAsync(string username, string password)
        {
            Console.WriteLine($"Intentando login con usuario: '{username}'");
            
            if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password))
            {
                Console.WriteLine("Usuario o contraseña vacíos");
                return false;
            }

            try
            {
                Console.WriteLine("Buscando usuario en base de datos...");
                var user = await _userRepository.FirstOrDefaultAsync(u =>
                    u.Username == username &&
                    u.Password == password &&
                    u.Active
                );

                if (user != null)
                {
                    Console.WriteLine($"Usuario encontrado: {user.Name} (Tipo: {user.UserType})");
                    // Autenticación exitosa
                    CurrentUser = user;
                    
                    // NO establecer _currentBranchId aquí - se sincroniza desde ConfigService en HandleSuccessfulLogin
                    
                    // Registrar el último login (opcional)
                    user.UpdatedAt = DateTime.Now;
                    await _userRepository.UpdateAsync(user);
                    
                    // Disparar evento de login
                    UserLoggedIn?.Invoke(this, user);
                    
                    return true;
                }

                Console.WriteLine("Credenciales inválidas - Usuario no encontrado");
                return false;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error en login: {ex.Message}");
                // En caso de error, denegar acceso
                return false;
            }
        }

        public void Logout()
        {
            CurrentUser = null;
            _currentBranchId = 1;
            
            // Disparar evento de logout
            UserLoggedOut?.Invoke(this, EventArgs.Empty);
        }

        public void SetCurrentUser(User user)
        {
            if (user == null) throw new ArgumentNullException(nameof(user));
            
            CurrentUser = user;
            _currentBranchId = user.BranchId ?? 1;
            
            // Disparar evento de login
            UserLoggedIn?.Invoke(this, user);
        }

        public bool SetCurrentBranch(int branchId)
        {
            _currentBranchId = branchId;
            Console.WriteLine($"[AuthService] Sucursal cambiada a: {branchId}");
            return true;
        }

        public bool HasAccessLevel(int requiredLevel)
        {
            if (!IsAuthenticated)
                return false;

            // Usa el nivel de acceso del rol desde la BD
            return _roleService.HasAccessLevel(CurrentUser!.UserType, requiredLevel);
        }

        /// <summary>
        /// Obtiene el nombre del rol del usuario actual.
        /// </summary>
        public string GetCurrentRoleName()
        {
            if (!IsAuthenticated) return "Sin sesión";
            return _roleService.GetRoleName(CurrentUser!.UserType);
        }

        public string? GetUserCashRegisterId()
        {
            // Aquí podrías implementar lógica para obtener la caja asignada
            // Por ahora devuelve null, se implementará en fases posteriores
            return null;
        }

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