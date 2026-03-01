using System.Collections.Generic;
using System.Threading.Tasks;
using CasaCejaRemake.Models;
using CasaCejaRemake.Models.Results;

namespace CasaCejaRemake.Services.Interfaces
{
    /// <summary>
    /// Contrato público del servicio de impresión.
    /// </summary>
    public interface IPrintService
    {
        List<string> GetAvailablePrinters();
        Task<PrintResult> PrintAsync(string content);
        Task<bool> PrintThermalAsync(string text, string printerName);
        Task<bool> PrintLetterAsync(string text, string printerName, PosTerminalConfig config);
        Task<PrintResult> PrintSaleTicketAsync(string ticketText);
        Task<PrintResult> PrintCashCloseTicketAsync(string cashCloseText);
    }
}
