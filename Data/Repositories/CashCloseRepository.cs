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
        /// Se filtra por la fecha de hoy para evitar cortes históricos sin cerrar.
        /// </summary>
        public async Task<Models.CashClose?> GetOpenByBranchAsync(int branchId)
        {
            var today = DateTime.Now.Date;
            var todayEnd = today.AddDays(1);

            // Traer cortes del día actual de la sucursal (filtro simple, compatible con sqlite-net-pcl)
            var todayCloses = await FindAsync(c =>
                c.BranchId == branchId &&
                c.OpeningDate >= today &&
                c.OpeningDate < todayEnd);

            // Determinar cuál está "abierto" (CloseDate no ha avanzado más allá de OpeningDate)
            return todayCloses
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
