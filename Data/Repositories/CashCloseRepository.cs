using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace CasaCejaRemake.Data.Repositories
{
    public class CashCloseRepository : BaseRepository<Models.CashClose>
    {
        public CashCloseRepository(DatabaseService databaseService)
            : base(databaseService) { }

        /// <summary>
        /// Obtiene el corte de caja "abierto" para una sucursal.
        /// Un corte está abierto cuando CloseDate <= OpeningDate.AddSeconds(1)
        /// (convención usada en CashCloseService: se crea con CloseDate == OpeningDate).
        /// No se filtra por fecha para permitir que un corte abierto persista entre días
        /// sin forzar la creación de uno nuevo.
        /// </summary>
        public async Task<Models.CashClose?> GetOpenByBranchAsync(int branchId)
        {
            var allForBranch = await FindAsync(c => c.BranchId == branchId);

            // Determinar cuál está "abierto" (CloseDate no ha avanzado más allá de OpeningDate)
            return allForBranch
                .OrderByDescending(c => c.OpeningDate)
                .FirstOrDefault(c => c.CloseDate <= c.OpeningDate.AddSeconds(1));
        }

        /// <summary>
        /// Obtiene el historial de cortes CERRADOS de una sucursal.
        /// Un corte está cerrado cuando CloseDate > OpeningDate.AddSeconds(1).
        /// Devuelve los últimos N registros ordenados por fecha de cierre descendente.
        /// </summary>
        public async Task<List<Models.CashClose>> GetHistoryByBranchAsync(int branchId, int limit)
        {
            var allForBranch = await FindAsync(c => c.BranchId == branchId);

            return allForBranch
                .Where(c => c.CloseDate > c.OpeningDate.AddSeconds(1))
                .OrderByDescending(c => c.CloseDate)
                .Take(limit)
                .ToList();
        }
    }
}
