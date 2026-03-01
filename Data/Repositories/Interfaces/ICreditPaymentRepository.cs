using CasaCejaRemake.Models;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace CasaCejaRemake.Data.Repositories.Interfaces
{
    /// <summary>
    /// Repositorio especializado para pagos de crédito (tabla credit_payments).
    /// Hereda CRUD genérico de IRepository<CreditPayment>.
    /// </summary>
    public interface ICreditPaymentRepository : IRepository<CreditPayment>
    {
        /// <summary>
        /// Obtiene todos los abonos registrados para un crédito específico.
        /// </summary>
        Task<List<CreditPayment>> GetByCreditIdAsync(int creditId);

        /// <summary>
        /// Obtiene los abonos de crédito realizados a partir de la fecha indicada (para cálculo de corte).
        /// </summary>
        Task<List<CreditPayment>> GetPaymentsSinceAsync(DateTime since);
    }
}
