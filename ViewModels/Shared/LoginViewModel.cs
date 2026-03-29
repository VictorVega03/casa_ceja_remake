using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CasaCejaRemake.Services;
using CasaCejaRemake.Models;
using CasaCejaRemake.Data.Repositories;
using System;
using System.Threading.Tasks;

namespace CasaCejaRemake.ViewModels.Shared
{
    public partial class LoginViewModel : ViewModelBase
    {
        private readonly AuthService _authService;
        private readonly ApiClient _apiClient;
        private readonly ConfigService _configService;
        private readonly BaseRepository<User> _userRepo;

        [ObservableProperty]
        private string _username = string.Empty;

        [ObservableProperty]
        private string _password = string.Empty;

        [ObservableProperty]
        private string _errorMessage = string.Empty;

        [ObservableProperty]
        private bool _isLoading = false;

        [ObservableProperty]
        private bool _hasError = false;

        public event EventHandler<LoginSuccessEventArgs>? LoginSuccess;
        public event EventHandler? LoginCancelled;

        public LoginViewModel(
            AuthService authService,
            ApiClient apiClient,
            ConfigService configService,
            BaseRepository<User> userRepo)
        {
            _authService   = authService   ?? throw new ArgumentNullException(nameof(authService));
            _apiClient     = apiClient     ?? throw new ArgumentNullException(nameof(apiClient));
            _configService = configService ?? throw new ArgumentNullException(nameof(configService));
            _userRepo      = userRepo      ?? throw new ArgumentNullException(nameof(userRepo));
        }

        [RelayCommand(CanExecute = nameof(CanLogin))]
        private async Task LoginAsync()
        {
            ErrorMessage = string.Empty;
            HasError     = false;
            IsLoading    = true;

            try
            {
                if (string.IsNullOrWhiteSpace(Username))
                {
                    ShowError("Por favor ingrese su usuario");
                    return;
                }

                if (string.IsNullOrWhiteSpace(Password))
                {
                    ShowError("Por favor ingrese su contraseña");
                    return;
                }

                var username = Username.Trim();
                var password = Password;

                var serverAvailable = await _apiClient.IsServerAvailableAsync();
                Console.WriteLine($"[LoginViewModel] Servidor disponible: {serverAvailable}");

                if (serverAvailable)
                    await LoginWithServerAsync(username, password);
                else
                    await LoginOfflineAsync(username, password);
            }
            catch (Exception ex)
            {
                ShowError($"Error al iniciar sesión: {ex.Message}");
                Console.WriteLine($"[LoginViewModel] Excepción: {ex}");
            }
            finally
            {
                IsLoading = false;
            }
        }

        private async Task LoginWithServerAsync(string username, string password)
        {
            Console.WriteLine($"[LoginViewModel] Intentando login en servidor para '{username}'");

            var response = await _apiClient.LoginAsync(username, password);

            if (response?.IsSuccess != true || response.Data == null)
            {
                Console.WriteLine("[LoginViewModel] Servidor rechazó las credenciales");
                ShowError("Usuario o contraseña incorrectos");
                Password = string.Empty;
                return;
            }

            var serverUser = response.Data.User;
            var token      = response.Data.Token;

            Console.WriteLine($"[LoginViewModel] Servidor aceptó credenciales. Usuario ID={serverUser.Id}");

            await _configService.UpdateAppConfigAsync(config =>
            {
                config.UserToken = token;
            });

            var localUser = await SaveOrUpdateLocalUserAsync(serverUser);

            _authService.SetCurrentUser(localUser);

            Console.WriteLine($"[LoginViewModel] Sesión establecida para '{localUser.Name}'");
            NotifySuccess(isOffline: false);
        }

        private async Task LoginOfflineAsync(string username, string password)
        {
            Console.WriteLine("[LoginViewModel] Sin internet — intentando login local");

            var anyUser = await _userRepo.AnyAsync();
            if (!anyUser)
            {
                ShowError("Sin conexión al servidor. Se requiere internet para el primer inicio de sesión.");
                return;
            }

            var success = await _authService.LoginAsync(username, password);

            if (!success)
            {
                ShowError("Usuario o contraseña incorrectos");
                Password = string.Empty;
                return;
            }

            Console.WriteLine("[LoginViewModel] Login offline exitoso");
            NotifySuccess(isOffline: true);
        }

        private async Task<User> SaveOrUpdateLocalUserAsync(Models.DTOs.LoginUser serverUser)
        {
            var existing = await _userRepo.FirstOrDefaultAsync(u => u.Id == serverUser.Id);

            if (existing != null)
            {
                existing.Name       = serverUser.Name;
                existing.Username   = serverUser.Username;
                existing.UserType   = serverUser.UserType;
                existing.BranchId   = serverUser.BranchId;
                existing.Active     = true;
                existing.SyncStatus = 2;

                if (!string.IsNullOrWhiteSpace(serverUser.Password))
                    existing.Password = serverUser.Password;

                await _userRepo.UpdateAsync(existing);
                Console.WriteLine($"[LoginViewModel] Usuario local actualizado: '{existing.Username}'");
                return existing;
            }
            else
            {
                var newUser = new User
                {
                    Id         = serverUser.Id,
                    Name       = serverUser.Name,
                    Username   = serverUser.Username,
                    Password   = serverUser.Password,
                    UserType   = serverUser.UserType,
                    BranchId   = serverUser.BranchId,
                    Active     = true,
                    SyncStatus = 2,
                    CreatedAt  = DateTime.Now,
                    UpdatedAt  = DateTime.Now,
                };

                await _userRepo.AddAsync(newUser);
                Console.WriteLine($"[LoginViewModel] Usuario local creado: '{newUser.Username}'");
                return newUser;
            }
        }

        private void NotifySuccess(bool isOffline)
        {
            var isAdmin  = _authService.IsAdmin;
            var userName = _authService.CurrentUserName ?? "Usuario";
            LoginSuccess?.Invoke(this, new LoginSuccessEventArgs(isAdmin, userName, isOffline));
            Username = string.Empty;
            Password = string.Empty;
        }

        private void ShowError(string message)
        {
            ErrorMessage = message;
            HasError     = true;
        }

        private bool CanLogin() =>
            !IsLoading &&
            !string.IsNullOrWhiteSpace(Username) &&
            !string.IsNullOrWhiteSpace(Password);

        [RelayCommand]
        private void Cancel() => LoginCancelled?.Invoke(this, EventArgs.Empty);

        [RelayCommand]
        private void ClearFields()
        {
            Username     = string.Empty;
            Password     = string.Empty;
            ErrorMessage = string.Empty;
            HasError     = false;
        }

        partial void OnUsernameChanged(string value) => LoginCommand.NotifyCanExecuteChanged();
        partial void OnPasswordChanged(string value) => LoginCommand.NotifyCanExecuteChanged();
        partial void OnIsLoadingChanged(bool value)  => LoginCommand.NotifyCanExecuteChanged();
    }

    public class LoginSuccessEventArgs : EventArgs
    {
        public bool IsAdmin    { get; }
        public string UserName  { get; }
        public bool IsOffline  { get; }

        public LoginSuccessEventArgs(bool isAdmin, string userName, bool isOffline = false)
        {
            IsAdmin   = isAdmin;
            UserName  = userName;
            IsOffline = isOffline;
        }
    }
}