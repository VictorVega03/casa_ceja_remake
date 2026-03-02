using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CasaCejaRemake.Data.Repositories;
using CasaCejaRemake.Models;
using CasaCejaRemake.Models.Results;

namespace CasaCejaRemake.Services
{
    /// <summary>
    /// Servicio para gestión de apertura y cierre de caja.
    /// Implementa las reglas de negocio documentadas en reglas_corte.md
    /// </summary>
    public class CashCloseService
    {
        private readonly CashCloseRepository _cashCloseRepository;
        private readonly BaseRepository<CashMovement> _movementRepository;
        private readonly SaleRepository _saleRepository;
        private readonly CreditRepository _creditRepository;
        private readonly LayawayRepository _layawayRepository;
        private readonly BaseRepository<LayawayPayment> _layawayPaymentRepository;
        private readonly BaseRepository<CreditPayment> _creditPaymentRepository;
        private readonly FolioService _folioService;
        private readonly ConfigService _configService;

        public CashCloseService(
            CashCloseRepository cashCloseRepository,
            BaseRepository<CashMovement> movementRepository,
            SaleRepository saleRepository,
            CreditRepository creditRepository,
            LayawayRepository layawayRepository,
            BaseRepository<LayawayPayment> layawayPaymentRepository,
            BaseRepository<CreditPayment> creditPaymentRepository,
            FolioService folioService,
            ConfigService configService)
        {
            _cashCloseRepository = cashCloseRepository;
            _movementRepository = movementRepository;
            _saleRepository = saleRepository;
            _creditRepository = creditRepository;
            _layawayRepository = layawayRepository;
            _layawayPaymentRepository = layawayPaymentRepository;
            _creditPaymentRepository = creditPaymentRepository;
            _folioService = folioService;
            _configService = configService;
        }

        /// <summary>Abre una nueva caja con el fondo inicial especificado.</summary>
        public async Task<CashCloseResult> OpenCashAsync(decimal openingAmount, int userId, int branchId)
        {
            try
            {
                var existingOpen = await GetOpenCashAsync(branchId);
                if (existingOpen != null)
                {
                    return CashCloseResult.Error("Ya existe una caja abierta para esta sucursal");
                }

                var folio = await GenerateFolioAsync(branchId);

                var cashClose = new CashClose
                {
                    Folio = folio,
                    BranchId = branchId,
                    UserId = userId,
                    OpeningCash = openingAmount,
                    OpeningDate = DateTime.Now,
                    CloseDate = DateTime.Now,
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

        /// <summary>Obtiene la caja abierta para una sucursal.</summary>
        public async Task<CashClose?> GetOpenCashAsync(int branchId)
        {
            try
            {
                return await _cashCloseRepository.GetOpenByBranchAsync(branchId);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[CashCloseService] Error al verificar caja abierta: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// Calcula los totales para el corte de caja según las reglas de negocio.
        ///
        /// REGLAS CLAVE (de reglas_corte.md):
        /// 1. Para ventas se usa sale.Total (NO AmountPaid). El cambio está implícito.
        /// 2. Los créditos/apartados CREADOS suman al Total del Corte (productividad).
        /// 3. Solo el efectivo de abonos/enganches suma al Efectivo Esperado.
        /// 4. El cambio NO se resta (ya está contemplado al usar Total en vez de AmountPaid).
        /// </summary>
        public async Task<CashCloseTotals> CalculateTotalsAsync(int cashCloseId, DateTime openingDate)
        {
            var totals = new CashCloseTotals();

            try
            {
                Console.WriteLine($"[CashCloseService] CalculateTotalsAsync - openingDate={openingDate}");

                // ==================== 1. VENTAS DIRECTAS ====================
                var salesSinceOpen = await _saleRepository.GetByBranchSinceDateAsync(0, openingDate);
                // branchId=0 → filter all branches; we only want since openingDate which is already done

                totals.SalesCount = salesSinceOpen.Count;
                Console.WriteLine($"[CashCloseService] Ventas directas: {salesSinceOpen.Count}");

                foreach (var sale in salesSinceOpen)
                {
                    if (sale.PaymentMethod.StartsWith("{"))
                    {
                        ProcessMixedPaymentSale(sale, totals);
                    }
                    else
                    {
                        switch (sale.PaymentMethod)
                        {
                            case "Efectivo":
                                totals.TotalCash += sale.Total;
                                break;
                            case "TarjetaDebito":
                                totals.TotalDebitCard += sale.Total;
                                break;
                            case "TarjetaCredito":
                                totals.TotalCreditCard += sale.Total;
                                break;
                            case "Transferencia":
                                totals.TotalTransfers += sale.Total;
                                break;
                            case "Cheque":
                                totals.TotalChecks += sale.Total;
                                break;
                        }
                    }
                }

                totals.TotalSales = totals.TotalCash + totals.TotalDebitCard +
                                    totals.TotalCreditCard + totals.TotalTransfers + totals.TotalChecks;

                // ==================== 2. CRÉDITOS CREADOS ====================
                var creditsCreated = await _creditRepository.GetCreatedSinceAsync(openingDate);
                totals.CreditCount = creditsCreated.Count;
                totals.CreditTotalCreated = creditsCreated.Sum(c => c.Total);
                Console.WriteLine($"[CashCloseService] Créditos creados: {creditsCreated.Count}, Total: ${totals.CreditTotalCreated}");

                var creditPaymentsSinceOpen = await _creditPaymentRepository.FindAsync(p => p.PaymentDate >= openingDate);
                foreach (var payment in creditPaymentsSinceOpen)
                {
                    decimal cashAmount = ExtractCashFromPaymentMethod(payment.PaymentMethod, payment.AmountPaid);
                    totals.CreditCash += cashAmount;
                }
                Console.WriteLine($"[CashCloseService] Efectivo de créditos (abonos): ${totals.CreditCash}");

                // ==================== 3. APARTADOS CREADOS ====================
                var layawaysCreated = await _layawayRepository.GetCreatedSinceAsync(openingDate);
                totals.LayawayCount = layawaysCreated.Count;
                totals.LayawayTotalCreated = layawaysCreated.Sum(l => l.Total);
                Console.WriteLine($"[CashCloseService] Apartados creados: {layawaysCreated.Count}, Total: ${totals.LayawayTotalCreated}");

                var layawayPaymentsSinceOpen = await _layawayPaymentRepository.FindAsync(p => p.PaymentDate >= openingDate);
                foreach (var payment in layawayPaymentsSinceOpen)
                {
                    decimal cashAmount = ExtractCashFromPaymentMethod(payment.PaymentMethod, payment.AmountPaid);
                    totals.LayawayCash += cashAmount;
                }
                Console.WriteLine($"[CashCloseService] Efectivo de apartados (abonos): ${totals.LayawayCash}");

                // ==================== 4. MOVIMIENTOS DE CAJA ====================
                var movements = await GetMovementsAsync(cashCloseId);
                totals.TotalExpenses = movements.Where(m => m.IsExpense).Sum(m => m.Amount);
                totals.TotalIncome = movements.Where(m => m.IsIncome).Sum(m => m.Amount);

                // ==================== RESUMEN ====================
                Console.WriteLine($"[CashCloseService] === RESUMEN DE CÁLCULOS ===");
                Console.WriteLine($"  VENTAS DIRECTAS:");
                Console.WriteLine($"    Efectivo: ${totals.TotalCash}");
                Console.WriteLine($"    Débito: ${totals.TotalDebitCard}");
                Console.WriteLine($"    Crédito: ${totals.TotalCreditCard}");
                Console.WriteLine($"    Transferencia: ${totals.TotalTransfers}");
                Console.WriteLine($"    Total Ventas Directas: ${totals.TotalSales}");
                Console.WriteLine($"  CRÉDITOS:");
                Console.WriteLine($"    Creados (Total): ${totals.CreditTotalCreated} ({totals.CreditCount})");
                Console.WriteLine($"    Efectivo Abonos: ${totals.CreditCash}");
                Console.WriteLine($"  APARTADOS:");
                Console.WriteLine($"    Creados (Total): ${totals.LayawayTotalCreated} ({totals.LayawayCount})");
                Console.WriteLine($"    Efectivo Abonos: ${totals.LayawayCash}");
                Console.WriteLine($"  MOVIMIENTOS:");
                Console.WriteLine($"    Gastos: ${totals.TotalExpenses}");
                Console.WriteLine($"    Ingresos: ${totals.TotalIncome}");
                Console.WriteLine($"  ---");
                Console.WriteLine($"  TOTAL DEL CORTE (Productividad): ${totals.TotalDelCorte}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[CashCloseService] Error calculando totales: {ex.Message}");
            }

            return totals;
        }

        /// <summary>
        /// Procesa una venta con pago mixto (JSON) distribuyendo el Total proporcionalmente.
        /// </summary>
        private void ProcessMixedPaymentSale(Sale sale, CashCloseTotals totals)
        {
            try
            {
                var payments = System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, decimal>>(sale.PaymentMethod);
                if (payments == null || payments.Count == 0) return;

                decimal totalPaidInJson = payments.Values.Sum();
                if (totalPaidInJson <= 0) return;

                foreach (var kvp in payments)
                {
                    decimal proportion = kvp.Value / totalPaidInJson;
                    decimal amountForMethod = sale.Total * proportion;

                    switch (kvp.Key.ToLower())
                    {
                        case "efectivo":
                            totals.TotalCash += amountForMethod;
                            break;
                        case "tarjeta_debito":
                            totals.TotalDebitCard += amountForMethod;
                            break;
                        case "tarjeta_credito":
                            totals.TotalCreditCard += amountForMethod;
                            break;
                        case "transferencia":
                            totals.TotalTransfers += amountForMethod;
                            break;
                        case "cheque":
                            totals.TotalChecks += amountForMethod;
                            break;
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[CashCloseService] Error procesando pago mixto: {ex.Message}");
            }
        }

        /// <summary>
        /// Extrae el monto en efectivo de un PaymentMethod (puede ser JSON o string simple)
        /// </summary>
        private decimal ExtractCashFromPaymentMethod(string paymentMethod, decimal totalAmount)
        {
            if (string.IsNullOrEmpty(paymentMethod))
                return 0;

            if (paymentMethod == "Efectivo")
                return totalAmount;

            if (paymentMethod.StartsWith("{"))
            {
                try
                {
                    var payments = System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, decimal>>(paymentMethod);
                    if (payments != null && payments.TryGetValue("efectivo", out decimal cash))
                    {
                        return cash;
                    }
                }
                catch
                {
                    // Ignorar errores de parsing
                }
            }

            return 0;
        }

        /// <summary>
        /// Cierra la caja con los datos finales.
        ///
        /// FÓRMULA (de reglas_corte.md):
        /// EFECTIVO ESPERADO = Fondo + Efectivo Ventas + Efectivo Abonos Créditos
        ///                   + Efectivo Abonos Apartados + Ingresos - Gastos
        ///
        /// NOTA: El cambio NO se resta porque ya está implícito al usar sale.Total
        /// en lugar de sale.AmountPaid.
        /// </summary>
        public async Task<CashCloseResult> CloseCashAsync(CashClose cashClose, decimal declaredAmount)
        {
            try
            {
                Console.WriteLine($"[CashCloseService] CloseCashAsync iniciado - Id={cashClose.Id}, Folio={cashClose.Folio}");

                var totals = await CalculateTotalsAsync(cashClose.Id, cashClose.OpeningDate);

                Console.WriteLine($"[CashCloseService] Totales calculados:");
                Console.WriteLine($"  - Ventas efectivo: ${totals.TotalCash}");
                Console.WriteLine($"  - Créditos creados: ${totals.CreditTotalCreated}");
                Console.WriteLine($"  - Apartados creados: ${totals.LayawayTotalCreated}");
                Console.WriteLine($"  - Efectivo abonos créditos: ${totals.CreditCash}");
                Console.WriteLine($"  - Efectivo abonos apartados: ${totals.LayawayCash}");

                cashClose.TotalCash = Math.Round(totals.TotalCash, 2);
                cashClose.TotalDebitCard = Math.Round(totals.TotalDebitCard, 2);
                cashClose.TotalCreditCard = Math.Round(totals.TotalCreditCard, 2);
                cashClose.TotalTransfers = Math.Round(totals.TotalTransfers, 2);
                cashClose.TotalChecks = Math.Round(totals.TotalChecks, 2);

                cashClose.CreditTotalCreated = Math.Round(totals.CreditTotalCreated, 2);
                cashClose.LayawayTotalCreated = Math.Round(totals.LayawayTotalCreated, 2);

                cashClose.CreditCash = Math.Round(totals.CreditCash, 2);
                cashClose.LayawayCash = Math.Round(totals.LayawayCash, 2);

                cashClose.TotalSales = Math.Round(totals.TotalSales, 2);

                var movements = await GetMovementsAsync(cashClose.Id);
                var expenses = movements.Where(m => m.IsExpense).Select(m => new { description = m.Concept, amount = m.Amount }).ToList();
                var income = movements.Where(m => !m.IsExpense).Select(m => new { description = m.Concept, amount = m.Amount }).ToList();

                cashClose.Expenses = System.Text.Json.JsonSerializer.Serialize(expenses);
                cashClose.Income = System.Text.Json.JsonSerializer.Serialize(income);

                decimal expectedCash = cashClose.OpeningCash
                                     + totals.TotalCash
                                     + totals.CreditCash
                                     + totals.LayawayCash
                                     + totals.TotalIncome
                                     - totals.TotalExpenses;

                cashClose.ExpectedCash = Math.Round(expectedCash, 2);
                cashClose.Surplus = Math.Round(declaredAmount - expectedCash, 2);
                cashClose.CloseDate = DateTime.Now;
                cashClose.UpdatedAt = DateTime.Now;

                await _cashCloseRepository.UpdateAsync(cashClose);

                // ==================== ASIGNAR FOLIO DE CORTE A TRANSACCIONES ====================
                var salesSinceOpen = await _saleRepository.FindAsync(s => s.SaleDate >= cashClose.OpeningDate);
                foreach (var sale in salesSinceOpen)
                {
                    if (string.IsNullOrEmpty(sale.CashCloseFolio))
                    {
                        sale.CashCloseFolio = cashClose.Folio;
                        await _saleRepository.UpdateAsync(sale);
                    }
                }

                var creditPaymentsSinceOpen = await _creditPaymentRepository.FindAsync(p => p.PaymentDate >= cashClose.OpeningDate);
                foreach (var payment in creditPaymentsSinceOpen)
                {
                    if (string.IsNullOrEmpty(payment.CashCloseFolio))
                    {
                        payment.CashCloseFolio = cashClose.Folio;
                        await _creditPaymentRepository.UpdateAsync(payment);
                    }
                }

                var layawayPaymentsSinceOpen = await _layawayPaymentRepository.FindAsync(p => p.PaymentDate >= cashClose.OpeningDate);
                foreach (var payment in layawayPaymentsSinceOpen)
                {
                    if (string.IsNullOrEmpty(payment.CashCloseFolio))
                    {
                        payment.CashCloseFolio = cashClose.Folio;
                        await _layawayPaymentRepository.UpdateAsync(payment);
                    }
                }

                Console.WriteLine($"[CashCloseService] Folio de corte asignado a transacciones: {cashClose.Folio}");
                Console.WriteLine($"[CashCloseService] === CORTE DE CAJA COMPLETADO ===");
                Console.WriteLine($"  Fondo inicial: ${cashClose.OpeningCash}");
                Console.WriteLine($"  + Ventas efectivo: ${totals.TotalCash}");
                Console.WriteLine($"  + Abonos créditos (efectivo): ${totals.CreditCash}");
                Console.WriteLine($"  + Abonos apartados (efectivo): ${totals.LayawayCash}");
                Console.WriteLine($"  + Ingresos extra: ${totals.TotalIncome}");
                Console.WriteLine($"  - Gastos: ${totals.TotalExpenses}");
                Console.WriteLine($"  = EFECTIVO ESPERADO: ${expectedCash}");
                Console.WriteLine($"  Declarado por cajero: ${declaredAmount}");
                Console.WriteLine($"  Diferencia (sobrante/faltante): ${cashClose.Surplus}");
                Console.WriteLine($"  ---");
                Console.WriteLine($"  TOTAL DEL CORTE (Productividad): ${totals.TotalDelCorte}");

                return CashCloseResult.Ok(cashClose);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[CashCloseService] Error al cerrar caja: {ex.Message}");
                return CashCloseResult.Error($"Error al cerrar caja: {ex.Message}");
            }
        }

        /// <summary>Agrega un movimiento (gasto o ingreso) al turno actual.</summary>
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

        /// <summary>Obtiene los movimientos de un corte de caja.</summary>
        public async Task<List<CashMovement>> GetMovementsAsync(int cashCloseId)
        {
            try
            {
                return await _movementRepository.FindAsync(m => m.CashCloseId == cashCloseId);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[CashCloseService] Error al obtener movimientos: {ex.Message}");
                return new List<CashMovement>();
            }
        }

        /// <summary>
        /// Obtiene el historial de cortes de una sucursal.
        /// Solo incluye cortes ya cerrados (excluye el corte actualmente abierto).
        /// </summary>
        public async Task<List<CashClose>> GetHistoryAsync(int branchId, int limit = 30)
        {
            try
            {
                return await _cashCloseRepository.GetHistoryByBranchAsync(branchId, limit);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[CashCloseService] Error al obtener historial: {ex.Message}");
                return new List<CashClose>();
            }
        }

        /// <summary>Genera un folio único para el corte usando FolioService.</summary>
        private async Task<string> GenerateFolioAsync(int branchId)
        {
            try
            {
                var terminalId = _configService.PosTerminalConfig.TerminalId ?? "CAJA-01";
                var cajaId = int.TryParse(terminalId.Replace("CAJA-", ""), out var caja) ? caja : 1;
                return await _folioService.GenerarFolioCorteAsync(branchId, cajaId);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[CashCloseService] Error generando folio: {ex.Message}");
                return $"CC-{DateTime.Now:yyyyMMddHHmmss}";
            }
        }
    }
}
