using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using CasaCejaRemake.Models;

namespace CasaCejaRemake.Services
{
    /// <summary>
    /// Servicio de impresión multiplataforma.
    /// Soporta impresoras térmicas (ticket) y convencionales (carta).
    /// </summary>
    public class PrintService
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
                var process = new Process
                {
                    StartInfo = new ProcessStartInfo
                    {
                        FileName = "wmic",
                        Arguments = "printer get name",
                        RedirectStandardOutput = true,
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
                    if (!string.IsNullOrEmpty(trimmed) && trimmed != "Name")
                        printers.Add(trimmed);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[PrintService] Error detectando impresoras Windows: {ex.Message}");
            }
            return printers;
        }

        /// <summary>macOS: usa lpstat (CUPS) para listar impresoras.</summary>
        private List<string> GetMacPrinters()
        {
            var printers = new List<string>();
            try
            {
                var process = new Process
                {
                    StartInfo = new ProcessStartInfo
                    {
                        FileName = "lpstat",
                        Arguments = "-p",
                        RedirectStandardOutput = true,
                        UseShellExecute = false,
                        CreateNoWindow = true
                    }
                };
                process.Start();
                var output = process.StandardOutput.ReadToEnd();
                process.WaitForExit();

                foreach (var line in output.Split('\n'))
                {
                    // Formato: "printer NOMBRE_IMPRESORA is idle..."
                    if (line.StartsWith("printer "))
                    {
                        var parts = line.Split(' ');
                        if (parts.Length >= 2)
                            printers.Add(parts[1]);
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[PrintService] Error detectando impresoras macOS: {ex.Message}");
            }
            return printers;
        }

        // ============================================================
        // IMPRESIÓN
        // ============================================================

        /// <summary>
        /// Imprime texto usando la configuración actual (impresora y formato).
        /// Punto de entrada principal para todos los módulos.
        /// </summary>
        public async Task<bool> PrintAsync(string content)
        {
            var config = _configService.PosTerminalConfig;

            if (string.IsNullOrEmpty(config.PrinterName))
            {
                Console.WriteLine("[PrintService] No hay impresora configurada");
                return false;
            }

            return config.PrintFormat == "Térmica" || config.PrintFormat == "thermal"
                ? await PrintThermalAsync(content, config.PrinterName)
                : await PrintLetterAsync(content, config.PrinterName, config);
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
        /// Impresión en hoja carta: genera formato con márgenes y tipografía.
        /// Para impresoras láser/inyección convencionales.
        /// </summary>
        public async Task<bool> PrintLetterAsync(string text, string printerName, PosTerminalConfig config)
        {
            try
            {
                // Para formato carta, agregar encabezado con formato
                var formattedText = FormatForLetter(text, config);
                var tempFile = Path.Combine(Path.GetTempPath(), $"letter_{Guid.NewGuid()}.txt");
                await File.WriteAllTextAsync(tempFile, formattedText);

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
                    result = false;
                }

                // Limpiar archivo temporal
                try { File.Delete(tempFile); } catch { /* ignorar */ }

                return result;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[PrintService] Error en impresión carta: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Imprime un ticket de venta usando TicketService + configuración.
        /// </summary>
        public async Task<bool> PrintSaleTicketAsync(string ticketText)
        {
            return await PrintAsync(ticketText);
        }

        /// <summary>
        /// Imprime un ticket de corte de caja.
        /// </summary>
        public async Task<bool> PrintCashCloseTicketAsync(string cashCloseText)
        {
            return await PrintAsync(cashCloseText);
        }

        // ============================================================
        // MÉTODOS PRIVADOS DE IMPRESIÓN POR SO
        // ============================================================

        /// <summary>
        /// Envía un archivo a la impresora en Windows.
        /// </summary>
        private async Task<bool> PrintFileWindows(string filePath, string printerName)
        {
            try
            {
                var process = new Process
                {
                    StartInfo = new ProcessStartInfo
                    {
                        FileName = "cmd.exe",
                        Arguments = $"/c print /d:\"{printerName}\" \"{filePath}\"",
                        RedirectStandardOutput = true,
                        RedirectStandardError = true,
                        UseShellExecute = false,
                        CreateNoWindow = true
                    }
                };

                process.Start();
                await process.WaitForExitAsync();

                if (process.ExitCode != 0)
                {
                    var error = await process.StandardError.ReadToEndAsync();
                    Console.WriteLine($"[PrintService] Error Windows: {error}");
                    return false;
                }

                Console.WriteLine($"[PrintService] Impreso correctamente en {printerName}");
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[PrintService] Error imprimiendo en Windows: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Envía un archivo a la impresora en macOS usando lp (CUPS).
        /// </summary>
        private async Task<bool> PrintFileMac(string filePath, string printerName)
        {
            try
            {
                var process = new Process
                {
                    StartInfo = new ProcessStartInfo
                    {
                        FileName = "lp",
                        Arguments = $"-d \"{printerName}\" \"{filePath}\"",
                        RedirectStandardOutput = true,
                        RedirectStandardError = true,
                        UseShellExecute = false,
                        CreateNoWindow = true
                    }
                };

                process.Start();
                await process.WaitForExitAsync();

                if (process.ExitCode != 0)
                {
                    var error = await process.StandardError.ReadToEndAsync();
                    Console.WriteLine($"[PrintService] Error macOS: {error}");
                    return false;
                }

                Console.WriteLine($"[PrintService] Impreso correctamente en {printerName} (macOS)");
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[PrintService] Error imprimiendo en macOS: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Formatea el texto para impresión en hoja carta (agrega márgenes, etc.)
        /// </summary>
        private string FormatForLetter(string text, PosTerminalConfig config)
        {
            var lines = new List<string>
            {
                "", // Margen superior
                ""
            };

            // Agregar el contenido con indentación para simular margen izquierdo
            foreach (var line in text.Split(Environment.NewLine))
            {
                lines.Add($"    {line}"); // 4 espacios de margen
            }

            lines.Add("");
            lines.Add($"    {config.TicketFooter}");
            lines.Add("");
            lines.Add($"    Impreso: {DateTime.Now:dd/MM/yyyy HH:mm:ss}");

            return string.Join(Environment.NewLine, lines);
        }
    }
}
