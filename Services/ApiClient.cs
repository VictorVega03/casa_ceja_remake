using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using CasaCejaRemake.Models.DTOs;

namespace CasaCejaRemake.Services
{
    public class ApiClient
    {
        private readonly HttpClient _httpClient;
        private readonly ConfigService _configService;
        private static readonly JsonSerializerOptions _jsonOptions = new()
        {
            PropertyNamingPolicy        = JsonNamingPolicy.SnakeCaseLower,
            PropertyNameCaseInsensitive = true,
            Converters =
            {
                new CasaCejaRemake.Helpers.DateTimeConverter(),
                new CasaCejaRemake.Helpers.NullableDateTimeConverter(),
            }
        };

        private const int MaxRetries     = 3;
        private const int MaxGetRetries  = 1;
        private const int TimeoutSeconds = 15;

        public ApiClient(ConfigService configService)
        {
            _configService = configService;
            _httpClient = new HttpClient
            {
                Timeout = TimeSpan.FromSeconds(TimeoutSeconds)
            };
            _httpClient.DefaultRequestHeaders.Add("Accept", "application/json");
            _httpClient.DefaultRequestHeaders.Add("User-Agent", "CasaCeja/1.0");
        }

        private string BaseUrl    => _configService.AppConfig.ServerUrl.TrimEnd('/');
        private string? UserToken => _configService.AppConfig.UserToken;

        private static StringContent CreateJsonContent(object body)
        {
            var json    = JsonSerializer.Serialize(body, _jsonOptions);
            var content = new StringContent(json);
            content.Headers.ContentType = new MediaTypeHeaderValue("application/json");
            return content;
        }

        private HttpRequestMessage CreateRequest(HttpMethod method, string endpoint)
        {
            var request = new HttpRequestMessage(method, $"{BaseUrl}{endpoint}");
            if (!string.IsNullOrEmpty(UserToken))
                request.Headers.Add("X-User-Token", UserToken);
            return request;
        }

        public async Task<bool> IsServerAvailableAsync()
        {
            try
            {
                var response = await _httpClient.GetAsync($"{BaseUrl}/api/v1/health");
                // Conectividad = servidor alcanzable, aunque responda 401/404.
                // Si hubo respuesta HTTP, hay conexión con el servidor.
                Console.WriteLine($"[ApiClient] Health check HTTP: {response.StatusCode}");
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ApiClient] Health check falló: {ex.Message}");
                return false;
            }
        }

        public async Task<long> GetServerTimeAsync()
        {
            try
            {
                var response = await _httpClient.GetAsync($"{BaseUrl}/api/v1/health");
                if (!response.IsSuccessStatusCode) return 0;
                var json   = await response.Content.ReadAsStringAsync();
                var health = JsonSerializer.Deserialize<HealthResponse>(json);
                return health?.ServerTime ?? 0;
            }
            catch { return 0; }
        }

        public async Task<ApiResponse<LoginResponse>?> LoginAsync(string username, string password)
        {
            var url  = $"{BaseUrl}/api/v1/auth/login";
            var body = new LoginRequest { Username = username, Password = password };
            try
            {
                var json    = JsonSerializer.Serialize(body, _jsonOptions);
                var request = new HttpRequestMessage(HttpMethod.Post, url);
                request.Content = CreateJsonContent(body);

                var response = await _httpClient.SendAsync(request);
                var respJson = await response.Content.ReadAsStringAsync();
                if (response.IsSuccessStatusCode)
                    return JsonSerializer.Deserialize<ApiResponse<LoginResponse>>(respJson, _jsonOptions);
                Console.WriteLine($"[ApiClient] Login fallido: {response.StatusCode} - {respJson}");
                return null;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ApiClient] Error en login: {ex.Message}");
                return null;
            }
        }

        public async Task<ApiResponse<T>?> GetAsync<T>(string endpoint, CancellationToken ct = default)
        {
            for (int attempt = 1; attempt <= MaxGetRetries; attempt++)
            {
                try
                {
                    var request  = CreateRequest(HttpMethod.Get, endpoint);
                    var response = await _httpClient.SendAsync(request, ct);
                    var json     = await response.Content.ReadAsStringAsync(ct);

                    if (response.IsSuccessStatusCode)
                        return JsonSerializer.Deserialize<ApiResponse<T>>(json, _jsonOptions);

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
            }
            return null;
        }

        public async Task<ApiResponse<T>?> PostAsync<T>(string endpoint, object body, CancellationToken ct = default)
        {
            for (int attempt = 1; attempt <= MaxRetries; attempt++)
            {
                try
                {
                    var request = CreateRequest(HttpMethod.Post, endpoint);
                    request.Content = CreateJsonContent(body);

                    var response = await _httpClient.SendAsync(request, ct);
                    var json     = await response.Content.ReadAsStringAsync(ct);

                    if (response.IsSuccessStatusCode || (int)response.StatusCode == 207)
                        return JsonSerializer.Deserialize<ApiResponse<T>>(json, _jsonOptions);

                    if ((int)response.StatusCode >= 400 && (int)response.StatusCode < 500)
                    {
                        Console.WriteLine($"[ApiClient] Error {response.StatusCode} en {endpoint}: {json}");
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

        public async Task<ApiResponse<T>?> PutAsync<T>(string endpoint, object body, CancellationToken ct = default)
        {
            try
            {
                var request = CreateRequest(HttpMethod.Put, endpoint);
                request.Content = CreateJsonContent(body);
                var response    = await _httpClient.SendAsync(request);
                var json        = await response.Content.ReadAsStringAsync();
                if (response.IsSuccessStatusCode)
                    return JsonSerializer.Deserialize<ApiResponse<T>>(json, _jsonOptions);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ApiClient] Error en PUT {endpoint}: {ex.Message}");
            }
            return null;
        }
    }
}