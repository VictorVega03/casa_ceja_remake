using System.Collections.Generic;

namespace CasaCejaRemake.Models.Results
{
    /// <summary>
    /// Resultado de la validación de stock de un carrito.
    /// </summary>
    public class StockValidationResult
    {
        public bool IsValid { get; set; }
        public List<string> Errors { get; set; } = new();
    }
}
