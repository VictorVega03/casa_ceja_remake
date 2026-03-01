using System;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;
using CasaCejaRemake.Helpers;
using CasaCejaRemake.Models;
using CasaCejaRemake.Services.Interfaces;

namespace CasaCejaRemake.Services
{
    /// <summary>
    /// Servicio para gestión de configuración de la aplicación.
    /// Maneja dos niveles:
    /// 1. AppConfig: Configuración general (sucursal actual) - Solo Admin
    /// 2. PosTerminalConfig: Configuración del terminal POS (impresora, tickets, etc.)
    /// </summary>
    public class ConfigService : IConfigService
    {
        private readonly string _appConfigPath;
        private readonly string _posTerminalConfigPath;
        private AppConfig _appConfig = new();
        private PosTerminalConfig _posTerminalConfig = new();

        /// <summary>Configuración general de la aplicación.</summary>
        public AppConfig AppConfig => _appConfig;

        /// <summary>Configuración del terminal POS.</summary>
        public PosTerminalConfig PosTerminalConfig => _posTerminalConfig;

        /// <summary>Se dispara cuando cambia la configuración general.</summary>
        public event EventHandler? AppConfigChanged;

        /// <summary>Se dispara cuando cambia la configuración del terminal POS.</summary>
        public event EventHandler? PosTerminalConfigChanged;

        public ConfigService()
        {
            // Misma carpeta que DatabaseService usa para la BD
            var appDataPath = Environment.GetFolderPath(
                Environment.SpecialFolder.ApplicationData);
            var casaCejaFolder = Path.Combine(appDataPath, Constants.APP_DATA_FOLDER);
            
            _appConfigPath = Path.Combine(casaCejaFolder, "app_config.json");
            _posTerminalConfigPath = Path.Combine(casaCejaFolder, "pos_terminal_config.json");
        }

        /// <summary>
        /// Carga ambas configuraciones desde disco. Si no existen, crea valores por defecto.
        /// Llamar una vez al iniciar la aplicación.
        /// </summary>
        public async Task LoadAsync()
        {
            await LoadAppConfigAsync();
            await LoadPosTerminalConfigAsync();
        }

        /// <summary>
        /// Carga la configuración general de la aplicación.
        /// </summary>
        private async Task LoadAppConfigAsync()
        {
            try
            {
                if (File.Exists(_appConfigPath))
                {
                    var json = await File.ReadAllTextAsync(_appConfigPath);
                    _appConfig = JsonSerializer.Deserialize<AppConfig>(json) ?? new AppConfig();
                    Console.WriteLine("[ConfigService] AppConfig cargada desde disco");
                }
                else
                {
                    _appConfig = new AppConfig();
                    await SaveAppConfigAsync();
                    Console.WriteLine("[ConfigService] AppConfig por defecto creada");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ConfigService] Error cargando AppConfig: {ex.Message}");
                _appConfig = new AppConfig();
            }
        }

        /// <summary>
        /// Carga la configuración del terminal POS.
        /// </summary>
        private async Task LoadPosTerminalConfigAsync()
        {
            try
            {
                if (File.Exists(_posTerminalConfigPath))
                {
                    var json = await File.ReadAllTextAsync(_posTerminalConfigPath);
                    _posTerminalConfig = JsonSerializer.Deserialize<PosTerminalConfig>(json) ?? new PosTerminalConfig();
                    Console.WriteLine("[ConfigService] PosTerminalConfig cargada desde disco");
                }
                else
                {
                    _posTerminalConfig = new PosTerminalConfig();
                    await SavePosTerminalConfigAsync();
                    Console.WriteLine("[ConfigService] PosTerminalConfig por defecto creada");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ConfigService] Error cargando PosTerminalConfig: {ex.Message}");
                _posTerminalConfig = new PosTerminalConfig();
            }
        }

        /// <summary>
        /// Guarda la configuración general de la aplicación.
        /// </summary>
        public async Task SaveAppConfigAsync()
        {
            try
            {
                var directory = Path.GetDirectoryName(_appConfigPath);
                if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
                    Directory.CreateDirectory(directory);

                _appConfig.LastModified = DateTime.Now;

                var options = new JsonSerializerOptions { WriteIndented = true };
                var json = JsonSerializer.Serialize(_appConfig, options);
                await File.WriteAllTextAsync(_appConfigPath, json);

                Console.WriteLine("[ConfigService] AppConfig guardada en disco");
                AppConfigChanged?.Invoke(this, EventArgs.Empty);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ConfigService] Error guardando AppConfig: {ex.Message}");
            }
        }

        /// <summary>
        /// Guarda la configuración del terminal POS.
        /// </summary>
        public async Task SavePosTerminalConfigAsync()
        {
            try
            {
                var directory = Path.GetDirectoryName(_posTerminalConfigPath);
                if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
                    Directory.CreateDirectory(directory);

                _posTerminalConfig.LastModified = DateTime.Now;

                var options = new JsonSerializerOptions { WriteIndented = true };
                var json = JsonSerializer.Serialize(_posTerminalConfig, options);
                await File.WriteAllTextAsync(_posTerminalConfigPath, json);

                Console.WriteLine("[ConfigService] PosTerminalConfig guardada en disco");
                PosTerminalConfigChanged?.Invoke(this, EventArgs.Empty);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ConfigService] Error guardando PosTerminalConfig: {ex.Message}");
            }
        }

        /// <summary>
        /// Actualiza la configuración general y guarda automáticamente.
        /// </summary>
        public async Task UpdateAppConfigAsync(Action<AppConfig> updateAction)
        {
            updateAction(_appConfig);
            await SaveAppConfigAsync();
        }

        /// <summary>
        /// Actualiza la configuración del terminal POS y guarda automáticamente.
        /// </summary>
        public async Task UpdatePosTerminalConfigAsync(Action<PosTerminalConfig> updateAction)
        {
            updateAction(_posTerminalConfig);
            await SavePosTerminalConfigAsync();
        }
    }
}
