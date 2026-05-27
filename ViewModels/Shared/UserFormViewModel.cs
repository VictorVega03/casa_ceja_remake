using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CasaCejaRemake.Models;
using CasaCejaRemake.Services;

namespace CasaCejaRemake.ViewModels.Shared
{
    /// <summary>
    /// ViewModel para el formulario de crear/editar usuario.
    /// En modo POS, el rol se fuerza a Cajero y no es editable.
    /// En modo Admin, se puede seleccionar cualquier rol.
    /// </summary>
    public partial class UserFormViewModel : ViewModelBase
    {
        private readonly UserService _userService;
        private readonly int _branchId;
        private readonly bool _isAdminMode;
        private readonly User? _existingUser;
        private readonly ApiClient? _apiClient;
        private Avalonia.Controls.Window? _parentWindow;
        private bool _passwordActivated = false;

        // ============ CAMPOS DEL FORMULARIO ============

        [ObservableProperty] private string _name = string.Empty;
        [ObservableProperty] private string _email = string.Empty;
        [ObservableProperty] private string _phone = string.Empty;
        [ObservableProperty] private string _username = string.Empty;
        [ObservableProperty] private string _password = string.Empty;
        [ObservableProperty] private string _confirmPassword = string.Empty;
        [ObservableProperty] private Role? _selectedRole;
        [ObservableProperty] private ObservableCollection<Role> _availableRoles = new();

        // ============ ESTADO ============

        [ObservableProperty] private bool _isEditing;
        [ObservableProperty] private string _statusMessage = string.Empty;
        [ObservableProperty] private bool _hasError;
        [ObservableProperty] private bool _isSaving;
        [ObservableProperty] private bool _isReadOnly;

        // ============ VISIBILIDAD ============

        /// <summary>El selector de rol solo es visible en modo Admin.</summary>
        public bool ShowRoleSelector => _isAdminMode;

        /// <summary>La sección de contraseña ahora siempre es visible.</summary>
        public bool ShowPasswordSection => true;

        /// <summary>Editable al crear, o al editar en modo admin.</summary>
        public bool IsPasswordEnabled => !IsEditing || _isAdminMode;

        /// <summary>Watermark según contexto de edición y modo.</summary>
        public string PasswordWatermark => (IsEditing && !_isAdminMode)
            ? "Bloqueado, solo disponible en modulo Administrador"
            : (IsEditing ? "Vacío = sin cambios (mín. 4 chars)" : "Mínimo 4 caracteres");

        /// <summary>Título dinámico del formulario.</summary>
        public string FormTitle => IsReadOnly
            ? "Detalle de Usuario"
            : (IsEditing
                ? "Editar Usuario"
                : (_isAdminMode ? "Nuevo Usuario" : "Nuevo Cajero"));

        /// <summary>Icono del formulario.</summary>
        public string FormIcon => IsReadOnly ? "�" : (IsEditing ? "✏️" : "➕");

        // ============ EVENTOS ============
        public event EventHandler? CloseRequested;
        public event EventHandler? SaveCompleted;

        public UserFormViewModel(UserService userService, bool isAdminMode, int branchId = 0, User? existingUser = null, bool isReadOnly = false)
        {
            _userService = userService ?? throw new ArgumentNullException(nameof(userService));
            _branchId = branchId;
            _isAdminMode = isAdminMode;
            _existingUser = existingUser;
            _apiClient = userService.ApiClient;
            _isEditing = existingUser != null;
            _isReadOnly = isReadOnly;

            LoadRoles();

            if (_existingUser != null)
            {
                PopulateFromUser(_existingUser);
            }
        }

        public void SetParentWindow(Avalonia.Controls.Window parentWindow)
        {
            _parentWindow = parentWindow;
        }

        /// <summary>
        /// Llamado cuando el usuario hace clic/focus en el campo de contraseña en modo edición admin.
        /// Limpia ambos campos la primera vez para que pueda escribir una nueva contraseña.
        /// </summary>
        public void OnPasswordFieldFocused()
        {
            if (IsEditing && _isAdminMode && !_passwordActivated)
            {
                Password = string.Empty;
                ConfirmPassword = string.Empty;
                _passwordActivated = true;
            }
        }

        private void LoadRoles()
        {
            var roles = _userService.GetAvailableRoles();
            AvailableRoles = new ObservableCollection<Role>(roles);

            if (_isAdminMode)
            {
                // En modo admin: seleccionar el rol del usuario si existe, o el primero
                if (_existingUser != null)
                {
                    SelectedRole = AvailableRoles.FirstOrDefault(r => r.Id == _existingUser.UserType)
                                   ?? AvailableRoles.FirstOrDefault();
                }
                else
                {
                    // Por defecto seleccionar "Cajero" al crear
                    var cashierRoleId = _userService.GetCashierRoleId();
                    SelectedRole = AvailableRoles.FirstOrDefault(r => r.Id == cashierRoleId)
                                   ?? AvailableRoles.FirstOrDefault();
                }
            }
            else
            {
                // En modo POS: forzar rol de cajero
                var cashierRoleId = _userService.GetCashierRoleId();
                SelectedRole = AvailableRoles.FirstOrDefault(r => r.Id == cashierRoleId);
            }
        }

        private void PopulateFromUser(User user)
        {
            Console.WriteLine($"[UserFormVM] PopulateFromUser — user.Name='{user.Name}', user.Username='{user.Username}'");

            Name = user.Name;
            Email = user.Email;
            Phone = user.Phone;
            Username = user.Username;

            if (_isEditing)
            {
                // 8 chars = 8 puntos visuales. En admin, se limpia al hacer clic (OnPasswordFieldFocused).
                // En POS (no admin), el campo está deshabilitado de todas formas.
                Password = "        ";
                ConfirmPassword = "        ";
            }
        }

        // ============ COMANDOS ============

        [RelayCommand]
        private async Task SaveAsync()
        {
            HasError = false;
            StatusMessage = string.Empty;

            var validationError = ValidateForm();
            if (validationError != null)
            {
                HasError = true;
                StatusMessage = validationError;
                return;
            }

            await SaveServerFirstAsync();
        }

        private async Task SaveServerFirstAsync()
        {
            Console.WriteLine($"[UserFormVM] SaveServerFirstAsync — IsEditing={IsEditing}, _parentWindow={(_parentWindow != null ? "OK" : "NULL")}, _apiClient={(_apiClient != null ? "OK" : "NULL")}");

            if (_parentWindow == null || _apiClient == null)
            {
                HasError = true;
                StatusMessage = "No se pudo iniciar el guardado. Verifica la conexión al servidor.";
                return;
            }

            string? operationMessage = null;
            var isUpdating = IsEditing && _existingUser != null;

            var success = await CasaCejaRemake.Helpers.AdminOperationHelper.ExecuteAsync(
                _parentWindow,
                _apiClient,
                async () =>
                {
                    if (isUpdating)
                    {
                        Console.WriteLine($"[UserFormVM] Formulario actual — Name='{Name}', Username='{Username}'");
                        Console.WriteLine($"[UserFormVM] _existingUser actual — Name='{_existingUser?.Name}', Username='{_existingUser?.Username}'");

                        var userToUpdate = new User
                        {
                            Id         = _existingUser!.Id,
                            Name       = Name.Trim(),
                            Email      = Email.Trim(),
                            Phone      = Phone.Trim(),
                            Username   = Username.Trim(),
                            UserType   = SelectedRole?.Id ?? _existingUser.UserType,
                            BranchId   = _existingUser.BranchId,
                            Active     = _existingUser.Active,
                            Password   = _existingUser.Password,
                            CreatedAt  = _existingUser.CreatedAt,
                            UpdatedAt  = DateTime.Now,
                            SyncStatus = _existingUser.SyncStatus,
                            LastSync   = _existingUser.LastSync,
                            RoleName   = _existingUser.RoleName,
                        };

                        Console.WriteLine($"[UserFormVM] userToUpdate construido — Name='{userToUpdate.Name}', Username='{userToUpdate.Username}'");

                        // Solo enviar contraseña si el admin activó el campo (hizo clic en él)
                        string? newPlainPassword = (_passwordActivated && !string.IsNullOrWhiteSpace(Password)) ? Password : null;
                        var updateResult = await _userService.UpdateUserAsync(userToUpdate, newPlainPassword);
                        Console.WriteLine($"[UserFormVM] UpdateUserAsync retornó — Success={updateResult.Success}, Message='{updateResult.Message}'");
                        operationMessage = updateResult.Message;
                        return updateResult;
                    }

                    var user = new User
                    {
                        Name     = Name.Trim(),
                        Email    = Email.Trim(),
                        Phone    = Phone.Trim(),
                        Username = Username.Trim(),
                        Password = Password,
                        UserType = SelectedRole?.Id ?? _userService.GetCashierRoleId(),
                        BranchId = _branchId > 0 ? _branchId : (int?)null
                    };

                    var createResult = await _userService.CreateUserAsync(user);
                    operationMessage = createResult.Message;
                    return createResult;
                },
                isUpdating ? "Usuario actualizado exitosamente." : "Usuario creado exitosamente.",
                onBusy: () => IsSaving = true,
                onIdle: () => IsSaving = false);

            if (success)
            {
                StatusMessage = operationMessage ?? string.Empty;
                HasError = false;
                Console.WriteLine($"[UserFormVM] Éxito — operación completada para Name='{Name}'");
                SaveCompleted?.Invoke(this, EventArgs.Empty);
                CloseRequested?.Invoke(this, EventArgs.Empty);
            }
            else
            {
                StatusMessage = operationMessage ?? StatusMessage;
                HasError = true;
                Console.WriteLine($"[UserFormVM] Fallo — operación no completada");
            }
        }

        private string? ValidateForm()
        {
            if (string.IsNullOrWhiteSpace(Name))
                return "El nombre es requerido.";

            if (string.IsNullOrWhiteSpace(Username))
                return "El nombre de usuario es requerido.";

            if (string.IsNullOrWhiteSpace(Email))
                return "El correo electrónico es requerido.";

            // Validación básica de formato de email
            if (!Email.Contains('@') || !Email.Contains('.'))
                return "El correo electrónico no tiene un formato válido.";

            if (string.IsNullOrWhiteSpace(Phone))
                return "El teléfono es requerido.";

            // Validación básica de teléfono (mínimo 10 dígitos)
            var phoneDigits = new string(Phone.Where(char.IsDigit).ToArray());
            if (phoneDigits.Length < 10)
                return "El teléfono debe tener al menos 10 dígitos.";

            // Solo validar contraseña en modo admin o al crear nuevo usuario
            if (ShowPasswordSection)
            {
                if (!IsEditing)
                {
                    // Contraseña obligatoria al crear
                    if (string.IsNullOrWhiteSpace(Password))
                        return "La contraseña es requerida.";

                    if (Password.Length < 4)
                        return "La contraseña debe tener al menos 4 caracteres.";
                }
                else
                {
                    // Al editar, si se pone contraseña, validar que no sea solo espacios y longitud mínima
                    if (!string.IsNullOrEmpty(Password))
                    {
                        if (string.IsNullOrWhiteSpace(Password))
                            return "La contraseña no puede contener solo espacios.";
                        if (Password.Length < 4)
                            return "La contraseña debe tener al menos 4 caracteres.";
                    }
                }

                // Confirmar contraseña si se proporcionó
                if (!string.IsNullOrEmpty(Password) && Password != ConfirmPassword)
                    return "Las contraseñas no coinciden.";
            }

            if (SelectedRole == null)
                return "Debe seleccionar un rol.";

            return null;
        }

        [RelayCommand]
        private void Cancel()
        {
            CloseRequested?.Invoke(this, EventArgs.Empty);
        }
    }
}
