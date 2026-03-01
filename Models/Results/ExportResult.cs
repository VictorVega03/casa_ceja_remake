namespace CasaCejaRemake.Models.Results
{
    /// <summary>
    /// Resultado de una operación de exportación a Excel.
    /// </summary>
    public class ExportResult
    {
        public bool Success { get; set; }
        public string? FilePath { get; set; }
        public string? ErrorMessage { get; set; }

        public static ExportResult Ok(string filePath) =>
            new() { Success = true, FilePath = filePath };

        public static ExportResult Error(string message) =>
            new() { Success = false, ErrorMessage = message };
    }
}
