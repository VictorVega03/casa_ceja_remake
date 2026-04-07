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
        ///
        /// Solo el corte MÁS RECIENTE puede estar abierto. Si el más reciente ya fue
        /// cerrado, se devuelve null aunque existan cortes históricos sin cerrar
        /// (datos huérfanos de pruebas o días anteriores). Esto permite que un corte
        /// abierto persista varios días sin forzar uno nuevo al cambiar la fecha.
        /// </summary>
        public async Task<Models.CashClose?> GetOpenByBranchAsync(int branchId)
        {
            var allForBranch = await FindAsync(c => c.BranchId == branchId);
            if (!allForBranch.Any()) return null;

            // Solo el corte más reciente puede considerarse abierto
            var mostRecent = allForBranch.OrderByDescending(c => c.OpeningDate).First();

            return mostRecent.CloseDate <= mostRecent.OpeningDate.AddSeconds(1) ? mostRecent : null;
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
