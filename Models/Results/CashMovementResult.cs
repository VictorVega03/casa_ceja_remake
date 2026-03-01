using CasaCejaRemake.Models;

namespace CasaCejaRemake.Models.Results
{
    /// <summary>
    /// Resultado de operaciones de movimientos de efectivo.
    /// </summary>
    public class CashMovementResult
    {
        public bool Success { get; set; }
        public string? ErrorMessage { get; set; }
        public CashMovement? Movement { get; set; }

        public static CashMovementResult Ok(CashMovement movement) =>
            new() { Success = true, Movement = movement };

        public static CashMovementResult Error(string message) =>
            new() { Success = false, ErrorMessage = message };
    }
}
