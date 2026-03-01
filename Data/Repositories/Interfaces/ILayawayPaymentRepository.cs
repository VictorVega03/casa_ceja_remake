using CasaCejaRemake.Models;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace CasaCejaRemake.Data.Repositories.Interfaces
{
    /// <summary>
    /// Repositorio especializado para pagos de apartado (tabla layaway_payments).
    /// Hereda CRUD genérico de IRepository<LayawayPayment>.
    /// </summary>
    public interface ILayawayPaymentRepository : IRepository<LayawayPayment>
    {
        /// <summary>
        /// Obtiene todos los abonos registrados para un apartado específico.
        /// </summary>
        Task<List<LayawayPayment>> GetByLayawayIdAsync(int layawayId);

        /// <summary>
        /// Obtiene los abonos de apartado realizados a partir de la fecha indicada (para cálculo de corte).
        /// </summary>
        Task<List<LayawayPayment>> GetPaymentsSinceAsync(DateTime since);
    }
}
