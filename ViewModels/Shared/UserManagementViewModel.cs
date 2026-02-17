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
    /// ViewModel para la vista principal de gestión de usuarios.
    /// Se adapta según el modo: Admin = CRUD completo, POS = solo alta/edición de cajeros.
    /// </summary>
    public partial class UserManagementViewModel : ViewModelBase
    {
        private readonly UserService _userService;
        private readonly AuthService _authService;

        // ============ PROPIEDADES ============

        /// <summary>Si es true, modo Admin con acceso completo. Si false, modo POS solo cajeros.</summary>
        [ObservableProperty] private bool _isAdminMode;

        [ObservableProperty] private ObservableCollection<User> _users = new();
        [ObservableProperty] private User? _selectedUser;
        [ObservableProperty] private string _searchText = string.Empty;
        [ObservableProperty] private bool _isLoading;
        [ObservableProperty] private string _statusMessage = string.Empty;

        /// <summary>Título dinámico según modo.</summary>
        public string Title => IsAdminMode ? "Gestión de Usuarios" : "Gestión de Cajeros";

        /// <summary>Subtítulo dinámico según modo.</summary>
        public string Subtitle => IsAdminMode
            ? "Administrar todos los usuarios del sistema"
            : "Dar de alta y modificar datos de cajeros";

        /// <summary>Icono emoji del título.</summary>
        public string TitleIcon => IsAdminMode ? "👥" : "👤";

        /// <summary>¿Se puede desactivar usuarios? Solo en modo Admin.</summary>
        public bool CanDeactivate => IsAdminMode;

        // Lista completa sin filtro (para búsqueda)
        private List<User> _allUsers = new();

        // ============ EVENTOS ============
        public event EventHandler? CloseRequested;
        public event EventHandler<User?>? EditUserRequested;
        public event EventHandler? AddUserRequested;

        public UserManagementViewModel(UserService userService, AuthService authService, bool isAdminMode)
        {
            _userService = userService ?? throw new ArgumentNullException(nameof(userService));
            _authService = authService ?? throw new ArgumentNullException(nameof(authService));
            _isAdminMode = isAdminMode;
        }

        /// <summary>
        /// Inicializa la vista cargando los usuarios.
        /// </summary>
        public async Task InitializeAsync()
        {
            await LoadUsersAsync();
        }

        // ============ COMANDOS ============

        [RelayCommand]
        private async Task LoadUsersAsync()
        {
            IsLoading = true;
            StatusMessage = "Cargando usuarios...";

            try
            {
                _allUsers = IsAdminMode
                    ? await _userService.GetAllUsersAsync()
                    : await _userService.GetCashiersAsync();

                ApplyFilter();
                StatusMessage = $"{_allUsers.Count} usuario(s) encontrado(s)";
            }
            catch (Exception ex)
            {
                StatusMessage = $"Error al cargar usuarios: {ex.Message}";
                Console.WriteLine($"[UserManagementVM] Error: {ex.Message}");
            }
            finally
            {
                IsLoading = false;
            }
        }

        [RelayCommand]
        private void AddUser()
        {
            AddUserRequested?.Invoke(this, EventArgs.Empty);
        }

        [RelayCommand]
        private void EditUser()
        {
            if (SelectedUser != null)
            {
                EditUserRequested?.Invoke(this, SelectedUser);
            }
        }

        [RelayCommand]
        private async Task DeactivateUserAsync()
        {
            if (SelectedUser == null || !IsAdminMode) return;

            // El diálogo de confirmación se maneja en el code-behind
            var result = await _userService.DeactivateUserAsync(SelectedUser.Id);
            StatusMessage = result.Message;

            if (result.Success)
            {
                await LoadUsersAsync();
            }
        }

        [RelayCommand]
        private void Close()
        {
            CloseRequested?.Invoke(this, EventArgs.Empty);
        }

        // ============ BÚSQUEDA ============

        partial void OnSearchTextChanged(string value)
        {
            ApplyFilter();
        }

        private void ApplyFilter()
        {
            if (string.IsNullOrWhiteSpace(SearchText))
            {
                Users = new ObservableCollection<User>(_allUsers);
            }
            else
            {
                var filtered = _allUsers.Where(u =>
                    u.Name.Contains(SearchText, StringComparison.OrdinalIgnoreCase) ||
                    u.Username.Contains(SearchText, StringComparison.OrdinalIgnoreCase) ||
                    u.RoleName.Contains(SearchText, StringComparison.OrdinalIgnoreCase))
                    .ToList();

                Users = new ObservableCollection<User>(filtered);
            }
        }

        /// <summary>
        /// Refresca la lista después de crear/editar un usuario.
        /// </summary>
        public async Task RefreshAsync()
        {
            await LoadUsersAsync();
        }
    }
}
