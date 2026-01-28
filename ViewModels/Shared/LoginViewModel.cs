using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CasaCejaRemake.Services;
using System;
using System.Threading.Tasks;

namespace CasaCejaRemake.ViewModels.Shared
{
    /// <summary>
    /// ViewModel para la pantalla de login
    /// </summary>
    public partial class LoginViewModel : ViewModelBase
    {
        private readonly AuthService _authService;

        // ====================
        // PROPIEDADES OBSERVABLES
        // ====================

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

        // ====================
        // EVENTOS
        // ====================

        /// <summary>
        /// Evento que se dispara cuando el login es exitoso
        /// </summary>
        public event EventHandler<LoginSuccessEventArgs>? LoginSuccess;

        /// <summary>
        /// Evento que se dispara cuando se cancela el login
        /// </summary>
        public event EventHandler? LoginCancelled;

        // ====================
        // CONSTRUCTOR
        // ====================

        public LoginViewModel(AuthService authService)
        {
            _authService = authService ?? throw new ArgumentNullException(nameof(authService));
        }

        // ====================
        // COMANDOS
        // ====================

        /// <summary>
        /// Comando para ejecutar el login
        /// </summary>
        [RelayCommand(CanExecute = nameof(CanLogin))]
        private async Task LoginAsync()
        {
            // Limpiar errores previos
            ErrorMessage = string.Empty;
            HasError = false;
            IsLoading = true;

            try
            {
                // Validar entrada
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

                // Intentar login
                var success = await _authService.LoginAsync(Username.Trim(), Password);

                if (success)
                {
                    // Login exitoso
                    var isAdmin = _authService.IsAdmin;
                    var userName = _authService.CurrentUserName ?? "Usuario";

                    Console.WriteLine($"[LoginViewModel] Login exitoso para {userName}, disparando evento LoginSuccess...");
                    
                    // Disparar evento de éxito
                    LoginSuccess?.Invoke(this, new LoginSuccessEventArgs(isAdmin, userName));

                    Console.WriteLine("[LoginViewModel] Evento LoginSuccess disparado");

                    // Limpiar campos
                    Username = string.Empty;
                    Password = string.Empty;
                }
                else
                {
                    // Credenciales incorrectas
                    ShowError("Usuario o contraseña incorrectos");
                    Password = string.Empty; // Limpiar solo contraseña
                }
            }
            catch (Exception ex)
            {
                ShowError($"Error al iniciar sesión: {ex.Message}");
            }
            finally
            {
                IsLoading = false;
            }
        }

        /// <summary>
        /// Determina si el comando Login puede ejecutarse
        /// </summary>
        private bool CanLogin()
        {
            return !IsLoading &&
                   !string.IsNullOrWhiteSpace(Username) &&
                   !string.IsNullOrWhiteSpace(Password);
        }

        /// <summary>
        /// Comando para cancelar el login y cerrar la aplicación
        /// </summary>
        [RelayCommand]
        private void Cancel()
        {
            // Disparar evento de cancelación
            LoginCancelled?.Invoke(this, EventArgs.Empty);
        }

        /// <summary>
        /// Comando para limpiar los campos
        /// </summary>
        [RelayCommand]
        private void ClearFields()
        {
            Username = string.Empty;
            Password = string.Empty;
            ErrorMessage = string.Empty;
            HasError = false;
        }

        // ====================
        // MÉTODOS PRIVADOS
        // ====================

        /// <summary>
        /// Muestra un mensaje de error
        /// </summary>
        private void ShowError(string message)
        {
            ErrorMessage = message;
            HasError = true;
        }

        // ====================
        // MÉTODOS PARCIALES (para CommunityToolkit.Mvvm)
        // ====================

        /// <summary>
        /// Se ejecuta cuando Username cambia - actualiza CanExecute del LoginCommand
        /// </summary>
        partial void OnUsernameChanged(string value)
        {
            LoginCommand.NotifyCanExecuteChanged();
        }

        /// <summary>
        /// Se ejecuta cuando Password cambia - actualiza CanExecute del LoginCommand
        /// </summary>
        partial void OnPasswordChanged(string value)
        {
            LoginCommand.NotifyCanExecuteChanged();
        }

        /// <summary>
        /// Se ejecuta cuando IsLoading cambia - actualiza CanExecute del LoginCommand
        /// </summary>
        partial void OnIsLoadingChanged(bool value)
        {
            LoginCommand.NotifyCanExecuteChanged();
        }
    }

    // ====================
    // EVENT ARGS
    // ====================

    /// <summary>
    /// Argumentos del evento de login exitoso
    /// </summary>
    public class LoginSuccessEventArgs : EventArgs
    {
        public bool IsAdmin { get; }
        public string UserName { get; }

        public LoginSuccessEventArgs(bool isAdmin, string userName)
        {
            IsAdmin = isAdmin;
            UserName = userName;
        }
    }
}