using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CasaCejaRemake.Services;
using CasaCejaRemake.Services.Interfaces;

namespace CasaCejaRemake.ViewModels.Shared
{
    /// <summary>
    /// ViewModel para configuración del terminal POS.
    /// Accesible desde dentro del módulo POS para cajeros y admin.
    /// Maneja: ID de terminal, parámetros del ticket (fuente, ancho, pie de página).
    /// La configuración de impresora (selección e tipo) se maneja en AppConfigViewModel.
    /// </summary>
    public partial class PosTerminalConfigViewModel : ViewModelBase
    {
        private readonly IConfigService _configService;
        private readonly IAuthService _authService;

        // ============ TERMINAL ============
        [ObservableProperty] private string _terminalId = "CAJA-01";
        [ObservableProperty] private string _terminalName = "Terminal Principal";

        // ============ TICKET ============
        [ObservableProperty] private string _ticketFooter = "Gracias por su compra";
        [ObservableProperty] private int _selectedFontSize = 9;
        [ObservableProperty] private string _selectedFontFamily = "Courier New";
        [ObservableProperty] private int _selectedTicketLineWidth = 40;

        // ============ OPCIONES ============
        [ObservableProperty] private bool _autoPrint = true;
        [ObservableProperty] private bool _openCashDrawer = false;

        // ============ PERMISOS ============
        /// <summary>Solo Admin puede editar ID de Terminal</summary>
        public bool CanEditAdminFields => _authService.IsAdmin;
        public bool IsReadOnlyForCajero => !_authService.IsAdmin;

        // ============ OPCIONES ESTÁTICAS ============
        public List<int> FontSizeOptions { get; } = new() { 8, 9, 10, 11, 12 };
        public List<string> FontFamilyOptions { get; } = new()
        {
            "Courier New", "Consolas", "Lucida Console", "Menlo", "Monaco"
        };
        public List<int> TicketLineWidthOptions { get; } = new() { 32, 40, 48 };

        // ============ ESTADO ============
        [ObservableProperty] private bool _isLoading;
        [ObservableProperty] private string _statusMessage = string.Empty;

        /// <summary>Evento para solicitar cierre de la vista</summary>
        public event EventHandler? CloseRequested;

        public PosTerminalConfigViewModel(
            ConfigService configService,
            AuthService authService,
            PrintService printService)
        {
            _configService = configService;
            _authService = authService;
        }

        /// <summary>
        /// Inicializa la vista: carga configuración del terminal.
        /// </summary>
        public async Task InitializeAsync()
        {
            IsLoading = true;
            try
            {
                var config = _configService.PosTerminalConfig;
                TerminalId = config.TerminalId;
                TerminalName = config.TerminalName;
                TicketFooter = config.TicketFooter;
                SelectedFontSize = config.FontSize;
                SelectedFontFamily = config.FontFamily;
                SelectedTicketLineWidth = config.TicketLineWidth;
                AutoPrint = config.AutoPrint;
                OpenCashDrawer = config.OpenCashDrawer;

                StatusMessage = "Configuración cargada";
                await Task.CompletedTask;
            }
            catch (Exception ex)
            {
                StatusMessage = $"Error cargando configuración: {ex.Message}";
                Console.WriteLine($"[PosTerminalConfigViewModel] Error: {ex.Message}");
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
                IsLoading = true;
                StatusMessage = "Guardando configuración...";

                await _configService.UpdatePosTerminalConfigAsync(config =>
                {
                    // Solo Admin puede cambiar ID de terminal
                    if (_authService.IsAdmin)
                    {
                        config.TerminalId = TerminalId;
                        config.TerminalName = TerminalName;
                    }

                    // Todos pueden cambiar parámetros de ticket
                    config.TicketFooter = TicketFooter;
                    config.FontSize = SelectedFontSize;
                    config.FontFamily = SelectedFontFamily;
                    config.TicketLineWidth = SelectedTicketLineWidth;
                    config.AutoPrint = AutoPrint;
                    config.OpenCashDrawer = OpenCashDrawer;
                });

                StatusMessage = "✓ Configuración guardada correctamente";

                // Esperar un momento para que el usuario vea el mensaje
                await Task.Delay(1500);

                // Cerrar automáticamente después de guardar exitosamente
                CloseRequested?.Invoke(this, EventArgs.Empty);
            }
            catch (Exception ex)
            {
                StatusMessage = $"Error al guardar: {ex.Message}";
                Console.WriteLine($"[ConfigViewModel] Error: {ex.Message}");
            }
            finally
            {
                IsLoading = false;
            }
        }

        [RelayCommand]
        private void Close() => CloseRequested?.Invoke(this, EventArgs.Empty);
    }
}
