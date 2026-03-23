using System;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using CasaCejaRemake.Models.DTOs;

namespace CasaCejaRemake.Services
{
    
    /// Cliente HTTP para comunicarse con el servidor Laravel.
    /// Maneja autenticación por token, reintentos y timeout.
    
    public class ApiClient
    {
        private readonly HttpClient _httpClient;
        private readonly ConfigService _configService;

        private const int MaxRetries    = 3;
        private const int MaxGetRetries = 1;
        private const int TimeoutSeconds = 15;

        public ApiClient(ConfigService configService)
        {
            _configService = configService;
            _httpClient = new HttpClient
            {
                Timeout = TimeSpan.FromSeconds(TimeoutSeconds)
            };
        }

        // ──────────────────────────────────────────────────────
        // CONFIGURACIÓN
        // ──────────────────────────────────────────────────────

        private string BaseUrl => _configService.AppConfig.ServerUrl.TrimEnd('/');
        private string Token   => _configService.AppConfig.UserToken;

        private void SetAuthHeader()
        {
            _httpClient.DefaultRequestHeaders.Remove("X-User-Token");
            if (!string.IsNullOrEmpty(Token))
                _httpClient.DefaultRequestHeaders.Add("X-User-Token", Token);
            }

        // ──────────────────────────────────────────────────────
        // HEALTH CHECK
        // ──────────────────────────────────────────────────────

        
        /// Verifica si el servidor está disponible.
        /// No requiere token.
        
     public async Task<bool> IsServerAvailableAsync()
    {
        try
        {
            var response = await _httpClient.GetAsync($"{BaseUrl}/api/v1/health");
            Console.WriteLine($"[ApiClient] Health check: {response.StatusCode}");
            return response.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[ApiClient] Health check falló: {ex.Message}");
            return false;
        }
    }

        
        /// Obtiene el server_time del servidor para usarlo como LastSyncTimestamp.
        
        public async Task<long> GetServerTimeAsync()
        {
            try
            {
                var response = await _httpClient.GetAsync($"{BaseUrl}/api/v1/health");
                if (!response.IsSuccessStatusCode) return 0;

                var json = await response.Content.ReadAsStringAsync();
                var health = JsonSerializer.Deserialize<HealthResponse>(json);
                return health?.ServerTime ?? 0;
            }
            catch
            {
                return 0;
            }
        }

    /// Hace login al servidor y regresa el token del usuario.
    /// Endpoint público — no requiere token previo.
    /// 
    public async Task<ApiResponse<LoginResponse>?> LoginAsync(string username, string password)
    {
        var url  = $"{BaseUrl}/api/v1/auth/login";
        var body = new LoginRequest { Username = username, Password = password };

        try
        {
            var response = await _httpClient.PostAsJsonAsync(url, body);
            var json     = await response.Content.ReadAsStringAsync();

            if (response.IsSuccessStatusCode)
                return JsonSerializer.Deserialize<ApiResponse<LoginResponse>>(json);

            Console.WriteLine($"[ApiClient] Login fallido: {response.StatusCode}");
            return null;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[ApiClient] Error en login: {ex.Message}");
            return null;
        }
    }
        // ──────────────────────────────────────────────────────
        // GET (PULL)
        // ──────────────────────────────────────────────────────

        
        /// Hace un GET al servidor con reintentos.
        /// Usado para Pull de catálogos y operaciones.
        
        public async Task<ApiResponse<T>?> GetAsync<T>(string endpoint, CancellationToken ct = default)
        {
            SetAuthHeader();
            var url = $"{BaseUrl}{endpoint}";

            for (int attempt = 1; attempt <= MaxGetRetries; attempt++)
            {
                try
                {
                    var response = await _httpClient.GetAsync(url, ct);
                    var json     = await response.Content.ReadAsStringAsync(ct);

                    if (response.IsSuccessStatusCode)
                        return JsonSerializer.Deserialize<ApiResponse<T>>(json);

                    // No reintentar errores 4xx (son errores del cliente)
                    if ((int)response.StatusCode >= 400 && (int)response.StatusCode < 500)
                    {
                        Console.WriteLine($"[ApiClient] Error {response.StatusCode} en {endpoint}");
                        return null;
                    }
                }
                catch (TaskCanceledException)
                {
                    Console.WriteLine($"[ApiClient] Timeout en intento {attempt} para {endpoint}");
                }
                catch (HttpRequestException ex)
                {
                    Console.WriteLine($"[ApiClient] Error de red en intento {attempt}: {ex.Message}");
                }

                if (attempt < MaxRetries)
                    await Task.Delay(TimeSpan.FromSeconds(Math.Pow(2, attempt)), ct);
            }

            return null;
        }

        // ──────────────────────────────────────────────────────
        // POST (PUSH)
        // ──────────────────────────────────────────────────────

        
        /// Hace un POST al servidor con reintentos.
        /// Usado para Push de ventas, cortes, entradas, etc.
        
        public async Task<ApiResponse<T>?> PostAsync<T>(string endpoint, object body, CancellationToken ct = default)
        {
            SetAuthHeader();
            var url = $"{BaseUrl}{endpoint}";

            for (int attempt = 1; attempt <= MaxRetries; attempt++)
            {
                try
                {
                    var response = await _httpClient.PostAsJsonAsync(url, body, ct);
                    var json     = await response.Content.ReadAsStringAsync(ct);

                    if (response.IsSuccessStatusCode || (int)response.StatusCode == 207)
                        return JsonSerializer.Deserialize<ApiResponse<T>>(json);

                    if ((int)response.StatusCode >= 400 && (int)response.StatusCode < 500)
                    {
                        Console.WriteLine($"[ApiClient] Error {response.StatusCode} en {endpoint}");
                        return null;
                    }
                }
                catch (TaskCanceledException)
                {
                    Console.WriteLine($"[ApiClient] Timeout en intento {attempt} para {endpoint}");
                }
                catch (HttpRequestException ex)
                {
                    Console.WriteLine($"[ApiClient] Error de red en intento {attempt}: {ex.Message}");
                }

                if (attempt < MaxRetries)
                    await Task.Delay(TimeSpan.FromSeconds(Math.Pow(2, attempt)), ct);
            }

            return null;
        }

        // ──────────────────────────────────────────────────────
        // PUT (ON-DEMAND)
        // ──────────────────────────────────────────────────────

        
        /// Hace un PUT al servidor.
        /// Usado para operaciones OnDemand como actualizar stock.
        
        public async Task<ApiResponse<T>?> PutAsync<T>(string endpoint, object body, CancellationToken ct = default)
        {
            SetAuthHeader();
            var url = $"{BaseUrl}{endpoint}";

            try
            {
                var response = await _httpClient.PutAsJsonAsync(url, body, ct);
                var json     = await response.Content.ReadAsStringAsync(ct);

                if (response.IsSuccessStatusCode)
                    return JsonSerializer.Deserialize<ApiResponse<T>>(json);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ApiClient] Error en PUT {endpoint}: {ex.Message}");
            }

            return null;
        }
    }
}