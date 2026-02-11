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
    /// ViewModel para configuración del terminal POS.
    /// Accesible desde dentro del módulo POS para cajeros y admin.
    /// Maneja: Impresora, formato de tickets, ID de terminal, etc.
    /// </summary>
    public partial class PosTerminalConfigViewModel : ViewModelBase
    {
        private readonly ConfigService _configService;
        private readonly AuthService _authService;
        private readonly PrintService _printService;

        // ============ TERMINAL ============
        [ObservableProperty] private string _terminalId = "CAJA-01";
        [ObservableProperty] private string _terminalName = "Terminal Principal";

        // ============ IMPRESORA ============
        [ObservableProperty] private ObservableCollection<string> _availablePrinters = new();
        [ObservableProperty] private string? _selectedPrinter;

        // ============ CAJA ============
        [ObservableProperty] private string _cashRegisterId = "CAJA-01";

        // ============ TICKET ============
        [ObservableProperty] private string _ticketFooter = "Gracias por su compra";
        [ObservableProperty] private int _selectedFontSize = 9;
        [ObservableProperty] private string _selectedFontFamily = "Courier New";
        [ObservableProperty] private string _selectedPrintFormat = "thermal";
        [ObservableProperty] private int _selectedTicketLineWidth = 40;

        // ============ OPCIONES ============
        [ObservableProperty] private bool _autoPrint = true;
        [ObservableProperty] private bool _openCashDrawer = false;

        // ============ PERMISOS ============
        /// <summary>Solo Admin puede editar Sucursal e ID de Caja</summary>
        public bool CanEditAdminFields => _authService.IsAdmin;
        public bool IsReadOnlyForCajero => !_authService.IsAdmin;

        // ============ OPCIONES ESTÁTICAS ============
        public List<int> FontSizeOptions { get; } = new() { 8, 9, 10, 11, 12 };
        public List<string> FontFamilyOptions { get; } = new()
        {
            "Courier New", "Consolas", "Lucida Console", "Menlo", "Monaco"
        };
        public List<string> PrintFormatOptions { get; } = new()
        {
            "thermal",  // Ticket Térmico
            "letter"    // Hoja Carta
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
            _printService = printService;
        }

        /// <summary>
        /// Inicializa la vista: carga config e impresoras.
        /// </summary>
        public async Task InitializeAsync()
        {
            IsLoading = true;
            try
            {
                // 1. Cargar impresoras del sistema
                var printers = _printService.GetAvailablePrinters();
                AvailablePrinters = new ObservableCollection<string>(printers);

                // 2. Aplicar configuración guardada a los controles
                var config = _configService.PosTerminalConfig;
                TerminalId = config.TerminalId;
                TerminalName = config.TerminalName;
                SelectedPrinter = AvailablePrinters.Contains(config.PrinterName)
                    ? config.PrinterName
                    : AvailablePrinters.FirstOrDefault();
                TicketFooter = config.TicketFooter;
                SelectedFontSize = config.FontSize;
                SelectedFontFamily = config.FontFamily;
                SelectedPrintFormat = config.PrintFormat;
                SelectedTicketLineWidth = config.TicketLineWidth;
                AutoPrint = config.AutoPrint;
                OpenCashDrawer = config.OpenCashDrawer;

                StatusMessage = "Configuración cargada";
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
                await _configService.UpdatePosTerminalConfigAsync(config =>
                {
                    // Solo Admin puede cambiar ID de terminal
                    if (_authService.IsAdmin)
                    {
                        config.TerminalId = TerminalId;
                        config.TerminalName = TerminalName;
                    }

                    // Todos pueden cambiar estos campos
                    config.PrinterName = SelectedPrinter ?? string.Empty;
                    config.PrintFormat = SelectedPrintFormat;
                    config.TicketFooter = TicketFooter;
                    config.FontSize = SelectedFontSize;
                    config.FontFamily = SelectedFontFamily;
                    config.TicketLineWidth = SelectedTicketLineWidth;
                    config.AutoPrint = AutoPrint;
                    config.OpenCashDrawer = OpenCashDrawer;
                });

                StatusMessage = "✓ Configuración guardada correctamente";
                
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
