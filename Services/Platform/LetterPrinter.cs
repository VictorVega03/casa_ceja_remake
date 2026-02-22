using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using CasaCejaRemake.Models;

namespace CasaCejaRemake.Services.Platform
{
    /// <summary>
    /// Impresión automática en hoja carta (Letter 8.5"×11") para impresoras convencionales.
    /// Sin diálogo nativo — imprime directamente a la impresora configurada,
    /// igual que la impresión térmica.
    ///
    /// macOS:   CUPS lp con media=Letter (cpi=10, lpi=6 para texto estándar).
    /// Windows: notepad.exe /PT para renderizado GDI con Courier New (monoespaciado).
    ///
    /// Patrón: misma estructura que MacCupsPrinter y WindowsRawPrinter.
    /// </summary>
    internal static class LetterPrinter
    {
        // ============================================================
        // API PÚBLICA
        // ============================================================

        /// <summary>
        /// Imprime texto en hoja carta de forma automática.
        /// Formatea el contenido con márgenes, escribe un archivo temporal
        /// y lo envía a la impresora según el SO detectado.
        /// </summary>
        /// <param name="text">Texto del ticket ya generado (ASCII monoespaciado).</param>
        /// <param name="printerName">Nombre de la impresora tal como aparece en el SO.</param>
        /// <param name="config">Configuración del terminal (footer, etc.).</param>
        /// <returns>true si el trabajo de impresión se envió correctamente.</returns>
        public static async Task<bool> PrintAsync(string text, string printerName, PosTerminalConfig config)
        {
            string? tempFile = null;
            try
            {
                // 1. Formatear contenido para hoja carta (márgenes + footer)
                var formatted = FormatForLetter(text, config);

                // 2. Archivo temporal (mismo patrón que MacCupsPrinter / PrintService)
                tempFile = Path.Combine(Path.GetTempPath(), $"letter_{Guid.NewGuid():N}.txt");
                await File.WriteAllTextAsync(tempFile, formatted, System.Text.Encoding.UTF8);

                Console.WriteLine($"[LetterPrinter] Archivo temporal: {tempFile}");

                // 3. Delegar al método de plataforma correspondiente
                bool result;

                if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
                    result = await PrintMacAsync(tempFile, printerName);
                else if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                    result = await PrintWindowsAsync(tempFile, printerName);
                else
                {
                    Console.WriteLine("[LetterPrinter] SO no soportado para impresión carta.");
                    result = false;
                }

                return result;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[LetterPrinter] Error: {ex.Message}");
                return false;
            }
            finally
            {
                // 4. Limpiar archivo temporal (mismo patrón que PrintService)
                if (tempFile != null)
                    try { File.Delete(tempFile); } catch { /* ignorar */ }
            }
        }

        // ============================================================
        // IMPRESIÓN POR PLATAFORMA
        // ============================================================

        /// <summary>
        /// macOS: usa CUPS lp con parámetros de hoja carta.
        /// media=Letter fuerza el tamaño de papel correcto.
        /// cpi=10 → ~65 chars/línea en el ancho útil de carta (~6.5").
        /// lpi=6  → espaciado estándar de texto (6 líneas/pulgada).
        /// Misma estructura que MacCupsPrinter.PrintFileAsync().
        /// </summary>
        private static async Task<bool> PrintMacAsync(string filePath, string printerName)
        {
            try
            {
                // cpi=10 y lpi=6 son los valores estándar para texto en hoja carta
                var args = $"-d \"{printerName}\" -o media=Letter -o cpi=10 -o lpi=6 \"{filePath}\"";

                Console.WriteLine($"[LetterPrinter] macOS: lp {args}");

                var process = new Process
                {
                    StartInfo = new ProcessStartInfo
                    {
                        FileName               = "lp",
                        Arguments              = args,
                        RedirectStandardOutput = true,
                        RedirectStandardError  = true,
                        UseShellExecute        = false,
                        CreateNoWindow         = true
                    }
                };

                process.Start();
                var stdOut = await process.StandardOutput.ReadToEndAsync();
                var stdErr = await process.StandardError.ReadToEndAsync();
                await process.WaitForExitAsync();

                if (process.ExitCode != 0)
                {
                    Console.WriteLine($"[LetterPrinter] macOS: lp falló (exit {process.ExitCode}): {stdErr.Trim()}");
                    return false;
                }

                Console.WriteLine($"[LetterPrinter] macOS: ✓ Trabajo enviado. lp: {stdOut.Trim()}");
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[LetterPrinter] macOS: Excepción: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Windows: usa notepad.exe /PT para imprimir a la impresora especificada.
        /// notepad /PT renderiza con GDI usando Courier New (monoespaciado),
        /// lo que preserva el formato ASCII del ticket.
        /// Disponible en Windows 10/11 con el Notepad clásico (system32\notepad.exe).
        /// La ventana de Notepad puede aparecer brevemente minimizada.
        /// </summary>
        private static async Task<bool> PrintWindowsAsync(string filePath, string printerName)
        {
            // Task.Run porque WaitForExit puede bloquear (mismo patrón que WindowsRawPrinter)
            return await Task.Run(async () =>
            {
                try
                {
                    // Sintaxis: notepad.exe /PT <archivo> <impresora> <driver> <puerto>
                    // driver y puerto vacíos: Windows los resuelve automáticamente
                    var args = $"/PT \"{filePath}\" \"{printerName}\" \"\" \"\"";

                    Console.WriteLine($"[LetterPrinter] Windows: notepad.exe {args}");

                    var process = new Process
                    {
                        StartInfo = new ProcessStartInfo
                        {
                            FileName               = "notepad.exe",
                            Arguments              = args,
                            UseShellExecute        = false,
                            CreateNoWindow         = true,
                            RedirectStandardOutput = false,
                            RedirectStandardError  = false
                        }
                    };

                    process.Start();
                    await process.WaitForExitAsync();

                    // notepad /PT retorna 0 al imprimir exitosamente;
                    // ExitCode negativo indica error del proceso
                    if (process.ExitCode < 0)
                    {
                        Console.WriteLine($"[LetterPrinter] Windows: notepad falló (exit {process.ExitCode})");
                        return false;
                    }

                    Console.WriteLine($"[LetterPrinter] Windows: ✓ Trabajo enviado a '{printerName}'");
                    return true;
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[LetterPrinter] Windows: Excepción: {ex.Message}");
                    return false;
                }
            });
        }

        // ============================================================
        // FORMATEO PARA HOJA CARTA
        // ============================================================

        /// <summary>
        /// Agrega márgenes, footer y timestamp al texto del ticket para impresión carta.
        /// Centraliza la lógica que antes estaba en PrintService.FormatForLetter().
        /// </summary>
        private static string FormatForLetter(string text, PosTerminalConfig config)
        {
            var lines = new System.Collections.Generic.List<string>
            {
                "", // Margen superior
                ""
            };

            // Contenido con indentación para simular margen izquierdo
            foreach (var line in text.Split(Environment.NewLine))
            {
                lines.Add($"    {line}"); // 4 espacios de margen
            }

            lines.Add("");
            if (!string.IsNullOrWhiteSpace(config.TicketFooter))
                lines.Add($"    {config.TicketFooter}");
            lines.Add("");
            lines.Add($"    Impreso: {DateTime.Now:dd/MM/yyyy HH:mm:ss}");

            return string.Join(Environment.NewLine, lines);
        }
    }
}
