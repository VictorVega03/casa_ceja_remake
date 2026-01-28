using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CasaCejaRemake.Data;
using CasaCejaRemake.Data.Repositories;
using CasaCejaRemake.Models;

namespace CasaCejaRemake.Services
{
    /// <summary>
    /// Resultado de operaciones de corte de caja.
    /// </summary>
    public class CashCloseResult
    {
        public bool Success { get; set; }
        public string? ErrorMessage { get; set; }
        public CashClose? CashClose { get; set; }

        public static CashCloseResult Ok(CashClose cashClose) =>
            new() { Success = true, CashClose = cashClose };

        public static CashCloseResult Error(string message) =>
            new() { Success = false, ErrorMessage = message };
    }

    /// <summary>
    /// Resultado de operaciones de movimientos de efectivo.
    /// </summary>
    public class CashMovementResult
    {
        public bool Success { get; set; }
        public string? ErrorMessage { get; set; }
        public CashMovement? Movement { get; set; }

        public static CashMovementResult Ok(CashMovement movement) =>
            new() { Success = true, Movement = movement };

        public static CashMovementResult Error(string message) =>
            new() { Success = false, ErrorMessage = message };
    }

    /// <summary>
    /// Totales calculados para el corte de caja.
    /// </summary>
    public class CashCloseTotals
    {
        public decimal TotalCash { get; set; }
        public decimal TotalDebit { get; set; }
        public decimal TotalCredit { get; set; }
        public decimal TotalTransfer { get; set; }
        public decimal TotalCheck { get; set; }
        public decimal LayawayCash { get; set; }
        public decimal LayawayTotal { get; set; }
        public decimal CreditPaymentsCash { get; set; }
        public decimal CreditPaymentsTotal { get; set; }
        public decimal TotalExpenses { get; set; }
        public decimal TotalIncome { get; set; }
        public int SalesCount { get; set; }
    }

    /// <summary>
    /// Servicio para gestión de apertura y cierre de caja.
    /// </summary>
    public class CashCloseService
    {
        private readonly DatabaseService _databaseService;
        private readonly BaseRepository<CashClose> _cashCloseRepository;
        private readonly BaseRepository<CashMovement> _movementRepository;
        private readonly BaseRepository<Sale> _saleRepository;
        private readonly BaseRepository<LayawayPayment> _layawayPaymentRepository;
        private readonly BaseRepository<CreditPayment> _creditPaymentRepository;

        public CashCloseService(DatabaseService databaseService)
        {
            _databaseService = databaseService;
            _cashCloseRepository = new BaseRepository<CashClose>(databaseService);
            _movementRepository = new BaseRepository<CashMovement>(databaseService);
            _saleRepository = new BaseRepository<Sale>(databaseService);
            _layawayPaymentRepository = new BaseRepository<LayawayPayment>(databaseService);
            _creditPaymentRepository = new BaseRepository<CreditPayment>(databaseService);
        }

        /// <summary>
        /// Abre una nueva caja con el fondo inicial especificado.
        /// </summary>
        public async Task<CashCloseResult> OpenCashAsync(decimal openingAmount, int userId, int branchId)
        {
            try
            {
                // Verificar que no haya caja abierta
                var existingOpen = await GetOpenCashAsync(branchId);
                if (existingOpen != null)
                {
                    return CashCloseResult.Error("Ya existe una caja abierta para esta sucursal");
                }

                // Generar folio
                var folio = await GenerateFolioAsync(branchId);

                var cashClose = new CashClose
                {
                    Folio = folio,
                    BranchId = branchId,
                    UserId = userId,
                    OpeningCash = openingAmount,
                    OpeningDate = DateTime.Now,
                    CloseDate = DateTime.Now, // Se actualizará al cerrar
                    CreatedAt = DateTime.Now,
                    UpdatedAt = DateTime.Now,
                    SyncStatus = 1
                };

                await _cashCloseRepository.AddAsync(cashClose);
                Console.WriteLine($"[CashCloseService] Caja abierta: Folio={folio}, Fondo=${openingAmount}");

                return CashCloseResult.Ok(cashClose);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[CashCloseService] Error al abrir caja: {ex.Message}");
                return CashCloseResult.Error($"Error al abrir caja: {ex.Message}");
            }
        }

        /// <summary>
        /// Obtiene la caja abierta para una sucursal.
        /// </summary>
        public async Task<CashClose?> GetOpenCashAsync(int branchId)
        {
            try
            {
                // Una caja está "abierta" si CloseDate es igual a OpeningDate (no se ha cerrado)
                // O si no tiene TotalCash > 0 y fue creada hoy
                var today = DateTime.Today;
                var cashCloses = await _cashCloseRepository.GetAllAsync();
                
                var openCash = cashCloses
                    .Where(c => c.BranchId == branchId)
                    .Where(c => c.OpeningDate.Date == today)
                    .Where(c => c.CloseDate <= c.OpeningDate.AddSeconds(1)) // No cerrada aún
                    .OrderByDescending(c => c.OpeningDate)
                    .FirstOrDefault();

                return openCash;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[CashCloseService] Error al verificar caja abierta: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// Calcula los totales para el corte de caja.
        /// </summary>
        public async Task<CashCloseTotals> CalculateTotalsAsync(int cashCloseId, DateTime openingDate)
        {
            var totals = new CashCloseTotals();

            try
            {
                // Obtener ventas desde la apertura
                var allSales = await _saleRepository.GetAllAsync();
                var salesSinceOpen = allSales
                    .Where(s => s.SaleDate >= openingDate)
                    .ToList();

                totals.SalesCount = salesSinceOpen.Count;

                // Totales por método de pago
                totals.TotalCash = salesSinceOpen
                    .Where(s => s.PaymentMethod == "cash" || s.PaymentMethod == "Efectivo")
                    .Sum(s => s.Total);

                totals.TotalDebit = salesSinceOpen
                    .Where(s => s.PaymentMethod == "debit" || s.PaymentMethod == "Tarjeta Débito")
                    .Sum(s => s.Total);

                totals.TotalCredit = salesSinceOpen
                    .Where(s => s.PaymentMethod == "credit" || s.PaymentMethod == "Tarjeta Crédito")
                    .Sum(s => s.Total);

                totals.TotalTransfer = salesSinceOpen
                    .Where(s => s.PaymentMethod == "transfer" || s.PaymentMethod == "Transferencia")
                    .Sum(s => s.Total);

                totals.TotalCheck = salesSinceOpen
                    .Where(s => s.PaymentMethod == "check" || s.PaymentMethod == "Cheque")
                    .Sum(s => s.Total);

                // Abonos de apartados
                var allLayawayPayments = await _layawayPaymentRepository.GetAllAsync();
                var layawayPaymentsSinceOpen = allLayawayPayments
                    .Where(p => p.PaymentDate >= openingDate)
                    .ToList();

                totals.LayawayTotal = layawayPaymentsSinceOpen.Sum(p => p.AmountPaid);
                totals.LayawayCash = layawayPaymentsSinceOpen
                    .Where(p => p.PaymentMethod == "cash" || p.PaymentMethod == "Efectivo")
                    .Sum(p => p.AmountPaid);

                // Abonos de créditos
                var allCreditPayments = await _creditPaymentRepository.GetAllAsync();
                var creditPaymentsSinceOpen = allCreditPayments
                    .Where(p => p.PaymentDate >= openingDate)
                    .ToList();

                totals.CreditPaymentsTotal = creditPaymentsSinceOpen.Sum(p => p.AmountPaid);
                totals.CreditPaymentsCash = creditPaymentsSinceOpen
                    .Where(p => p.PaymentMethod == "cash" || p.PaymentMethod == "Efectivo")
                    .Sum(p => p.AmountPaid);

                // Movimientos (gastos e ingresos)
                var movements = await GetMovementsAsync(cashCloseId);
                totals.TotalExpenses = movements.Where(m => m.IsExpense).Sum(m => m.Amount);
                totals.TotalIncome = movements.Where(m => m.IsIncome).Sum(m => m.Amount);

                Console.WriteLine($"[CashCloseService] Totales calculados: Ventas={totals.SalesCount}, Efectivo=${totals.TotalCash}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[CashCloseService] Error calculando totales: {ex.Message}");
            }

            return totals;
        }

        /// <summary>
        /// Cierra la caja con los datos finales.
        /// </summary>
        public async Task<CashCloseResult> CloseCashAsync(CashClose cashClose, decimal declaredAmount)
        {
            try
            {
                // Calcular totales
                var totals = await CalculateTotalsAsync(cashClose.Id, cashClose.OpeningDate);

                // Actualizar campos
                cashClose.TotalCash = totals.TotalCash;
                cashClose.TotalDebitCard = totals.TotalDebit;
                cashClose.TotalCreditCard = totals.TotalCredit;
                cashClose.TotalTransfers = totals.TotalTransfer;
                cashClose.TotalChecks = totals.TotalCheck;
                cashClose.LayawayCash = totals.LayawayCash;
                cashClose.CreditCash = totals.CreditPaymentsCash;
                cashClose.TotalSales = totals.TotalCash + totals.TotalDebit + totals.TotalCredit + 
                                       totals.TotalTransfer + totals.TotalCheck;

                // Calcular efectivo esperado
                // Fondo + Ventas en efectivo + Abonos en efectivo + Ingresos - Gastos
                decimal expectedCash = cashClose.OpeningCash + totals.TotalCash + 
                                       totals.LayawayCash + totals.CreditPaymentsCash +
                                       totals.TotalIncome - totals.TotalExpenses;

                cashClose.ExpectedCash = expectedCash;
                cashClose.Surplus = declaredAmount - expectedCash;
                cashClose.CloseDate = DateTime.Now;
                cashClose.UpdatedAt = DateTime.Now;

                await _cashCloseRepository.UpdateAsync(cashClose);

                Console.WriteLine($"[CashCloseService] Caja cerrada: Folio={cashClose.Folio}, " +
                                  $"Esperado=${expectedCash}, Declarado=${declaredAmount}, Diferencia=${cashClose.Surplus}");

                return CashCloseResult.Ok(cashClose);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[CashCloseService] Error al cerrar caja: {ex.Message}");
                return CashCloseResult.Error($"Error al cerrar caja: {ex.Message}");
            }
        }

        /// <summary>
        /// Agrega un movimiento (gasto o ingreso) al turno actual.
        /// </summary>
        public async Task<CashMovementResult> AddMovementAsync(int cashCloseId, string type, string concept, decimal amount, int userId)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(concept))
                {
                    return CashMovementResult.Error("El concepto es requerido");
                }

                if (amount <= 0)
                {
                    return CashMovementResult.Error("El monto debe ser mayor a 0");
                }

                var movement = new CashMovement
                {
                    CashCloseId = cashCloseId,
                    Type = type,
                    Concept = concept,
                    Amount = amount,
                    UserId = userId,
                    CreatedAt = DateTime.Now
                };

                await _movementRepository.AddAsync(movement);
                Console.WriteLine($"[CashCloseService] Movimiento agregado: {type} - {concept} - ${amount}");

                return CashMovementResult.Ok(movement);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[CashCloseService] Error al agregar movimiento: {ex.Message}");
                return CashMovementResult.Error($"Error al agregar movimiento: {ex.Message}");
            }
        }

        /// <summary>
        /// Obtiene los movimientos de un corte de caja.
        /// </summary>
        public async Task<List<CashMovement>> GetMovementsAsync(int cashCloseId)
        {
            try
            {
                var all = await _movementRepository.GetAllAsync();
                return all.Where(m => m.CashCloseId == cashCloseId).ToList();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[CashCloseService] Error al obtener movimientos: {ex.Message}");
                return new List<CashMovement>();
            }
        }

        /// <summary>
        /// Obtiene el historial de cortes de una sucursal.
        /// </summary>
        public async Task<List<CashClose>> GetHistoryAsync(int branchId, int limit = 30)
        {
            try
            {
                var all = await _cashCloseRepository.GetAllAsync();
                return all
                    .Where(c => c.BranchId == branchId)
                    .OrderByDescending(c => c.CloseDate)
                    .Take(limit)
                    .ToList();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[CashCloseService] Error al obtener historial: {ex.Message}");
                return new List<CashClose>();
            }
        }

        /// <summary>
        /// Genera un folio único para el corte.
        /// </summary>
        private async Task<string> GenerateFolioAsync(int branchId)
        {
            try
            {
                var today = DateTime.Today;
                var all = await _cashCloseRepository.GetAllAsync();
                var todayCount = all
                    .Where(c => c.BranchId == branchId)
                    .Where(c => c.CreatedAt.Date == today)
                    .Count();

                // Formato: CC-YYYYMMDD-XXX (CC = Cash Close)
                return $"CC-{today:yyyyMMdd}-{(todayCount + 1):D3}";
            }
            catch
            {
                return $"CC-{DateTime.Now:yyyyMMddHHmmss}";
            }
        }
    }
}
