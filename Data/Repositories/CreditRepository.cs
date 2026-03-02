using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace CasaCejaRemake.Data.Repositories
{
    public class CreditRepository : BaseRepository<Models.Credit>
    {
        public CreditRepository(DatabaseService databaseService)
            : base(databaseService) { }

        /// <summary>
        /// Obtiene créditos pendientes de un cliente (Status 1=Pendiente, 2=Vencido — ambos < 3).
        /// </summary>
        public async Task<List<Models.Credit>> GetPendingByCustomerAsync(int customerId)
        {
            return await FindAsync(c => c.CustomerId == customerId && c.Status < 3);
        }

        /// <summary>
        /// Obtiene créditos pendientes de una sucursal (Status < 3 = Pendiente o Vencido).
        /// </summary>
        public async Task<List<Models.Credit>> GetPendingByBranchAsync(int branchId)
        {
            return await FindAsync(c => c.BranchId == branchId && c.Status < 3);
        }

        /// <summary>
        /// Búsqueda combinada de créditos por cliente, estado y sucursal.
        /// Los filtros opcionales se aplican en DB cuando tienen valor.
        /// </summary>
        public async Task<List<Models.Credit>> SearchAsync(int? customerId, int? status, int branchId)
        {
            if (customerId.HasValue && customerId > 0 && status.HasValue)
            {
                return await FindAsync(c =>
                    c.BranchId == branchId &&
                    c.CustomerId == customerId &&
                    c.Status == status);
            }
            else if (customerId.HasValue && customerId > 0)
            {
                return await FindAsync(c =>
                    c.BranchId == branchId &&
                    c.CustomerId == customerId);
            }
            else if (status.HasValue)
            {
                return await FindAsync(c =>
                    c.BranchId == branchId &&
                    c.Status == status);
            }
            else
            {
                return await FindAsync(c => c.BranchId == branchId);
            }
        }

        /// <summary>
        /// Busca un crédito por su folio único.
        /// </summary>
        public async Task<Models.Credit?> GetByFolioAsync(string folio)
        {
            if (string.IsNullOrWhiteSpace(folio))
                return null;

            return await FirstOrDefaultAsync(c => c.Folio == folio);
        }

        /// <summary>
        /// Obtiene todos los créditos creados a partir de una fecha dada.
        /// </summary>
        public async Task<List<Models.Credit>> GetCreatedSinceAsync(DateTime since)
        {
            return await FindAsync(c => c.CreatedAt >= since);
        }
    }
}
