using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CasaCejaRemake.Services;
using System;

namespace CasaCejaRemake.ViewModels.Admin
{
    public partial class AdminMainViewModel : ViewModelBase
    {
        private readonly AuthService _authService;
        private readonly ApiClient _apiClient;

        // ===== EVENTOS DE NAVEGACIÓN =====
        public event EventHandler? CatalogSelected;
        public event EventHandler? UnitsSelected;
        public event EventHandler? CategoriesSelected;
        public event EventHandler? UsersSelected;
        public event EventHandler? BranchesSelected;
        public event EventHandler? SuppliersSelected;
        public event EventHandler? MovementsSelected;
        public event EventHandler? CashCloseHistorySelected;
        public event EventHandler? GlobalStockSelected;
        public event EventHandler? ExitRequested;

        // ===== PROPIEDADES =====
        [ObservableProperty] private string _userName = "Administrador";
        [ObservableProperty] private bool _isOnline = false;
        [ObservableProperty] private bool _isCheckingConnection = false;
        [ObservableProperty] private bool _showOnlineBanner = false;

        private bool _wasOffline = false;
        private bool _hasConnectivityCheckCompleted = false;

        // ===== CONSTRUCTOR =====
        public AdminMainViewModel(AuthService authService, ApiClient apiClient)
        {
            _authService = authService;
            _apiClient = apiClient;
            UserName = authService.CurrentUserName ?? "Administrador";
        }

        // ===== COMMANDS DE NAVEGACIÓN =====
        [RelayCommand] private void OpenCatalog() => CatalogSelected?.Invoke(this, EventArgs.Empty);
        [RelayCommand] private void OpenUnits() => UnitsSelected?.Invoke(this, EventArgs.Empty);
        [RelayCommand] private void OpenCategories() => CategoriesSelected?.Invoke(this, EventArgs.Empty);
        [RelayCommand] private void OpenUsers() => UsersSelected?.Invoke(this, EventArgs.Empty);
        [RelayCommand] private void OpenBranches() => BranchesSelected?.Invoke(this, EventArgs.Empty);
        [RelayCommand] private void OpenSuppliers() => SuppliersSelected?.Invoke(this, EventArgs.Empty);
        [RelayCommand] private void OpenMovements() => MovementsSelected?.Invoke(this, EventArgs.Empty);
        [RelayCommand] private void OpenCashCloseHistory() => CashCloseHistorySelected?.Invoke(this, EventArgs.Empty);
        [RelayCommand] private void OpenGlobalStock() => GlobalStockSelected?.Invoke(this, EventArgs.Empty);
        [RelayCommand] private void Exit() => ExitRequested?.Invoke(this, EventArgs.Empty);

        // ===== CONECTIVIDAD =====
        [RelayCommand]
        private async System.Threading.Tasks.Task CheckConnectivityAsync()
        {
            IsCheckingConnection = true;
            try
            {
                var isOnlineNow = await _apiClient.IsServerAvailableAsync();
                if (!_hasConnectivityCheckCompleted)
                {
                    if (!isOnlineNow) _wasOffline = true;
                    _hasConnectivityCheckCompleted = true;
                }
                IsOnline = isOnlineNow;
            }
            catch { IsOnline = false; }
            finally { IsCheckingConnection = false; }
            Console.WriteLine($"[AdminMain] Conectividad: {(IsOnline ? "ONLINE" : "OFFLINE")}");
        }

        partial void OnIsOnlineChanged(bool value)
        {
            if (value && _wasOffline) _ = ShowOnlineBannerAsync();
            if (!value) _wasOffline = true;
        }

        private async System.Threading.Tasks.Task ShowOnlineBannerAsync()
        {
            ShowOnlineBanner = true;
            await System.Threading.Tasks.Task.Delay(3000);
            ShowOnlineBanner = false;
        }
    }
}
