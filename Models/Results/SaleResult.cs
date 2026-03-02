using System.Collections.Generic;
using CasaCejaRemake.Models;
using CasaCejaRemake.Services;

namespace CasaCejaRemake.Models.Results
{
    /// <summary>
    /// Resultado de una operación de venta.
    /// </summary>
    public class SaleResult
    {
        public bool Success { get; set; }
        public string? ErrorMessage { get; set; }
        public Sale? Sale { get; set; }
        public TicketData? Ticket { get; set; }
        public string? TicketText { get; set; }

        public static SaleResult Ok(Sale sale, TicketData ticket, string ticketText)
        {
            return new SaleResult
            {
                Success = true,
                Sale = sale,
                Ticket = ticket,
                TicketText = ticketText
            };
        }

        public static SaleResult Error(string message)
        {
            return new SaleResult
            {
                Success = false,
                ErrorMessage = message
            };
        }
    }

    /// <summary>
    /// Resultado de la validación de stock.
    /// </summary>
    public class StockValidationResult
    {
        public bool IsValid { get; set; }
        public List<string> Errors { get; set; } = new();
    }
}
