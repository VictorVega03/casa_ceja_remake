using System;
using System.Diagnostics;
using System.Threading.Tasks;

namespace CasaCejaRemake.Services.Platform
{
    /// <summary>
    /// Envía texto a una impresora en macOS usando CUPS (lp).
    /// Equivalente macOS de WindowsRawPrinter: encapsula toda la lógica
    /// de invocación de lp con los parámetros de formato correctos.
    /// 
    /// Parámetros clave:
    ///   cpi (characters per inch): controla el tamaño de fuente horizontal.
    ///        Menor FontSize → mayor cpi → letra más pequeña → caben más chars por línea.
    ///   lpi (lines per inch): controla el espaciado vertical.
    /// 
    /// Relación FontSize → cpi para papel de 58mm (~40 chars de ancho útil):
    ///   FontSize 8  → cpi=17  (~41 chars/línea)
    ///   FontSize 9  → cpi=15  (~38 chars/línea)
    ///   FontSize 10 → cpi=13  (~33 chars/línea)
    ///   FontSize 11 → cpi=12  (~30 chars/línea)
    ///   FontSize 12 → cpi=10  (~27 chars/línea)
    /// </summary>
    internal static class MacCupsPrinter
    {
        /// <summary>
        /// Envía el archivo de ticket a la impresora usando lp (CUPS).
        /// </summary>
        /// <param name="filePath">Ruta al archivo .txt con el contenido del ticket (UTF-8).</param>
        /// <param name="printerName">Nombre de la impresora tal como aparece en lpstat -a.</param>
        /// <param name="fontSize">FontSize configurado por el usuario (determina cpi/lpi).</param>
        /// <returns>true si lp retornó exit code 0.</returns>
        public static async Task<bool> PrintFileAsync(string filePath, string printerName, int fontSize)
        {
            try
            {
                // Derivar cpi y lpi del FontSize configurado.
                // cpi = characters per inch: controla ancho de carácter.
                int cpi = fontSize switch
                {
                    8  => 17,
                    9  => 15,
                    10 => 13,
                    11 => 12,
                    12 => 10,
                    _  => 15   // default seguro para 58mm
                };

                // lpi = lines per inch: fuentes más pequeñas permiten más densidad vertical.
                int lpi = fontSize <= 9 ? 9 : 8;

                Console.WriteLine($"[MacCupsPrinter] Enviando a '{printerName}' — cpi={cpi}, lpi={lpi} (FontSize={fontSize})");
                Console.WriteLine($"[MacCupsPrinter] Archivo: {filePath}");

                var process = new Process
                {
                    StartInfo = new ProcessStartInfo
                    {
                        FileName = "lp",
                        Arguments = $"-d \"{printerName}\" -o cpi={cpi} -o lpi={lpi} \"{filePath}\"",
                        RedirectStandardOutput = true,
                        RedirectStandardError  = true,
                        UseShellExecute  = false,
                        CreateNoWindow   = true
                    }
                };

                process.Start();
                var stdOut = await process.StandardOutput.ReadToEndAsync();
                var stdErr = await process.StandardError.ReadToEndAsync();
                await process.WaitForExitAsync();

                if (process.ExitCode != 0)
                {
                    Console.WriteLine($"[MacCupsPrinter] ✗ lp falló (exit {process.ExitCode}): {stdErr.Trim()}");
                    return false;
                }

                Console.WriteLine($"[MacCupsPrinter] ✓ Ticket enviado correctamente. lp: {stdOut.Trim()}");
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[MacCupsPrinter] ✗ Excepción: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Obtiene la lista de impresoras disponibles en macOS usando lpstat.
        /// Usa dos métodos (lpstat -a y lpstat -p) y deduplica con HashSet.
        /// </summary>
        public static System.Collections.Generic.List<string> GetAvailablePrinters()
        {
            var printers = new System.Collections.Generic.HashSet<string>();

            try
            {
                // Método 1: lpstat -a — impresoras que aceptan trabajos (más confiable para térmicas)
                // Formato válido: "NombreImpresora accepting/rejecting requests since ..."
                // Solo procesamos líneas con esas palabras clave para evitar que mensajes
                // de error de CUPS (ej. "no destinations added.") se tomen como nombres.
                var out1 = RunCommand("lpstat", "-a");
                foreach (var line in out1.Split('\n'))
                {
                    if (string.IsNullOrWhiteSpace(line)) continue;
                    if (!line.Contains("accepting") && !line.Contains("rejecting")) continue;
                    var parts = line.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                    if (parts.Length >= 1)
                        printers.Add(parts[0]);
                }

                // Método 2: lpstat -p — estado de impresoras (respaldo solo si Método 1 no encontró nada)
                if (printers.Count == 0)
                {
                    var out2 = RunCommand("lpstat", "-p");
                    foreach (var line in out2.Split('\n'))
                    {
                        if (string.IsNullOrWhiteSpace(line)) continue;

                        if (line.Contains("impresora "))
                        {
                            var parts = line.Split(new[] { "impresora " }, StringSplitOptions.None);
                            if (parts.Length >= 2)
                                printers.Add(parts[1].Split(' ')[0]);
                        }
                        else if (line.StartsWith("printer ", StringComparison.OrdinalIgnoreCase))
                        {
                            var parts = line.Split(' ');
                            if (parts.Length >= 2)
                                printers.Add(parts[1]);
                        }
                    }
                }

                // Filtrar entradas que claramente no son nombres de impresora
                // (falsos positivos de mensajes de CUPS como "no destinations added.")
                printers.RemoveWhere(p => p.Length <= 2);

                Console.WriteLine($"[MacCupsPrinter] Detectadas {printers.Count} impresora(s): " +
                                  $"{string.Join(", ", printers)}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[MacCupsPrinter] Error detectando impresoras: {ex.Message}");
            }

            return printers.Count > 0
                ? new System.Collections.Generic.List<string>(printers)
                : new System.Collections.Generic.List<string> { "(No se encontraron impresoras)" };
        }

        // ── Helpers ────────────────────────────────────────────────────────

        private static string RunCommand(string fileName, string arguments)
        {
            try
            {
                using var process = new Process
                {
                    StartInfo = new ProcessStartInfo
                    {
                        FileName = fileName,
                        Arguments = arguments,
                        RedirectStandardOutput = true,
                        UseShellExecute = false,
                        CreateNoWindow = true
                    }
                };
                process.Start();
                var output = process.StandardOutput.ReadToEnd();
                process.WaitForExit();
                return output;
            }
            catch
            {
                return string.Empty;
            }
        }
    }
}
