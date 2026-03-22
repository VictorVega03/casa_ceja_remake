using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CasaCejaRemake.Data.Repositories;
using CasaCejaRemake.Models;
using CasaCejaRemake.Services;

namespace CasaCejaRemake.ViewModels.Shared
{
    /// <summary>
    /// ViewModel para configuración general de la aplicación.
    /// Accesible por cualquier usuario desde el selector de módulos.
    /// Maneja: Sucursal (requiere admin) e Impresora (libre).
    /// </summary>
    public partial class AppConfigViewModel : ViewModelBase
    {
        private readonly ConfigService _configService;
        private readonly AuthService _authService;
        private readonly BaseRepository<Branch> _branchRepository;
        private readonly PrintService _printService;
        private readonly UserService _userService;

        // ============ SUCURSAL ============
        [ObservableProperty] private ObservableCollection<Branch> _branches = new();
        [ObservableProperty] private Branch? _selectedBranch;
        
        /// <summary>Si el cambio de sucursal está desbloqueado (requiere admin)</summary>
        [ObservableProperty] private bool _branchChangeUnlocked;
        
        /// <summary>Texto del botón de bloqueo de sucursal</summary>
        public string BranchLockButtonText => BranchChangeUnlocked ? "🔒 Bloquear" : "🔓 Desbloquear";

        // Guardar la sucursal original para detectar cambios
        private int _originalBranchId;

        // ============ IMPRESORA ============
        [ObservableProperty] private ObservableCollection<string> _availablePrinters = new();
        [ObservableProperty] private string? _selectedPrinter;
        [ObservableProperty] private string _selectedPrintFormat = "Térmica";

        // ============ OPCIONES ESTÁTICAS ============
        public List<string> PrintFormatOptions { get; } = new()
        {
            "Térmica",      // Ticket Térmico
            "T. Carta"      // Hoja Carta
        };

        // ============ ESTADO ============
        [ObservableProperty] private bool _isLoading;
        [ObservableProperty] private string _statusMessage = string.Empty;

        /// <summary>Evento para solicitar cierre de la vista</summary>
        public event EventHandler? CloseRequested;
        
        /// <summary>Evento cuando se guardó configuración exitosamente (con cambio de sucursal)</summary>
        public event EventHandler? ConfigurationSaved;

        /// <summary>Evento para solicitar verificación de admin (la vista lo maneja)</summary>
        public event Func<Task<bool>>? AdminVerificationRequested;

        public AppConfigViewModel(
            BaseRepository<Branch> branchRepository,
            ConfigService configService,
            AuthService authService,
            PrintService printService,
            UserService userService)
        {
            _branchRepository = branchRepository;
            _configService = configService;
            _authService = authService;
            _printService = printService;
            _userService = userService;
        }

        partial void OnBranchChangeUnlockedChanged(bool value)
        {
            OnPropertyChanged(nameof(BranchLockButtonText));
        }

        /// <summary>
        /// Inicializa la vista: carga sucursales, impresoras y configuración actual.
        /// </summary>
        public async Task InitializeAsync()
        {
            IsLoading = true;
            try
            {
                // 1. Cargar sucursales activas
                var allBranches = await _branchRepository.GetAllAsync();
                var branchList = allBranches.Where(b => b.Active).ToList();
                Branches = new ObservableCollection<Branch>(branchList);

                // 2. Aplicar configuración de sucursal guardada
                var appConfig = _configService.AppConfig;
                SelectedBranch = Branches.FirstOrDefault(b => b.Id == appConfig.CurrentBranchId)
                                 ?? Branches.FirstOrDefault();
                _originalBranchId = appConfig.CurrentBranchId ?? 0;

                // 3. Cargar impresoras del sistema
                var printers = _printService.GetAvailablePrinters();
                AvailablePrinters = new ObservableCollection<string>(printers);

                // 4. Aplicar configuración de impresora guardada
                var posConfig = _configService.PosTerminalConfig;
                SelectedPrinter = AvailablePrinters.Contains(posConfig.PrinterName)
                    ? posConfig.PrinterName
                    : AvailablePrinters.FirstOrDefault();
                SelectedPrintFormat = posConfig.PrintFormat switch
                {
                    "thermal" => "Térmica",
                    "letter" => "T. Carta",
                    _ => posConfig.PrintFormat
                };

                // Sucursal bloqueada por defecto
                BranchChangeUnlocked = false;

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

        /// <summary>
        /// Desbloquea/bloquea el cambio de sucursal (requiere admin).
        /// </summary>
        [RelayCommand]
        private async Task ToggleBranchLockAsync()
        {
            if (BranchChangeUnlocked)
            {
                // Bloquear de nuevo
                BranchChangeUnlocked = false;
                // Restaurar sucursal original si se bloqueó sin guardar
                SelectedBranch = Branches.FirstOrDefault(b => b.Id == _originalBranchId)
                                 ?? Branches.FirstOrDefault();
                return;
            }

            // Pedir verificación de admin
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
                IsLoading = true;
                StatusMessage = "Guardando configuración...";

                var branchChanged = false;

                // === Guardar sucursal si cambió ===
                if (SelectedBranch != null)
                {
                    var newBranchId = SelectedBranch.Id;
                    branchChanged = _originalBranchId != newBranchId && BranchChangeUnlocked;

                    if (branchChanged)
                    {
                        await _configService.UpdateAppConfigAsync(config =>
                        {
                            config.CurrentBranchId = SelectedBranch.Id;
                            config.CurrentBranchName = SelectedBranch.Name;
                        });
                        _authService.SetCurrentBranch(SelectedBranch.Id);
                    }
                }

                // === Guardar solo impresora y tipo de impresión ===
                await _configService.UpdatePosTerminalConfigAsync(config =>
                {
                    config.PrinterName = SelectedPrinter ?? string.Empty;
                    config.PrintFormat = SelectedPrintFormat;
                });

                if (!StatusMessage.Contains("⚠️"))
                {
                    StatusMessage = "✓ Configuración guardada correctamente";
                }

                // Si cambió la sucursal, notificar (la app se reiniciará)
                if (branchChanged)
                {
                    ConfigurationSaved?.Invoke(this, EventArgs.Empty);
                    return; // No cerrar, el evento se encarga de reiniciar
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

        /// <summary>
        /// Abre el gestor de impresoras nativo del sistema operativo.
        /// En macOS: Preferencias del Sistema > Impresoras y Escáneres.
        /// En Windows: Panel de control > Dispositivos e impresoras.
        /// </summary>
        [RelayCommand]
        private void OpenPrinterManager()
        {
            try
            {
                if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
                {
                    Process.Start(new ProcessStartInfo
                    {
                        FileName = "open",
                        Arguments = "/System/Library/PreferencePanes/PrintAndFax.prefPane",
                        UseShellExecute = false
                    });
                }
                else if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                {
                    Process.Start(new ProcessStartInfo
                    {
                        FileName = "ms-settings:printers",
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
                        FileName = "open",
                        Arguments = "x-apple.systempreferences:com.apple.preference.printfax",
                        UseShellExecute = false
                    });
                }
                else if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                {
                    Process.Start(new ProcessStartInfo
                    {
                        FileName = "ms-settings:printers",
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
