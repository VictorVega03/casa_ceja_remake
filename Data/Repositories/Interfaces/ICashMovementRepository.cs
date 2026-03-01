using CasaCejaRemake.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace CasaCejaRemake.Data.Repositories.Interfaces
{
    /// <summary>
    /// Repositorio especializado para movimientos de caja (tabla cash_movements).
    /// Hereda CRUD genérico de IRepository<CashMovement>.
    /// </summary>
    public interface ICashMovementRepository : IRepository<CashMovement>
    {
        /// <summary>
        /// Obtiene todos los movimientos de efectivo asociados a un corte de caja.
        /// </summary>
        Task<List<CashMovement>> GetByCashCloseIdAsync(int cashCloseId);
    }
}
