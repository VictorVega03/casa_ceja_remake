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
    /// Basado en las reglas de negocio documentadas en reglas_corte.md
    /// </summary>
    public class CashCloseTotals
    {
        // ==================== VENTAS DIRECTAS POR MÉTODO DE PAGO ====================
        // Se usa el TOTAL de la venta (no el monto pagado). El cambio está implícito.
        
        /// <summary>Ventas directas pagadas en efectivo (Total de venta, no AmountPaid)</summary>
        public decimal TotalCash { get; set; }
        
        /// <summary>Ventas directas pagadas con tarjeta débito</summary>
        public decimal TotalDebitCard { get; set; }
        
        /// <summary>Ventas directas pagadas con tarjeta crédito</summary>
        public decimal TotalCreditCard { get; set; }
        
        /// <summary>Ventas directas pagadas con transferencia</summary>
        public decimal TotalTransfers { get; set; }
        
        /// <summary>Ventas directas pagadas con cheque</summary>
        public decimal TotalChecks { get; set; }
        
        /// <summary>Total de ventas directas (todos los métodos)</summary>
        public decimal TotalSales { get; set; }
        
        /// <summary>Número de ventas directas</summary>
        public int SalesCount { get; set; }

        // ==================== CRÉDITOS ====================
        
        /// <summary>Total de TODOS los créditos CREADOS en el turno (valor completo)</summary>
        public decimal CreditTotalCreated { get; set; }
        
        /// <summary>Efectivo recibido por abonos/enganches de créditos</summary>
        public decimal CreditCash { get; set; }
        
        /// <summary>Número de créditos creados</summary>
        public int CreditCount { get; set; }

        // ==================== APARTADOS ====================
        
        /// <summary>Total de TODOS los apartados CREADOS en el turno (valor completo)</summary>
        public decimal LayawayTotalCreated { get; set; }
        
        /// <summary>Efectivo recibido por abonos de apartados</summary>
        public decimal LayawayCash { get; set; }
        
        /// <summary>Número de apartados creados</summary>
        public int LayawayCount { get; set; }

        // ==================== MOVIMIENTOS DE CAJA ====================
        
        /// <summary>Total de gastos registrados (sale de caja)</summary>
        public decimal TotalExpenses { get; set; }
        
        /// <summary>Total de ingresos extra registrados (entra a caja)</summary>
        public decimal TotalIncome { get; set; }

        // ==================== PROPIEDADES CALCULADAS ====================

        /// <summary>
        /// TOTAL DEL CORTE: Mide la productividad total del turno.
        /// = Ventas Directas + Créditos Creados + Apartados Creados
        /// </summary>
        public decimal TotalDelCorte => TotalSales + CreditTotalCreated + LayawayTotalCreated;

        /// <summary>
        /// EFECTIVO ESPERADO: Lo que debería haber físicamente en caja.
        /// = Fondo + Efectivo Ventas + Efectivo Abonos + Ingresos - Gastos
        /// NOTA: El cambio NO se resta porque ya está implícito al usar sale.Total
        /// </summary>
        public decimal CalcularEfectivoEsperado(decimal fondoApertura)
        {
            return fondoApertura + TotalCash + CreditCash + LayawayCash + TotalIncome - TotalExpenses;
        }
    }

    /// <summary>
    /// Servicio para gestión de apertura y cierre de caja.
    /// Implementa las reglas de negocio documentadas en reglas_corte.md
    /// </summary>
    public class CashCloseService
    {
        private readonly DatabaseService _databaseService;
        private readonly BaseRepository<CashClose> _cashCloseRepository;
        private readonly BaseRepository<CashMovement> _movementRepository;
        private readonly BaseRepository<Sale> _saleRepository;
        private readonly BaseRepository<Credit> _creditRepository;
        private readonly BaseRepository<Layaway> _layawayRepository;
        private readonly BaseRepository<LayawayPayment> _layawayPaymentRepository;
        private readonly BaseRepository<CreditPayment> _creditPaymentRepository;

        public CashCloseService(DatabaseService databaseService)
        {
            _databaseService = databaseService;
            _cashCloseRepository = new BaseRepository<CashClose>(databaseService);
            _movementRepository = new BaseRepository<CashMovement>(databaseService);
            _saleRepository = new BaseRepository<Sale>(databaseService);
            _creditRepository = new BaseRepository<Credit>(databaseService);
            _layawayRepository = new BaseRepository<Layaway>(databaseService);
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
                // Se contabiliza el TOTAL de la venta (NO AmountPaid)
                // El cambio dado ya está implícito en esta operación
                
                var allSales = await _saleRepository.GetAllAsync();
                var salesSinceOpen = allSales.Where(s => s.SaleDate >= openingDate).ToList();
                
                totals.SalesCount = salesSinceOpen.Count;
                Console.WriteLine($"[CashCloseService] Ventas directas: {salesSinceOpen.Count}");

                foreach (var sale in salesSinceOpen)
                {
                    // Clasificar por método de pago usando el TOTAL de la venta
                    if (sale.PaymentMethod.StartsWith("{"))
                    {
                        // Pago mixto (JSON) - distribuir proporcionalmente
                        ProcessMixedPaymentSale(sale, totals);
                    }
                    else
                    {
                        // Pago simple
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
                // El TOTAL del crédito suma al "Total del Corte" (productividad)
                // Solo el efectivo de enganches/abonos suma al "Efectivo Esperado"
                
                var allCredits = await _creditRepository.GetAllAsync();
                var creditsCreated = allCredits.Where(c => c.CreditDate >= openingDate).ToList();
                
                totals.CreditCount = creditsCreated.Count;
                totals.CreditTotalCreated = creditsCreated.Sum(c => c.Total);
                
                Console.WriteLine($"[CashCloseService] Créditos creados: {creditsCreated.Count}, Total: ${totals.CreditTotalCreated}");

                // Abonos de créditos (incluyendo enganches) en efectivo
                var allCreditPayments = await _creditPaymentRepository.GetAllAsync();
                var creditPaymentsSinceOpen = allCreditPayments.Where(p => p.PaymentDate >= openingDate).ToList();
                
                foreach (var payment in creditPaymentsSinceOpen)
                {
                    decimal cashAmount = ExtractCashFromPaymentMethod(payment.PaymentMethod, payment.AmountPaid);
                    totals.CreditCash += cashAmount;
                }
                
                Console.WriteLine($"[CashCloseService] Efectivo de créditos (abonos): ${totals.CreditCash}");

                // ==================== 3. APARTADOS CREADOS ====================
                // El TOTAL del apartado suma al "Total del Corte" (productividad)
                // Solo el efectivo de abonos suma al "Efectivo Esperado"
                
                var allLayaways = await _layawayRepository.GetAllAsync();
                var layawaysCreated = allLayaways.Where(l => l.LayawayDate >= openingDate).ToList();
                
                totals.LayawayCount = layawaysCreated.Count;
                totals.LayawayTotalCreated = layawaysCreated.Sum(l => l.Total);
                
                Console.WriteLine($"[CashCloseService] Apartados creados: {layawaysCreated.Count}, Total: ${totals.LayawayTotalCreated}");

                // Abonos de apartados en efectivo
                var allLayawayPayments = await _layawayPaymentRepository.GetAllAsync();
                var layawayPaymentsSinceOpen = allLayawayPayments.Where(p => p.PaymentDate >= openingDate).ToList();
                
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

                // Distribuir el Total de la venta proporcionalmente
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
        /// <param name="paymentMethod">Método de pago (JSON o string)</param>
        /// <param name="totalAmount">Monto total del pago (usado si es pago simple en efectivo)</param>
        private decimal ExtractCashFromPaymentMethod(string paymentMethod, decimal totalAmount)
        {
            if (string.IsNullOrEmpty(paymentMethod))
                return 0;

            // Si es "Efectivo" string simple, el abono completo es en efectivo
            if (paymentMethod == "Efectivo")
                return totalAmount;

            // Si es JSON
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
                
                // Calcular totales según reglas de negocio
                var totals = await CalculateTotalsAsync(cashClose.Id, cashClose.OpeningDate);
                
                Console.WriteLine($"[CashCloseService] Totales calculados:");
                Console.WriteLine($"  - Ventas efectivo: ${totals.TotalCash}");
                Console.WriteLine($"  - Créditos creados: ${totals.CreditTotalCreated}");
                Console.WriteLine($"  - Apartados creados: ${totals.LayawayTotalCreated}");
                Console.WriteLine($"  - Efectivo abonos créditos: ${totals.CreditCash}");
                Console.WriteLine($"  - Efectivo abonos apartados: ${totals.LayawayCash}");

                // ==================== ACTUALIZAR CAMPOS DEL MODELO ====================
                // IMPORTANTE: Redondear todos los valores a 2 decimales para evitar
                // problemas de precisión de punto flotante en la base de datos
                
                // Ventas directas por método de pago
                cashClose.TotalCash = Math.Round(totals.TotalCash, 2);
                cashClose.TotalDebitCard = Math.Round(totals.TotalDebitCard, 2);
                cashClose.TotalCreditCard = Math.Round(totals.TotalCreditCard, 2);
                cashClose.TotalTransfers = Math.Round(totals.TotalTransfers, 2);
                cashClose.TotalChecks = Math.Round(totals.TotalChecks, 2);
                
                // Créditos y Apartados CREADOS (para Total del Corte / productividad)
                cashClose.CreditTotalCreated = Math.Round(totals.CreditTotalCreated, 2);
                cashClose.LayawayTotalCreated = Math.Round(totals.LayawayTotalCreated, 2);
                
                // Efectivo de abonos (para Efectivo Esperado)
                cashClose.CreditCash = Math.Round(totals.CreditCash, 2);
                cashClose.LayawayCash = Math.Round(totals.LayawayCash, 2);
                
                // Total de ventas directas
                cashClose.TotalSales = Math.Round(totals.TotalSales, 2);

                // Serializar gastos e ingresos como JSON
                var movements = await GetMovementsAsync(cashClose.Id);
                var expenses = movements.Where(m => m.IsExpense).Select(m => new { description = m.Concept, amount = m.Amount }).ToList();
                var income = movements.Where(m => !m.IsExpense).Select(m => new { description = m.Concept, amount = m.Amount }).ToList();
                
                cashClose.Expenses = System.Text.Json.JsonSerializer.Serialize(expenses);
                cashClose.Income = System.Text.Json.JsonSerializer.Serialize(income);

                // ==================== FÓRMULA DE EFECTIVO ESPERADO ====================
                // IMPORTANTE: NO restamos cambio porque al usar sale.Total ya está implícito.
                // 
                // Efectivo Esperado = Fondo Inicial 
                //                   + Ventas en Efectivo (sale.Total, no AmountPaid)
                //                   + Efectivo de Abonos/Enganches de Créditos
                //                   + Efectivo de Abonos de Apartados
                //                   + Ingresos Extra
                //                   - Gastos
                decimal expectedCash = cashClose.OpeningCash 
                                     + totals.TotalCash      // Ventas directas en efectivo
                                     + totals.CreditCash     // Efectivo de abonos créditos
                                     + totals.LayawayCash    // Efectivo de abonos apartados
                                     + totals.TotalIncome    // Ingresos extra
                                     - totals.TotalExpenses; // Gastos

                // Redondear a 2 decimales
                cashClose.ExpectedCash = Math.Round(expectedCash, 2);
                cashClose.Surplus = Math.Round(declaredAmount - expectedCash, 2);
                cashClose.CloseDate = DateTime.Now;
                cashClose.UpdatedAt = DateTime.Now;

                await _cashCloseRepository.UpdateAsync(cashClose);

                // ==================== ASIGNAR FOLIO DE CORTE A TODAS LAS TRANSACCIONES ====================
                // Actualizar ventas con el folio del corte
                var salesSinceOpen = await _saleRepository.FindAsync(s => s.SaleDate >= cashClose.OpeningDate);
                foreach (var sale in salesSinceOpen)
                {
                    if (string.IsNullOrEmpty(sale.CashCloseFolio))
                    {
                        sale.CashCloseFolio = cashClose.Folio;
                        await _saleRepository.UpdateAsync(sale);
                    }
                }

                // Actualizar pagos de créditos con el folio del corte
                var creditPaymentsSinceOpen = await _creditPaymentRepository.FindAsync(p => p.PaymentDate >= cashClose.OpeningDate);
                foreach (var payment in creditPaymentsSinceOpen)
                {
                    if (string.IsNullOrEmpty(payment.CashCloseFolio))
                    {
                        payment.CashCloseFolio = cashClose.Folio;
                        await _creditPaymentRepository.UpdateAsync(payment);
                    }
                }

                // Actualizar pagos de apartados con el folio del corte
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
        /// Genera un folio único para el corte usando FolioService.
        /// </summary>
        private async Task<string> GenerateFolioAsync(int branchId)
        {
            try
            {
                // Extraer cajaId del TerminalId configurado (ej: "CAJA-01" -> 1)
                var terminalId = App.ConfigService?.PosTerminalConfig.TerminalId ?? "CAJA-01";
                var cajaId = int.TryParse(terminalId.Replace("CAJA-", ""), out var caja) ? caja : 1;
                return await App.FolioService!.GenerarFolioCorteAsync(branchId, cajaId);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[CashCloseService] Error generando folio: {ex.Message}");
                // Fallback en caso de error
                return $"CC-{DateTime.Now:yyyyMMddHHmmss}";
            }
        }
    }
}
