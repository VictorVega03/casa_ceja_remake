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
        private readonly bool _isAdminMode;
        private readonly User? _existingUser;

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

        /// <summary>El campo de contraseña tiene texto informativo diferente en edición.</summary>
        public string PasswordWatermark => IsEditing
            ? "Dejar vacío para mantener actual"
            : "Mínimo 4 caracteres";

        /// <summary>Título dinámico del formulario.</summary>
        public string FormTitle => IsReadOnly
            ? "Detalle de Usuario"
            : (IsEditing
                ? "Editar Usuario"
                : (_isAdminMode ? "Nuevo Usuario" : "Nuevo Cajero"));

        /// <summary>Icono del formulario.</summary>
        public string FormIcon => IsReadOnly ? "👁️" : (IsEditing ? "✏️" : "➕");

        // ============ EVENTOS ============
        public event EventHandler? CloseRequested;
        public event EventHandler? SaveCompleted;

        public UserFormViewModel(UserService userService, bool isAdminMode, User? existingUser = null, bool isReadOnly = false)
        {
            _userService = userService ?? throw new ArgumentNullException(nameof(userService));
            _isAdminMode = isAdminMode;
            _existingUser = existingUser;
            _isEditing = existingUser != null;
            _isReadOnly = isReadOnly;

            LoadRoles();

            if (_existingUser != null)
            {
                PopulateFromUser(_existingUser);
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
            Name = user.Name;
            Email = user.Email;
            Phone = user.Phone;
            Username = user.Username;
            // No poblar la contraseña por seguridad
        }

        // ============ COMANDOS ============

        [RelayCommand]
        private async Task SaveAsync()
        {
            // Limpiar estado previo
            HasError = false;
            StatusMessage = string.Empty;

            // Validaciones del formulario
            var validationError = ValidateForm();
            if (validationError != null)
            {
                HasError = true;
                StatusMessage = validationError;
                return;
            }

            IsSaving = true;

            try
            {
                if (IsEditing && _existingUser != null)
                {
                    await UpdateExistingUser();
                }
                else
                {
                    await CreateNewUser();
                }
            }
            finally
            {
                IsSaving = false;
            }
        }

        private async Task CreateNewUser()
        {
            var user = new User
            {
                Name = Name.Trim(),
                Email = Email.Trim(),
                Phone = Phone.Trim(),
                Username = Username.Trim(),
                Password = Password,
                UserType = SelectedRole?.Id ?? _userService.GetCashierRoleId()
            };

            var result = await _userService.CreateUserAsync(user);

            if (result.Success)
            {
                StatusMessage = result.Message;
                HasError = false;
                SaveCompleted?.Invoke(this, EventArgs.Empty);
                CloseRequested?.Invoke(this, EventArgs.Empty);
            }
            else
            {
                StatusMessage = result.Message;
                HasError = true;
            }
        }

        private async Task UpdateExistingUser()
        {
            if (_existingUser == null) return;

            _existingUser.Name = Name.Trim();
            _existingUser.Email = Email.Trim();
            _existingUser.Phone = Phone.Trim();
            _existingUser.Username = Username.Trim();

            // Solo actualizar contraseña si se proporcionó una nueva
            if (!string.IsNullOrWhiteSpace(Password))
            {
                _existingUser.Password = Password;
            }

            // Solo cambiar rol en modo Admin
            if (_isAdminMode && SelectedRole != null)
            {
                _existingUser.UserType = SelectedRole.Id;
            }

            var result = await _userService.UpdateUserAsync(_existingUser);

            if (result.Success)
            {
                StatusMessage = result.Message;
                HasError = false;
                SaveCompleted?.Invoke(this, EventArgs.Empty);
                CloseRequested?.Invoke(this, EventArgs.Empty);
            }
            else
            {
                StatusMessage = result.Message;
                HasError = true;
            }
        }

        private string? ValidateForm()
        {
            if (string.IsNullOrWhiteSpace(Name))
                return "El nombre es requerido.";

            if (string.IsNullOrWhiteSpace(Email))
                return "El correo electrónico es requerido.";

            if (string.IsNullOrWhiteSpace(Phone))
                return "El teléfono es requerido.";

            if (string.IsNullOrWhiteSpace(Username))
                return "El nombre de usuario es requerido.";

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
                // Al editar, si se pone contraseña, validar longitud
                if (!string.IsNullOrWhiteSpace(Password) && Password.Length < 4)
                    return "La contraseña debe tener al menos 4 caracteres.";
            }

            // Confirmar contraseña si se proporcionó
            if (!string.IsNullOrWhiteSpace(Password) && Password != ConfirmPassword)
                return "Las contraseñas no coinciden.";

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
