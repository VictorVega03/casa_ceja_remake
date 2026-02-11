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
    /// ViewModel para configuración general de la aplicación.
    /// Solo accesible para Admin.
    /// Maneja: Sucursal actual.
    /// </summary>
    public partial class AppConfigViewModel : ViewModelBase
    {
        private readonly ConfigService _configService;
        private readonly AuthService _authService;
        private readonly Data.DatabaseService _databaseService;

        // ============ SUCURSAL ============
        [ObservableProperty] private ObservableCollection<Branch> _branches = new();
        [ObservableProperty] private Branch? _selectedBranch;

        // ============ ESTADO ============
        [ObservableProperty] private bool _isLoading;
        [ObservableProperty] private string _statusMessage = string.Empty;

        /// <summary>Evento para solicitar cierre de la vista</summary>
        public event EventHandler? CloseRequested;

        public AppConfigViewModel(
            ConfigService configService,
            AuthService authService,
            Data.DatabaseService databaseService)
        {
            _configService = configService;
            _authService = authService;
            _databaseService = databaseService;
        }

        /// <summary>
        /// Inicializa la vista: carga sucursales y configuración actual.
        /// </summary>
        public async Task InitializeAsync()
        {
            IsLoading = true;
            try
            {
                // 1. Cargar sucursales activas
                var branchList = await _databaseService.Table<Branch>()
                    .Where(b => b.Active)
                    .ToListAsync();
                Branches = new ObservableCollection<Branch>(branchList);

                // 2. Aplicar configuración guardada
                var config = _configService.AppConfig;
                SelectedBranch = Branches.FirstOrDefault(b => b.Id == config.BranchId)
                                 ?? Branches.FirstOrDefault();

                StatusMessage = "Configuración cargada";
            }
            catch (Exception ex)
            {
                StatusMessage = $"Error cargando configuración: {ex.Message}";
                Console.WriteLine($"[AppConfigViewModel] Error: {ex.Message}");
            }
            finally
            {
                IsLoading = false;
            }
        }

        [RelayCommand]
        private async Task SaveAsync()
        {
            try
            {
                if (SelectedBranch == null)
                {
                    StatusMessage = "Debe seleccionar una sucursal";
                    return;
                }

                var oldBranchId = _configService.AppConfig.BranchId;
                var newBranchId = SelectedBranch.Id;

                await _configService.UpdateAppConfigAsync(config =>
                {
                    config.BranchId = SelectedBranch.Id;
                    config.BranchName = SelectedBranch.Name;
                });

                // Actualizar también AuthService si está usando BranchId
                if (_authService.IsAdmin)
                {
                    _authService.SetCurrentBranch(SelectedBranch.Id);
                }

                // Mensaje diferente si cambió la sucursal
                if (oldBranchId != newBranchId)
                {
                    StatusMessage = $"✓ Sucursal cambiada a '{SelectedBranch.Name}'. Cierre y vuelva a abrir el POS para aplicar cambios.";
                }
                else
                {
                    StatusMessage = "✓ Configuración guardada correctamente";
                }
                
                // Esperar 3 segundos para que el usuario lea el mensaje
                await Task.Delay(3000);
                
                // Cerrar automáticamente después de guardar exitosamente
                CloseRequested?.Invoke(this, EventArgs.Empty);
            }
            catch (Exception ex)
            {
                StatusMessage = $"Error al guardar: {ex.Message}";
            }
        }

        [RelayCommand]
        private void Close() => CloseRequested?.Invoke(this, EventArgs.Empty);
    }
}
