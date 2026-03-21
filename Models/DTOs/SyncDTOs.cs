using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace CasaCejaRemake.Models.DTOs
{
    /// Respuesta genérica del servidor para cualquier endpoint.
    public class ApiResponse<T>
    {
        [JsonPropertyName("status")]
        public string Status { get; set; } = string.Empty;

        [JsonPropertyName("message")]
        public string Message { get; set; } = string.Empty;

        [JsonPropertyName("data")]
        public T? Data { get; set; }

        public bool IsSuccess => Status == "success";
    }

    /// Respuesta del health check del servidor.
    public class HealthResponse
    {
        [JsonPropertyName("status")]
        public string Status { get; set; } = string.Empty;

        [JsonPropertyName("server_time")]
        public long ServerTime { get; set; }

        [JsonPropertyName("version")]
        public string Version { get; set; } = string.Empty;
    }

    /// Respuesta genérica de un Pull con paginación.
    public class PullResponse<T>
    {
        [JsonPropertyName("data")]
        public List<T> Data { get; set; } = new();

        [JsonPropertyName("count")]
        public int Count { get; set; }

        [JsonPropertyName("total")]
        public int Total { get; set; }

        [JsonPropertyName("current_page")]
        public int CurrentPage { get; set; }

        [JsonPropertyName("last_page")]
        public int LastPage { get; set; }

        public bool HasMorePages => CurrentPage < LastPage;
    }

    /// Respuesta de un Push con registros aceptados y rechazados.
    public class PushResponse
    {
        [JsonPropertyName("accepted")]
        public List<string> Accepted { get; set; } = new();

        [JsonPropertyName("rejected")]
        public List<RejectedRecord> Rejected { get; set; } = new();
    }

    /// Registro rechazado por el servidor en un Push.
    public class RejectedRecord
    {
        [JsonPropertyName("folio")]
        public string Folio { get; set; } = string.Empty;

        [JsonPropertyName("reason")]
        public string Reason { get; set; } = string.Empty;
    }

    /// Resultado de una operación de sincronización.
    public class SyncResult
    {
        public bool Success { get; set; }
        public string Entity { get; set; } = string.Empty;
        public int RecordsPulled { get; set; }
        public int RecordsPushed { get; set; }
        public int RecordsRejected { get; set; }
        public string? ErrorMessage { get; set; }

        public static SyncResult Ok(string entity, int pulled = 0, int pushed = 0) => new()
        {
            Success = true,
            Entity = entity,
            RecordsPulled = pulled,
            RecordsPushed = pushed,
        };

        public static SyncResult Fail(string entity, string error) => new()
        {
            Success = false,
            Entity = entity,
            ErrorMessage = error,
        };
    }
}