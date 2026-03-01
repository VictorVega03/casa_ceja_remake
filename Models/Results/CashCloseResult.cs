using CasaCejaRemake.Models;

namespace CasaCejaRemake.Models.Results
{
    /// <summary>
    /// Resultado de operaciones de corte de caja.
    /// </summary>
    public class CashCloseResult
    {
        public bool Success { get; set; }
        public string? ErrorMessage { get; set; }
        public CashClose? CashClose { get; set; }

        public static CashCloseResult Ok(CashClose cashClose) =>
            new() { Success = true, CashClose = cashClose };

        public static CashCloseResult Error(string message) =>
            new() { Success = false, ErrorMessage = message };
    }
}
