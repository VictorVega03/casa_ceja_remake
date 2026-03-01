using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using CasaCejaRemake.Models;
using CasaCejaRemake.Services.Interfaces;
using CasaCejaRemake.Services.Platform;

namespace CasaCejaRemake.Services
{
    /// <summary>
    /// Resultado de una operación de impresión.
    /// Permite distinguir por qué falló sin depender solo de bool.
    /// </summary>
    public class PrintResult
    {
        public bool Success { get; private set; }
        public string? ErrorMessage { get; private set; }
        public PrintFailReason FailReason { get; private set; }

        public static PrintResult Ok() =>
            new() { Success = true };

        public static PrintResult Fail(PrintFailReason reason, string message) =>
            new() { Success = false, FailReason = reason, ErrorMessage = message };
    }

    /// <summary>
    /// Categoría del fallo para que la UI decida qué mensaje mostrar.
    /// </summary>
    public enum PrintFailReason
    {
        None = 0,
        /// <summary>No hay impresora guardada en la configuración del terminal.</summary>
        NoPrinterConfigured,
        /// <summary>La impresión automática está desactivada por el usuario.</summary>
        AutoPrintDisabled,
        /// <summary>El formato configurado no es térmico (carta u otro).</summary>
        FormatNotThermal,
        /// <summary>Error al comunicarse con el driver/spooler.</summary>
        DriverError,
        /// <summary>Error de I/O (archivo temporal, permisos, etc.).</summary>
        IoError,
        /// <summary>Sistema operativo no soportado.</summary>
        UnsupportedOs
    }

    /// <summary>
    /// Servicio de impresión multiplataforma.
    /// Soporta impresoras térmicas (ticket) y convencionales (carta).
    /// </summary>
    public class PrintService : IPrintService
    {
        private readonly ConfigService _configService;

        public PrintService(ConfigService configService)
        {
            _configService = configService;
        }

        // ============================================================
        // DETECCIÓN DE IMPRESORAS
        // ============================================================

        /// <summary>
        /// Obtiene la lista de impresoras instaladas en el sistema.
        /// Detecta automáticamente si es Windows o macOS.
        /// </summary>
        public List<string> GetAvailablePrinters()
        {
            try
            {
                if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                    return GetWindowsPrinters();
                else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
                    return GetMacPrinters();
                else
                    return new List<string> { "(Sin impresoras detectadas)" };
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[PrintService] Error general detectando impresoras: {ex.Message}");
                return new List<string> { "(Error al detectar impresoras)" };
            }
        }

        /// <summary>Windows: usa wmic para listar impresoras.</summary>
        private List<string> GetWindowsPrinters()
        {
            var printers = new List<string>();
            try
            {
                // PowerShell Get-Printer: funciona en Windows 10/11.
                // wmic está deprecado en Windows 11.
                var process = new Process
                {
                    StartInfo = new ProcessStartInfo
                    {
                        FileName = "powershell.exe",
                        Arguments = "-NoProfile -Command \"Get-Printer | Select-Object -ExpandProperty Name\"",
                        RedirectStandardOutput = true,
                        RedirectStandardError = true,
                        UseShellExecute = false,
                        CreateNoWindow = true
                    }
                };
                process.Start();
                var output = process.StandardOutput.ReadToEnd();
                process.WaitForExit();

                foreach (var line in output.Split('\n'))
                {
                    var trimmed = line.Trim();
                    if (!string.IsNullOrEmpty(trimmed))
                        printers.Add(trimmed);
                }

                Console.WriteLine($"[PrintService] Detectadas {printers.Count} impresora(s) en Windows: {string.Join(", ", printers)}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[PrintService] Error detectando impresoras Windows: {ex.Message}");
            }
            return printers.Count > 0
                ? printers
                : new List<string> { "(No se encontraron impresoras)" };
        }

        /// <summary>macOS: delega a MacCupsPrinter que usa lpstat (CUPS).</summary>
        private List<string> GetMacPrinters()
        {
            return MacCupsPrinter.GetAvailablePrinters();
        }

        // ============================================================
        // IMPRESIÓN
        // ============================================================

        /// <summary>
        /// Imprime texto usando la configuración actual (impresora y formato).
        /// Punto de entrada principal para todos los módulos.
        /// Retorna un PrintResult con la razón exacta si algo falla.
        /// </summary>
        public async Task<PrintResult> PrintAsync(string content)
        {
            var config = _configService.PosTerminalConfig;

            if (string.IsNullOrEmpty(config.PrinterName))
            {
                Console.WriteLine("[PrintService] No hay impresora configurada");
                return PrintResult.Fail(
                    PrintFailReason.NoPrinterConfigured,
                    "No hay impresora configurada en esta terminal. " +
                    "Ve a Configuración → Impresora para seleccionar una.");
            }

            bool isThermal = config.PrintFormat == "Térmica" || config.PrintFormat == "thermal";

            bool ok = isThermal
                ? await PrintThermalAsync(content, config.PrinterName)
                : await PrintLetterAsync(content, config.PrinterName, config);

            return ok
                ? PrintResult.Ok()
                : PrintResult.Fail(
                    PrintFailReason.DriverError,
                    $"Error al enviar el ticket a '{config.PrinterName}'. " +
                    "Verifique que la impresora esté encendida y conectada.");
        }

        /// <summary>
        /// Impresión térmica: envía texto plano directamente a la impresora.
        /// Ideal para impresoras de tickets de 58mm y 80mm.
        /// </summary>
        public async Task<bool> PrintThermalAsync(string text, string printerName)
        {
            try
            {
                // Crear archivo temporal con el contenido del ticket
                var tempFile = Path.Combine(Path.GetTempPath(), $"ticket_{Guid.NewGuid()}.txt");
                await File.WriteAllTextAsync(tempFile, text);

                bool result;

                if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                {
                    result = await PrintFileWindows(tempFile, printerName);
                }
                else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
                {
                    result = await PrintFileMac(tempFile, printerName);
                }
                else
                {
                    Console.WriteLine("[PrintService] SO no soportado para impresión");
                    result = false;
                }

                // Limpiar archivo temporal
                try { File.Delete(tempFile); } catch { /* ignorar */ }

                return result;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[PrintService] Error en impresión térmica: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Impresión en hoja carta: delega a LetterPrinter según el SO.
        /// macOS: CUPS lp con media=Letter (cpi=10, lpi=6).
        /// Windows: notepad.exe /PT con renderizado GDI y Courier New.
        /// Imprime automáticamente sin diálogo, igual que la impresión térmica.
        /// </summary>
        public async Task<bool> PrintLetterAsync(string text, string printerName, PosTerminalConfig config)
        {
            return await LetterPrinter.PrintAsync(text, printerName, config);
        }

        /// <summary>
        /// Imprime un ticket de venta usando TicketService + configuración.
        /// </summary>
        public async Task<PrintResult> PrintSaleTicketAsync(string ticketText)
        {
            return await PrintAsync(ticketText);
        }

        /// <summary>
        /// Imprime un ticket de corte de caja.
        /// </summary>
        public async Task<PrintResult> PrintCashCloseTicketAsync(string cashCloseText)
        {
            return await PrintAsync(cashCloseText);
        }

        // ============================================================
        // MÉTODOS PRIVADOS DE IMPRESIÓN POR SO
        // ============================================================

        /// <summary>
        /// Envía el texto del ticket directamente al driver de la impresora en Windows
        /// usando winspool.drv (P/Invoke RAW). Mismo principio que lp en macOS:
        /// el driver recibe el contenido sin transformación del spooler.
        /// Compatible con Xprinter y cualquier impresora con driver instalado.
        /// </summary>
        private async Task<bool> PrintFileWindows(string filePath, string printerName)
        {
            // Task.Run porque WindowsRawPrinter es síncrono (P/Invoke)
            return await Task.Run(() =>
            {
                try
                {
                    var text = File.ReadAllText(filePath);
                    Console.WriteLine($"[PrintService] Enviando {text.Length} chars a '{printerName}' via WindowsRawPrinter...");
                    var success = WindowsRawPrinter.SendText(printerName, text);
                    if (success)
                        Console.WriteLine($"[PrintService] Ticket enviado correctamente a '{printerName}'");
                    return success;
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[PrintService] Error imprimiendo en Windows: {ex.Message}");
                    return false;
                }
            });
        }

        /// <summary>
        /// Envía un archivo a la impresora en macOS.
        /// Delega a MacCupsPrinter que gestiona lp (CUPS) con los parámetros correctos.
        /// </summary>
        private async Task<bool> PrintFileMac(string filePath, string printerName)
        {
            var fontSize = _configService.PosTerminalConfig.FontSize;
            return await MacCupsPrinter.PrintFileAsync(filePath, printerName, fontSize);
        }

    }
}
