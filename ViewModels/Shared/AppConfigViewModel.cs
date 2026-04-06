using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CasaCejaRemake.Data.Repositories;
using CasaCejaRemake.Models;
using CasaCejaRemake.Services;

namespace CasaCejaRemake.ViewModels.Shared
{
    public partial class AppConfigViewModel : ViewModelBase
    {
        private readonly ConfigService _configService;
        private readonly AuthService _authService;
        private readonly BaseRepository<Branch> _branchRepository;
        private readonly PrintService _printService;
        private readonly UserService _userService;
        private readonly SyncService _syncService;
        private readonly ApiClient _apiClient;

        // ============ SUCURSAL ============
        [ObservableProperty] private ObservableCollection<Branch> _branches = new();
        [ObservableProperty] private Branch? _selectedBranch;
        [ObservableProperty] private bool _branchChangeUnlocked;
        public string BranchLockButtonText => BranchChangeUnlocked ? "🔒 Bloquear" : "🔓 Desbloquear";
        private int _originalBranchId;

        // ============ IMPRESORA ============
        [ObservableProperty] private ObservableCollection<string> _availablePrinters = new();
        [ObservableProperty] private string? _selectedPrinter;
        [ObservableProperty] private string _selectedPrintFormat = "Térmica";

        public List<string> PrintFormatOptions { get; } = new()
        {
            "Térmica",
            "T. Carta"
        };

        // ============ SYNC CATÁLOGO ============
        [ObservableProperty] private bool _isSyncing;
        [ObservableProperty] private string _syncStatusMessage = string.Empty;
        [ObservableProperty] private double _syncProgress;

        public string LastSyncTimestampText
        {
            get
            {
                var ts = _configService.AppConfig.LastSyncTimestamp;
                if (ts <= 0) return "Sin sincronización registrada";
                var dt = DateTimeOffset.FromUnixTimeSeconds(ts).LocalDateTime;
                return dt.ToString("dd/MM/yyyy HH:mm");
            }
        }

        // ============ ESTADO GENERAL ============
        [ObservableProperty] private bool _isLoading;
        [ObservableProperty] private string _statusMessage = string.Empty;

        public event EventHandler? CloseRequested;
        public event EventHandler? ConfigurationSaved;
        public event Func<Task<bool>>? AdminVerificationRequested;

        public AppConfigViewModel(
            BaseRepository<Branch> branchRepository,
            ConfigService configService,
            AuthService authService,
            PrintService printService,
            UserService userService,
            SyncService syncService,
            ApiClient apiClient)
        {
            _branchRepository = branchRepository;
            _configService    = configService;
            _authService      = authService;
            _printService     = printService;
            _userService      = userService;
            _syncService      = syncService;
            _apiClient        = apiClient;
        }

        partial void OnBranchChangeUnlockedChanged(bool value)
        {
            OnPropertyChanged(nameof(BranchLockButtonText));
        }

        public async Task InitializeAsync()
        {
            IsLoading = true;
            try
            {
                var allBranches = await _branchRepository.GetAllAsync();
                var branchList  = allBranches.Where(b => b.Active).ToList();
                Branches = new ObservableCollection<Branch>(branchList);

                var appConfig = _configService.AppConfig;
                SelectedBranch = Branches.FirstOrDefault(b => b.Id == appConfig.CurrentBranchId)
                                 ?? Branches.FirstOrDefault();
                _originalBranchId = appConfig.CurrentBranchId ?? 0;

                var printers = await Task.Run(() => _printService.GetAvailablePrinters());
                AvailablePrinters = new ObservableCollection<string>(printers);

                var posConfig = _configService.PosTerminalConfig;
                SelectedPrinter = AvailablePrinters.Contains(posConfig.PrinterName)
                    ? posConfig.PrinterName
                    : AvailablePrinters.FirstOrDefault();
                SelectedPrintFormat = posConfig.PrintFormat switch
                {
                    "thermal" => "Térmica",
                    "letter"  => "T. Carta",
                    _         => posConfig.PrintFormat
                };

                BranchChangeUnlocked = false;
                StatusMessage = "Configuración cargada";

                // Refrescar texto del último sync
                OnPropertyChanged(nameof(LastSyncTimestampText));
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
        private async Task ToggleBranchLockAsync()
        {
            if (BranchChangeUnlocked)
            {
                BranchChangeUnlocked = false;
                SelectedBranch = Branches.FirstOrDefault(b => b.Id == _originalBranchId)
                                 ?? Branches.FirstOrDefault();
                return;
            }

            if (AdminVerificationRequested != null)
            {
                var verified = await AdminVerificationRequested.Invoke();
                if (verified)
                {
                    BranchChangeUnlocked = true;
                    StatusMessage = "✓ Cambio de sucursal desbloqueado";
                }
                else
                {
                    StatusMessage = "Verificación de administrador cancelada";
                }
            }
        }

        [RelayCommand]
        private Task RefreshPrintersAsync()
        {
            var printers = _printService.GetAvailablePrinters();
            AvailablePrinters = new ObservableCollection<string>(printers);
            StatusMessage = $"Se encontraron {printers.Count} impresora(s)";
            return Task.CompletedTask;
        }

        [RelayCommand]
        private async Task SaveAsync()
        {
            try
            {
                IsLoading     = true;
                StatusMessage = "Guardando configuración...";

                var branchChanged = false;

                if (SelectedBranch != null)
                {
                    var newBranchId = SelectedBranch.Id;
                    branchChanged = _originalBranchId != newBranchId && BranchChangeUnlocked;

                    if (branchChanged)
                    {
                        await _configService.UpdateAppConfigAsync(config =>
                        {
                            config.CurrentBranchId   = SelectedBranch.Id;
                            config.CurrentBranchName = SelectedBranch.Name;
                        });
                        _authService.SetCurrentBranch(SelectedBranch.Id);
                    }
                }

                await _configService.UpdatePosTerminalConfigAsync(config =>
                {
                    config.PrinterName  = SelectedPrinter ?? string.Empty;
                    config.PrintFormat  = SelectedPrintFormat;
                });

                if (!StatusMessage.Contains("⚠️"))
                    StatusMessage = "✓ Configuración guardada correctamente";

                if (branchChanged)
                {
                    ConfigurationSaved?.Invoke(this, EventArgs.Empty);
                    return;
                }

                await Task.Delay(1500);
                CloseRequested?.Invoke(this, EventArgs.Empty);
            }
            catch (Exception ex)
            {
                StatusMessage = $"Error al guardar: {ex.Message}";
                Console.WriteLine($"[AppConfigViewModel] Error: {ex.Message}");
            }
            finally
            {
                IsLoading = false;
            }
        }

        [RelayCommand]
        private async Task SyncCatalogAsync()
        {
            if (IsSyncing) return;

            var serverAvailable = await _apiClient.IsServerAvailableAsync();
            if (!serverAvailable)
            {
                SyncStatusMessage = "⚠️ Sin conexión al servidor";
                return;
            }

            IsSyncing         = true;
            SyncProgress      = 0;
            SyncStatusMessage = "Iniciando sincronización...";

            try
            {
                var results = await _syncService.PullCatalogFullAsync(
                    onProgress: (label, current, total) =>
                    {
                        SyncStatusMessage = $"Descargando {label}...";
                        SyncProgress      = (double)current / total;
                    },
                    ct: CancellationToken.None);

                var totalRecords = results.Sum(r => r.RecordsPulled);
                var errors       = results.Count(r => !r.Success);

                SyncProgress      = 1.0;
                SyncStatusMessage = errors == 0
                    ? $"✓ Catálogo actualizado — {totalRecords} registros descargados"
                    : $"⚠️ Completado con {errors} error(es)";

                // Actualizar label de último sync
                OnPropertyChanged(nameof(LastSyncTimestampText));
            }
            catch (Exception ex)
            {
                SyncStatusMessage = $"Error: {ex.Message}";
                Console.WriteLine($"[AppConfigViewModel] Error en sync catálogo: {ex.Message}");
            }
            finally
            {
                IsSyncing = false;
            }
        }

        [RelayCommand]
        private void OpenPrinterManager()
        {
            try
            {
                if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
                {
                    Process.Start(new ProcessStartInfo
                    {
                        FileName        = "open",
                        Arguments       = "/System/Library/PreferencePanes/PrintAndFax.prefPane",
                        UseShellExecute = false
                    });
                }
                else if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                {
                    Process.Start(new ProcessStartInfo
                    {
                        FileName        = "ms-settings:printers",
                        UseShellExecute = true
                    });
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[AppConfigViewModel] Error abriendo gestor de impresoras: {ex.Message}");
                StatusMessage = "No se pudo abrir el gestor de impresoras del sistema.";
            }
        }

        [RelayCommand]
        private void OpenNativePrinterManager()
        {
            try
            {
                if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
                {
                    Process.Start(new ProcessStartInfo
                    {
                        FileName        = "open",
                        Arguments       = "x-apple.systempreferences:com.apple.preference.printfax",
                        UseShellExecute = false
                    });
                }
                else if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                {
                    Process.Start(new ProcessStartInfo
                    {
                        FileName        = "ms-settings:printers",
                        UseShellExecute = true
                    });
                }
                StatusMessage = "Gestor de impresoras abierto";
            }
            catch (Exception ex)
            {
                StatusMessage = $"No se pudo abrir el gestor: {ex.Message}";
                Console.WriteLine($"[AppConfigViewModel] Error abriendo gestor de impresoras: {ex.Message}");
            }
        }

        [RelayCommand]
        private void Close() => CloseRequested?.Invoke(this, EventArgs.Empty);
    }
}