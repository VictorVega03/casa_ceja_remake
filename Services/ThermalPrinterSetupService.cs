using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading.Tasks;

namespace CasaCejaRemake.Services
{
    /// <summary>
    /// Servicio para configuración automática de impresoras térmicas.
    /// Detecta el driver correcto y configura la impresora automáticamente.
    /// </summary>
    public class ThermalPrinterSetupService
    {
        public class SetupResult
        {
            public bool Success { get; set; }
            public string Message { get; set; } = string.Empty;
            public string? PrinterName { get; set; }
            public string? DriverModel { get; set; }
            public List<string> Log { get; set; } = new();
        }

        /// <summary>
        /// Configura automáticamente una impresora térmica en macOS.
        /// </summary>
        public async Task<SetupResult> AutoConfigureThermalPrinterMacAsync(string printerName)
        {
            var result = new SetupResult();

            try
            {
                result.Log.Add("Iniciando configuración automática de impresora térmica...");

                // 1. Verificar que el driver Xprinter esté instalado
                result.Log.Add("Buscando drivers Xprinter instalados...");
                var availableDrivers = await GetAvailableXprinterDriversMacAsync();
                
                if (availableDrivers.Count == 0)
                {
                    result.Success = false;
                    result.Message = "No se encontraron drivers Xprinter instalados. Por favor, instale el driver desde el sitio oficial de Xprinter.";
                    result.Log.Add("ERROR: No se encontraron drivers Xprinter");
                    return result;
                }

                result.Log.Add($"Encontrados {availableDrivers.Count} drivers: {string.Join(", ", availableDrivers)}");

                // 2. Detectar la impresora conectada por USB
                result.Log.Add("Buscando impresora Xprinter conectada...");
                var printerUri = await DetectXprinterUsbMacAsync();
                
                if (string.IsNullOrEmpty(printerUri))
                {
                    result.Success = false;
                    result.Message = "No se detectó ninguna impresora Xprinter conectada por USB. Verifique la conexión.";
                    result.Log.Add("ERROR: No se detectó impresora USB");
                    return result;
                }

                result.Log.Add($"Impresora detectada: {printerUri}");

                // 3. Determinar el modelo más apropiado (preferir XP-80 que es el más común)
                string selectedDriver;
                if (availableDrivers.Contains("XP-80"))
                {
                    selectedDriver = "XP-80";
                }
                else if (availableDrivers.Contains("XP-58"))
                {
                    selectedDriver = "XP-58";
                }
                else
                {
                    selectedDriver = availableDrivers.First();
                }

                result.Log.Add($"Driver seleccionado: {selectedDriver}");
                result.DriverModel = selectedDriver;

                // 4. Eliminar configuración anterior si existe
                result.Log.Add("Eliminando configuración anterior (si existe)...");
                await RemovePrinterMacAsync(printerName);

                // 5. Configurar la impresora con el driver correcto
                result.Log.Add($"Configurando impresora con driver {selectedDriver}...");
                var setupSuccess = await ConfigurePrinterMacAsync(printerName, printerUri, selectedDriver);

                if (!setupSuccess)
                {
                    result.Success = false;
                    result.Message = "Error al configurar la impresora. Verifique los permisos de administrador.";
                    result.Log.Add("ERROR: Falló la configuración");
                    return result;
                }

                // 6. Habilitar la impresora
                result.Log.Add("Habilitando impresora...");
                await EnablePrinterMacAsync(printerName);

                // 7. Hacer prueba de impresión
                result.Log.Add("Realizando prueba de impresión...");
                var testSuccess = await PrintTestTicketMacAsync(printerName);

                result.Success = true;
                result.PrinterName = printerName;
                result.Message = testSuccess 
                    ? $"✓ Impresora configurada correctamente con driver {selectedDriver}. Ticket de prueba enviado."
                    : $"✓ Impresora configurada con driver {selectedDriver}, pero la prueba de impresión falló. Verifique que la impresora esté encendida.";
                
                result.Log.Add("Configuración completada exitosamente");

                return result;
            }
            catch (Exception ex)
            {
                result.Success = false;
                result.Message = $"Error inesperado: {ex.Message}";
                result.Log.Add($"ERROR CRÍTICO: {ex.Message}");
                return result;
            }
        }

        /// <summary>
        /// Obtiene la lista de drivers Xprinter instalados en macOS.
        /// </summary>
        private async Task<List<string>> GetAvailableXprinterDriversMacAsync()
        {
            var drivers = new List<string>();
            
            try
            {
                var process = new Process
                {
                    StartInfo = new ProcessStartInfo
                    {
                        FileName = "lpinfo",
                        Arguments = "-m",
                        RedirectStandardOutput = true,
                        UseShellExecute = false,
                        CreateNoWindow = true
                    }
                };

                process.Start();
                var output = await process.StandardOutput.ReadToEndAsync();
                await process.WaitForExitAsync();

                // Buscar drivers Xprinter (XP-58, XP-80, etc.)
                foreach (var line in output.Split('\n'))
                {
                    if (line.Contains("XP-58.ppd"))
                        drivers.Add("XP-58");
                    else if (line.Contains("XP-80.ppd"))
                        drivers.Add("XP-80");
                    else if (line.Contains("XP-76.ppd"))
                        drivers.Add("XP-76");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ThermalSetup] Error buscando drivers: {ex.Message}");
            }

            return drivers;
        }

        /// <summary>
        /// Detecta la URI de la impresora Xprinter conectada por USB.
        /// </summary>
        private async Task<string?> DetectXprinterUsbMacAsync()
        {
            try
            {
                var process = new Process
                {
                    StartInfo = new ProcessStartInfo
                    {
                        FileName = "lpinfo",
                        Arguments = "-v",
                        RedirectStandardOutput = true,
                        UseShellExecute = false,
                        CreateNoWindow = true
                    }
                };

                process.Start();
                var output = await process.StandardOutput.ReadToEndAsync();
                await process.WaitForExitAsync();

                // Buscar línea que contenga "usb://Xprinter"
                foreach (var line in output.Split('\n'))
                {
                    if (line.Contains("usb://Xprinter"))
                    {
                        // Extraer la URI: "direct usb://Xprinter/USB%20Printer..."
                        var parts = line.Split(' ');
                        foreach (var part in parts)
                        {
                            if (part.StartsWith("usb://Xprinter"))
                                return part;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ThermalSetup] Error detectando USB: {ex.Message}");
            }

            return null;
        }

        /// <summary>
        /// Elimina una impresora existente en macOS.
        /// </summary>
        private async Task RemovePrinterMacAsync(string printerName)
        {
            try
            {
                var process = new Process
                {
                    StartInfo = new ProcessStartInfo
                    {
                        FileName = "lpadmin",
                        Arguments = $"-x {printerName}",
                        RedirectStandardError = true,
                        UseShellExecute = false,
                        CreateNoWindow = true
                    }
                };

                process.Start();
                await process.WaitForExitAsync();
            }
            catch
            {
                // Ignorar errores si la impresora no existe
            }
        }

        /// <summary>
        /// Configura la impresora con el driver especificado en macOS.
        /// </summary>
        private async Task<bool> ConfigurePrinterMacAsync(string printerName, string deviceUri, string driverModel)
        {
            try
            {
                var ppdPath = $"/Library/Printers/PPDs/Contents/Resources/{driverModel}.ppd";
                
                var process = new Process
                {
                    StartInfo = new ProcessStartInfo
                    {
                        FileName = "lpadmin",
                        Arguments = $"-p {printerName} -v \"{deviceUri}\" -P \"{ppdPath}\" -E -o printer-is-shared=false",
                        RedirectStandardError = true,
                        RedirectStandardOutput = true,
                        UseShellExecute = false,
                        CreateNoWindow = true
                    }
                };

                process.Start();
                var error = await process.StandardError.ReadToEndAsync();
                await process.WaitForExitAsync();

                // Ignorar advertencias sobre drivers obsoletos
                return process.ExitCode == 0 || error.Contains("quedarán obsoletos");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ThermalSetup] Error configurando: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Habilita la impresora en macOS.
        /// </summary>
        private async Task EnablePrinterMacAsync(string printerName)
        {
            try
            {
                // Habilitar impresora
                var process1 = new Process
                {
                    StartInfo = new ProcessStartInfo
                    {
                        FileName = "cupsenable",
                        Arguments = printerName,
                        UseShellExecute = false,
                        CreateNoWindow = true
                    }
                };
                process1.Start();
                await process1.WaitForExitAsync();

                // Aceptar trabajos
                var process2 = new Process
                {
                    StartInfo = new ProcessStartInfo
                    {
                        FileName = "cupsaccept",
                        Arguments = printerName,
                        UseShellExecute = false,
                        CreateNoWindow = true
                    }
                };
                process2.Start();
                await process2.WaitForExitAsync();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ThermalSetup] Error habilitando: {ex.Message}");
            }
        }

        /// <summary>
        /// Imprime un ticket de prueba en la impresora.
        /// </summary>
        private async Task<bool> PrintTestTicketMacAsync(string printerName)
        {
            try
            {
                var testTicket = @"
========================================
       CASA CEJA - PRUEBA
========================================

Impresora configurada correctamente :D

Fecha: " + DateTime.Now.ToString("dd/MM/yyyy HH:mm") + @"

Este es un ticket de prueba
para verificar que la impresora
funciona correctamente.

========================================
      Sistema POS Casa Ceja
========================================


";

                var tempFile = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "test_setup.txt");
                await System.IO.File.WriteAllTextAsync(tempFile, testTicket);

                var process = new Process
                {
                    StartInfo = new ProcessStartInfo
                    {
                        FileName = "lp",
                        Arguments = $"-d {printerName} \"{tempFile}\"",
                        RedirectStandardError = true,
                        UseShellExecute = false,
                        CreateNoWindow = true
                    }
                };

                process.Start();
                await process.WaitForExitAsync();

                // Limpiar archivo temporal
                try { System.IO.File.Delete(tempFile); } catch { }

                return process.ExitCode == 0;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ThermalSetup] Error imprimiendo prueba: {ex.Message}");
                return false;
            }
        }
    }
}
