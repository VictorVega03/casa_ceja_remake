using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CasaCejaRemake.Services;
using System;

namespace CasaCejaRemake.ViewModels.Inventory
{
    /// <summary>
    /// ViewModel para el menú principal del módulo de Inventario.
    /// Muestra cards de navegación y controla el acceso según conectividad.
    /// </summary>
    public partial class InventoryMainViewModel : ViewModelBase
    {
        private readonly AuthService _authService;
        private readonly ApiClient _apiClient;

        // ========== EVENTOS DE NAVEGACIÓN ==========
        public event EventHandler? EntriesSelected;
        public event EventHandler? OutputsSelected;
        public event EventHandler? ConfirmEntrySelected;
        public event EventHandler? CatalogSelected;
        public event EventHandler? CategoriesSelected;
        public event EventHandler? HistorySelected;
        public event EventHandler? BackToModuleSelector;
        public event EventHandler? LogoutRequested;

        // ========== PROPIEDADES ==========

        [ObservableProperty]
        private string _branchName = "Sucursal";

        [ObservableProperty]
        private string _userName = "Usuario";

        [ObservableProperty]
        private bool _isOnline = false;

        [ObservableProperty]
        private bool _isCheckingConnection = false;

        [ObservableProperty]
        private int _pendingConfirmations = 0;

        /// <summary>
        /// Indica si hay confirmaciones pendientes (para badge en la card).
        /// </summary>
        public bool HasPendingConfirmations => PendingConfirmations > 0;

        // ========== CONSTRUCTOR ==========

        public InventoryMainViewModel(
            AuthService authService,
            ApiClient apiClient,
            int branchId,
            string branchName)
        {
            _authService = authService;
            _apiClient = apiClient;
            BranchName = branchName;
            UserName = authService.CurrentUserName ?? "Usuario";
        }

        // ========== COMMANDS ==========

        [RelayCommand]
        private void OpenEntries()
        {
            if (!IsOnline) return;
            EntriesSelected?.Invoke(this, EventArgs.Empty);
        }

        [RelayCommand]
        private void OpenOutputs()
        {
            if (!IsOnline) return;
            OutputsSelected?.Invoke(this, EventArgs.Empty);
        }

        [RelayCommand]
        private void OpenConfirmEntry()
        {
            if (!IsOnline) return;
            ConfirmEntrySelected?.Invoke(this, EventArgs.Empty);
        }

        [RelayCommand]
        private void OpenCatalog()
        {
            CatalogSelected?.Invoke(this, EventArgs.Empty);
        }

        [RelayCommand]
        private void OpenCategories()
        {
            CategoriesSelected?.Invoke(this, EventArgs.Empty);
        }

        [RelayCommand]
        private void OpenHistory()
        {
            HistorySelected?.Invoke(this, EventArgs.Empty);
        }

        [RelayCommand]
        private void GoBack()
        {
            BackToModuleSelector?.Invoke(this, EventArgs.Empty);
        }

        [RelayCommand]
        private void Logout()
        {
            LogoutRequested?.Invoke(this, EventArgs.Empty);
        }

        [RelayCommand]
        private async System.Threading.Tasks.Task CheckConnectivityAsync()
        {
            IsCheckingConnection = true;
            try
            {
                var response = await _apiClient.GetAsync<Models.DTOs.HealthResponse>("/api/v1/health");
                IsOnline = response?.IsSuccess == true;
            }
            catch
            {
                IsOnline = false;
            }
            finally
            {
                IsCheckingConnection = false;
            }

            Console.WriteLine($"[InventoryMain] Conectividad: {(IsOnline ? "ONLINE" : "OFFLINE")}");
        }

        partial void OnPendingConfirmationsChanged(int value)
        {
            OnPropertyChanged(nameof(HasPendingConfirmations));
        }
    }
}
