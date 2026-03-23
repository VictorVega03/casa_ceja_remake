using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;
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
        private static readonly JsonSerializerOptions JsonOptions = new()
        {
            PropertyNameCaseInsensitive = true
        };

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
        private string Token
        {
            get
            {
                var raw = _configService.AppConfig.UserToken ?? string.Empty;
                if (raw.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
                    raw = raw.Substring("Bearer ".Length).Trim();
                return raw.Trim();
            }
        }

        private void SetAuthHeader()
        {
            _httpClient.DefaultRequestHeaders.Remove("Authorization");
            _httpClient.DefaultRequestHeaders.Remove("X-User-Token");

            if (!string.IsNullOrEmpty(Token))
            {
                _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", Token);
                _httpClient.DefaultRequestHeaders.Add("X-User-Token", Token);
            }
        }

        private HttpRequestMessage CreateRequest(HttpMethod method, string url, HttpContent? content = null, bool rawAuthorization = false)
        {
            var request = new HttpRequestMessage(method, url);
            request.Headers.Accept.Clear();
            request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            request.Headers.TryAddWithoutValidation("X-Requested-With", "XMLHttpRequest");

            if (content != null)
                request.Content = content;

            if (!string.IsNullOrWhiteSpace(Token))
            {
                request.Headers.Remove("Authorization");
                request.Headers.Remove("X-User-Token");

                if (rawAuthorization)
                {
                    request.Headers.TryAddWithoutValidation("Authorization", Token);
                }
                else
                {
                    request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", Token);
                }

                request.Headers.TryAddWithoutValidation("X-User-Token", Token);
            }

            return request;
        }

        private static string? NormalizeToken(string? token)
        {
            if (string.IsNullOrWhiteSpace(token)) return null;
            token = token.Trim();
            if (token.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
                token = token.Substring("Bearer ".Length).Trim();
            return string.IsNullOrWhiteSpace(token) ? null : token;
        }

        private static bool LooksLikeHex64(string token)
        {
            if (token.Length != 64) return false;
            foreach (var ch in token)
            {
                var isHex = (ch >= '0' && ch <= '9')
                         || (ch >= 'a' && ch <= 'f')
                         || (ch >= 'A' && ch <= 'F');
                if (!isHex) return false;
            }
            return true;
        }

        private static int ScoreTokenCandidate(string key, string token)
        {
            var score = 0;
            var normalizedKey = key.ToLowerInvariant();

            if (normalizedKey.Contains("plaintexttoken") || normalizedKey.Contains("plain_text_token")) score += 100;
            else if (normalizedKey.Contains("access_token")) score += 80;
            else if (normalizedKey.EndsWith("token")) score += 40;

            if (normalizedKey.Contains("remember_token")) score -= 120;

            if (token.Contains("|")) score += 120; // típico sanctum plain text token
            if (token.StartsWith("eyJ", StringComparison.Ordinal)) score += 90; // JWT
            if (!LooksLikeHex64(token)) score += 30;
            else score -= 40;

            if (token.Length >= 20) score += 10;

            return score;
        }

        private static string? ExtractBestToken(string json, out string source)
        {
            source = "none";

            using var doc = JsonDocument.Parse(json);
            var candidates = new List<(string source, string token, int score)>();

            static void CollectObjectTokens(JsonElement obj, string prefix, List<(string source, string token, int score)> list)
            {
                foreach (var prop in obj.EnumerateObject())
                {
                    var propName = prop.Name;
                    var propNameLower = propName.ToLowerInvariant();
                    var currentPath = string.IsNullOrEmpty(prefix) ? propName : $"{prefix}.{propName}";

                    if (prop.Value.ValueKind == JsonValueKind.String)
                    {
                        if (propNameLower.Contains("token"))
                        {
                            var value = NormalizeToken(prop.Value.GetString());
                            if (!string.IsNullOrWhiteSpace(value))
                            {
                                var score = ScoreTokenCandidate(currentPath, value);
                                list.Add((currentPath, value, score));
                            }
                        }
                    }
                    else if (prop.Value.ValueKind == JsonValueKind.Object)
                    {
                        CollectObjectTokens(prop.Value, currentPath, list);
                    }
                }
            }

            if (doc.RootElement.ValueKind == JsonValueKind.Object)
            {
                CollectObjectTokens(doc.RootElement, string.Empty, candidates);
            }

            if (candidates.Count == 0)
                return null;

            var best = candidates[0];
            for (var i = 1; i < candidates.Count; i++)
            {
                if (candidates[i].score > best.score)
                    best = candidates[i];
            }

            source = best.source;
            return best.token;
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
            {
                var generic = JsonSerializer.Deserialize<ApiResponse<JsonElement>>(json, JsonOptions);

                var token = ExtractBestToken(json, out var tokenSource);
                if (!string.IsNullOrWhiteSpace(token))
                {
                    Console.WriteLine($"[ApiClient] Login token source={tokenSource} len={token.Length} hex64={LooksLikeHex64(token)}");

                    if (generic != null)
                    {
                        return new ApiResponse<LoginResponse>
                        {
                            Status = string.IsNullOrWhiteSpace(generic.Status) ? "success" : generic.Status,
                            Message = generic.Message,
                            Data = new LoginResponse { Token = token }
                        };
                    }

                    return new ApiResponse<LoginResponse>
                    {
                        Status = "success",
                        Message = "Login OK",
                        Data = new LoginResponse { Token = token }
                    };
                }

                Console.WriteLine("[ApiClient] Login OK, pero no se pudo extraer token del payload");
                return null;
            }

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
                    using var request = CreateRequest(HttpMethod.Get, url);
                    var response = await _httpClient.SendAsync(request, ct);
                    var json     = await response.Content.ReadAsStringAsync(ct);

                    if (response.IsSuccessStatusCode)
                        return JsonSerializer.Deserialize<ApiResponse<T>>(json, JsonOptions);

                    // No reintentar errores 4xx (son errores del cliente)
                    if ((int)response.StatusCode >= 400 && (int)response.StatusCode < 500)
                    {
                        if ((int)response.StatusCode == 401)
                        {
                            var authHeader = response.Headers.WwwAuthenticate?.ToString() ?? "(sin WWW-Authenticate)";
                            Console.WriteLine($"[ApiClient] Error 401 en {endpoint} | tokenLen={Token.Length} | authHeader={authHeader} | body={json}");
                        }
                        else
                        {
                            Console.WriteLine($"[ApiClient] Error {response.StatusCode} en {endpoint}");
                        }
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
                    using var request = CreateRequest(HttpMethod.Post, url, JsonContent.Create(body));
                    var response = await _httpClient.SendAsync(request, ct);
                    var json     = await response.Content.ReadAsStringAsync(ct);

                    if (response.IsSuccessStatusCode || (int)response.StatusCode == 207)
                        return JsonSerializer.Deserialize<ApiResponse<T>>(json, JsonOptions);

                    if ((int)response.StatusCode >= 400 && (int)response.StatusCode < 500)
                    {
                        if ((int)response.StatusCode == 401 && !string.IsNullOrWhiteSpace(Token))
                        {
                            var authHeader = response.Headers.WwwAuthenticate?.ToString() ?? "(sin WWW-Authenticate)";
                            Console.WriteLine($"[ApiClient] Error 401 en {endpoint} | tokenLen={Token.Length} | authHeader={authHeader} | body={json}");

                            // Fallback: algunos backends esperan Authorization sin esquema Bearer
                            using var fallbackRequest = CreateRequest(HttpMethod.Post, url, JsonContent.Create(body), rawAuthorization: true);
                            var fallbackResponse = await _httpClient.SendAsync(fallbackRequest, ct);
                            var fallbackJson = await fallbackResponse.Content.ReadAsStringAsync(ct);

                            if (fallbackResponse.IsSuccessStatusCode || (int)fallbackResponse.StatusCode == 207)
                            {
                                Console.WriteLine($"[ApiClient] Fallback Authorization (raw) exitoso en {endpoint}");
                                return JsonSerializer.Deserialize<ApiResponse<T>>(fallbackJson, JsonOptions);
                            }

                            Console.WriteLine($"[ApiClient] Fallback también falló {fallbackResponse.StatusCode} en {endpoint} | body={fallbackJson}");
                        }
                        else
                        {
                            Console.WriteLine($"[ApiClient] Error {response.StatusCode} en {endpoint}");
                        }
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
                using var request = CreateRequest(HttpMethod.Put, url, JsonContent.Create(body));
                var response = await _httpClient.SendAsync(request, ct);
                var json     = await response.Content.ReadAsStringAsync(ct);

                if (response.IsSuccessStatusCode)
                    return JsonSerializer.Deserialize<ApiResponse<T>>(json, JsonOptions);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ApiClient] Error en PUT {endpoint}: {ex.Message}");
            }

            return null;
        }
    }
}