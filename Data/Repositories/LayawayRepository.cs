using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace CasaCejaRemake.Data.Repositories
{
    public class LayawayRepository : BaseRepository<Models.Layaway>
    {
        public LayawayRepository(DatabaseService databaseService)
            : base(databaseService) { }

        /// <summary>
        /// Obtiene apartados pendientes de un cliente (Status 1=Pendiente, 2=Vencido — ambos < 3).
        /// </summary>
        public async Task<List<Models.Layaway>> GetPendingByCustomerAsync(int customerId)
        {
            return await FindAsync(l => l.CustomerId == customerId && l.Status < 3);
        }

        /// <summary>
        /// Obtiene apartados pendientes de una sucursal (Status < 3 = Pendiente o Vencido).
        /// </summary>
        public async Task<List<Models.Layaway>> GetPendingByBranchAsync(int branchId)
        {
            return await FindAsync(l => l.BranchId == branchId && l.Status < 3);
        }

        /// <summary>
        /// Búsqueda combinada de apartados por cliente, estado y sucursal.
        /// Los filtros opcionales se aplican en DB cuando tienen valor.
        /// </summary>
        public async Task<List<Models.Layaway>> SearchAsync(int? customerId, int? status, int branchId)
        {
            if (customerId.HasValue && customerId > 0 && status.HasValue)
            {
                return await FindAsync(l =>
                    l.BranchId == branchId &&
                    l.CustomerId == customerId &&
                    l.Status == status);
            }
            else if (customerId.HasValue && customerId > 0)
            {
                return await FindAsync(l =>
                    l.BranchId == branchId &&
                    l.CustomerId == customerId);
            }
            else if (status.HasValue)
            {
                return await FindAsync(l =>
                    l.BranchId == branchId &&
                    l.Status == status);
            }
            else
            {
                return await FindAsync(l => l.BranchId == branchId);
            }
        }

        /// <summary>
        /// Busca un apartado por su folio único.
        /// </summary>
        public async Task<Models.Layaway?> GetByFolioAsync(string folio)
        {
            if (string.IsNullOrWhiteSpace(folio))
                return null;

            return await FirstOrDefaultAsync(l => l.Folio == folio);
        }

        /// <summary>
        /// Obtiene todos los apartados creados a partir de una fecha dada.
        /// </summary>
        public async Task<List<Models.Layaway>> GetCreatedSinceAsync(DateTime since)
        {
            return await FindAsync(l => l.CreatedAt >= since);
        }
    }
}
