using CasaCejaRemake.Data.Repositories;
using CasaCejaRemake.Models;
using System;
using System.Threading.Tasks;

namespace CasaCejaRemake.Services
{
    public class AuthService
    {
        private readonly IRepository<User> _userRepository;
        private int _currentBranchId = 1;

        public User? CurrentUser { get; private set; }
        public bool IsAuthenticated => CurrentUser != null;
        public bool IsAdmin => CurrentUser?.UserType == 1;
        public bool IsCajero => CurrentUser?.UserType == 2;
        public string? CurrentUserName => CurrentUser?.Name;

        public int CurrentBranchId 
        { 
            get => IsAdmin ? _currentBranchId : (CurrentUser?.BranchId ?? 1);
            private set => _currentBranchId = value;
        }

        public event EventHandler<User>? UserLoggedIn;
        public event EventHandler? UserLoggedOut;

        public AuthService(IRepository<User> userRepository)
        {
            _userRepository = userRepository ?? throw new ArgumentNullException(nameof(userRepository));
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
                    
                    // Establecer sucursal inicial
                    _currentBranchId = user.BranchId ?? 1;
                    
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
            if (!IsAdmin)
            {
                Console.WriteLine("Solo Admin puede cambiar de sucursal");
                return false;
            }

            _currentBranchId = branchId;
            Console.WriteLine($"Sucursal cambiada a: {branchId}");
            return true;
        }

        public bool HasAccessLevel(int requiredLevel)
        {
            if (!IsAuthenticated)
                return false;

            // Admin (nivel 1) tiene acceso a todo
            // Cajero (nivel 2) solo a su nivel
            return CurrentUser!.UserType <= requiredLevel;
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