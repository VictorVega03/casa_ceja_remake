using System;
using System.Threading.Tasks;

namespace CasaCejaRemake.Services.Interfaces
{
    /// <summary>
    /// Contrato público del servicio de generación de folios.
    /// </summary>
    public interface IFolioService
    {
        Task<string> GenerarFolioVentaAsync(int sucursalId, int cajaId);
        Task<string> GenerarFolioApartadoAsync(int sucursalId, int cajaId);
        Task<string> GenerarFolioCreditoAsync(int sucursalId, int cajaId);
        Task<string> GenerarFolioPagoAsync(int sucursalId, int cajaId);
        Task<string> GenerarFolioCorteAsync(int sucursalId, int cajaId);
        (int sucursalId, int cajaId, DateTime fecha, char tipo, int secuencial) ParsearFolio(string folio);
    }
}
