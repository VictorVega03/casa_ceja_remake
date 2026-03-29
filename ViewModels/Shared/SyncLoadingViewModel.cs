using CommunityToolkit.Mvvm.ComponentModel;
using CasaCejaRemake.Services;
using System;
using System.Threading.Tasks;

namespace CasaCejaRemake.ViewModels.Shared
{
    public partial class SyncLoadingViewModel : ObservableObject
    {
        private readonly ApiClient _apiClient;
        private readonly SyncService _syncService;
        private readonly ConfigService _configService;
        private readonly AuthService _authService;

        private string _statusMessage = "Conectando con el servidor...";
        public string StatusMessage
        {
            get => _statusMessage;
            set => SetProperty(ref _statusMessage, value);
        }

        private bool _isError = false;
        public bool IsError
        {
            get => _isError;
            set => SetProperty(ref _isError, value);
        }

        private bool _isSyncing = true;
        public bool IsSyncing
        {
            get => _isSyncing;
            set => SetProperty(ref _isSyncing, value);
        }

        public event EventHandler? SyncCompleted;
        public event EventHandler? ServerUnavailable;

        public SyncLoadingViewModel(
            ApiClient apiClient,
            SyncService syncService,
            ConfigService configService,
            AuthService authService)
        {
            _apiClient     = apiClient;
            _syncService   = syncService;
            _configService = configService;
            _authService   = authService;
        }

        public async Task StartAsync()
        {
            try
            {
                // Paso 1 — Verificar servidor
                StatusMessage = "Verificando conexión...";
                var serverAvailable = await _apiClient.IsServerAvailableAsync();

                if (!serverAvailable)
                {
                    StatusMessage = "Sin conexión — trabajando en modo offline";
                    IsSyncing = false;
                    await Task.Delay(600);
                    ServerUnavailable?.Invoke(this, EventArgs.Empty);
                    return;
                }
                // Paso 2 — Verificar token (ya fue guardado durante el login)
                StatusMessage = "Autenticando...";
                var token = _configService.AppConfig.UserToken;

                if (!string.IsNullOrWhiteSpace(token))
                {
                    Console.WriteLine($"[SyncLoading] Token disponible (len={token.Length})");
                }
                else
                {
                    Console.WriteLine("[SyncLoading] Sin token — se omite Push");
                }

                // Paso 3 — Push de operaciones pendientes
                int totalPushed = 0;
                int totalRejected = 0;
                if (!string.IsNullOrWhiteSpace(_configService.AppConfig.UserToken))
                {
                    StatusMessage = "Enviando datos pendientes...";
                    var pushResults = await _syncService.PushAllAsync();
                    foreach (var result in pushResults)
                    {
                        totalPushed    += result.RecordsPushed;
                        totalRejected  += result.RecordsRejected;
                    }
                }
                if (totalPushed > 0)
                    Console.WriteLine($"[SyncLoading] Push completado — {totalPushed} enviados, {totalRejected} rechazados");

                // Paso 3 — Pull de catálogos solo si hay cambios
                StatusMessage = "Verificando actualizaciones...";
                var lastSync = _configService.AppConfig.LastSyncTimestamp;
                var serverTime = await _apiClient.GetServerTimeAsync();

                // Solo sincronizar si han pasado más de 5 minutos desde el último sync
                // o si nunca se ha sincronizado
                var fiveMinutesAgo = DateTimeOffset.UtcNow.ToUnixTimeSeconds() - 300;
                Console.WriteLine($"[SyncLoading] lastSync={lastSync} fiveMinutesAgo={fiveMinutesAgo} shouldSync={lastSync == 0 || lastSync < fiveMinutesAgo}");
                bool shouldSync = lastSync == 0 || lastSync < fiveMinutesAgo;
                
                int totalPulled = 0;
                if (shouldSync)
                {
                    StatusMessage = "Sincronizando datos...";
                    var results = await _syncService.PullAllAsync();
                    foreach (var result in results)
                    {
                        if (result.Success)
                            totalPulled += result.RecordsPulled;
                    }
                }
                else
                {
                    Console.WriteLine($"[SyncLoading] Sync reciente — omitiendo pull");
                }

                // Paso 4 — Actualizar timestamp                
                if (serverTime > 0)
                {
                    await _configService.UpdateAppConfigAsync(config =>
                    {
                        config.LastSyncTimestamp = serverTime;
                    });
                }

                StatusMessage = totalPulled > 0
                    ? $"✓ Sincronización completa — {totalPulled} registros actualizados"
                    : "✓ Todo actualizado";

                await Task.Delay(800);
                SyncCompleted?.Invoke(this, EventArgs.Empty);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[SyncLoading] Error: {ex.Message}");
                StatusMessage = "Error de sincronización — continuando en modo offline";
                await Task.Delay(1500);
                SyncCompleted?.Invoke(this, EventArgs.Empty);
            }
            finally
            {
                IsSyncing = false;
            }
        }
    }
}