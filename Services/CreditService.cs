using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CasaCejaRemake.Data;
using CasaCejaRemake.Data.Repositories;
using CasaCejaRemake.Helpers;
using CasaCejaRemake.Models;
using CasaCejaRemake.Services.Interfaces;

namespace CasaCejaRemake.Services
{
    public class CreditService : ICreditService
    {
        private readonly DatabaseService _databaseService;
        private readonly BaseRepository<Credit> _creditRepository;
        private readonly BaseRepository<CreditProduct> _creditProductRepository;
        private readonly BaseRepository<CreditPayment> _creditPaymentRepository;
        private readonly BaseRepository<Customer> _customerRepository;
        private readonly BaseRepository<Branch> _branchRepository;
        private readonly TicketService _ticketService;

        public CreditService(DatabaseService databaseService)
        {
            _databaseService = databaseService;
            _creditRepository = new BaseRepository<Credit>(databaseService);
            _creditProductRepository = new BaseRepository<CreditProduct>(databaseService);
            _creditPaymentRepository = new BaseRepository<CreditPayment>(databaseService);
            _customerRepository = new BaseRepository<Customer>(databaseService);
            _branchRepository = new BaseRepository<Branch>(databaseService);
            _ticketService = new TicketService();
        }



        public async Task<(bool Success, Credit? Credit, string? Error)> CreateCreditAsync(
            List<CartItem> items,
            int customerId,
            int monthsToPay,
            decimal initialPayment,
            PaymentMethod paymentMethod,
            int userId,
            int branchId,
            string? notes)
        {
            if (items == null || !items.Any())
                return (false, null, "No hay productos para crear el credito.");

            if (monthsToPay <= 0)
                return (false, null, "Los meses para pagar deben ser mayor a 0.");

            var customer = await _customerRepository.GetByIdAsync(customerId);
            if (customer == null)
                return (false, null, "Cliente no encontrado.");

            var branch = await _branchRepository.GetByIdAsync(branchId);

            decimal total = items.Sum(i => i.LineTotal);
            
            if (initialPayment < 0)
                return (false, null, "El abono inicial no puede ser negativo.");
            
            if (initialPayment > total)
                return (false, null, "El abono inicial no puede ser mayor al total.");

            try
            {
                // Generar folio usando FolioService
                // Extraer cajaId del TerminalId configurado (ej: "CAJA-01" -> 1)
                var terminalId = App.ConfigService?.PosTerminalConfig.TerminalId ?? "CAJA-01";
                var cajaId = int.TryParse(terminalId.Replace("CAJA-", ""), out var caja) ? caja : 1;
                string folio = await App.FolioService!.GenerarFolioCreditoAsync(branchId, cajaId);
                var creditDate = DateTime.Now;
                var dueDate = creditDate.AddMonths(monthsToPay);

                // Crear credito (sin TotalPaid inicial, se agregará con el pago)
                var credit = new Credit
                {
                    Folio = folio,
                    CustomerId = customerId,
                    BranchId = branchId,
                    UserId = userId,
                    Total = total,
                    TotalPaid = 0, // Se actualizará con el pago inicial
                    MonthsToPay = monthsToPay,
                    CreditDate = creditDate,
                    DueDate = dueDate,
                    Status = 1, // Pending
                    Notes = notes,
                    SyncStatus = 1
                };

                // Guardar credito
                await _creditRepository.AddAsync(credit);
                var creditId = credit.Id; // SQLite actualiza el ID automáticamente

                // Guardar productos del credito
                foreach (var item in items)
                {
                    var creditProduct = new CreditProduct
                    {
                        CreditId = creditId,
                        ProductId = item.ProductId,
                        Barcode = item.Barcode,
                        ProductName = item.ProductName,
                        Quantity = item.Quantity,
                        UnitPrice = item.FinalUnitPrice,
                        LineTotal = item.LineTotal,
                        PricingData = item.PricingData
                    };

                    await _creditProductRepository.AddAsync(creditProduct);
                }

                if (initialPayment > 0)
                {
                    await AddPaymentInternalAsync(credit, initialPayment, paymentMethod, userId, "Abono inicial");
                }

                // Recargar el credit actualizado de la BD
                var updatedCredit = await _creditRepository.GetByIdAsync(creditId);
                if (updatedCredit == null)
                {
                    return (false, null, "Error al recargar crédito después del pago inicial");
                }

                // Verificar si está completamente pagado
                if (updatedCredit.TotalPaid >= updatedCredit.Total)
                {
                    updatedCredit.Status = 2;
                }

                var currentUser = (Avalonia.Application.Current as App)?.GetAuthService()?.CurrentUser;
                var ticketData = _ticketService.GenerateCreditTicket(
                    folio,
                    branch?.Name ?? "Sucursal",
                    branch?.Address ?? "",
                    string.Empty,
                    branch?.RazonSocial ?? "",
                    currentUser?.Name ?? "", 
                    customer.Name,
                    customer.Phone,
                    items,
                    total,
                    initialPayment,
                    updatedCredit.RemainingBalance,
                    dueDate,
                    monthsToPay,
                    paymentMethod
                );

                byte[] ticketCompressed = JsonCompressor.Compress(ticketData);
                updatedCredit.TicketData = ticketCompressed;
                await _creditRepository.UpdateAsync(updatedCredit);

                return (true, updatedCredit, null);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[CreditService] Error al crear crédito: {ex.Message}");
                Console.WriteLine($"[CreditService] StackTrace: {ex.StackTrace}");
                return (false, null, $"Error al crear el credito: {ex.Message}");
            }
        }

        public async Task<List<Credit>> GetPendingByCustomerAsync(int customerId)
        {
            var credits = await _creditRepository.FindAsync(c => 
                c.CustomerId == customerId && 
                (c.Status == 1 || c.Status == 3));
            
            return credits.OrderByDescending(c => c.CreditDate).ToList();
        }

        public async Task<List<Credit>> GetPendingByBranchAsync(int branchId)
        {
            var credits = await _creditRepository.FindAsync(c => 
                c.BranchId == branchId && 
                (c.Status == 1 || c.Status == 3));
            
            return credits.OrderByDescending(c => c.CreditDate).ToList();
        }

        public async Task<List<Credit>> SearchAsync(int? customerId, int? status, int branchId)
        {
            var credits = await _creditRepository.FindAsync(c => c.BranchId == branchId);

            if (customerId.HasValue)
                credits = credits.Where(c => c.CustomerId == customerId.Value).ToList();

            if (status.HasValue)
                credits = credits.Where(c => c.Status == status.Value).ToList();

            return credits.OrderByDescending(c => c.CreditDate).ToList();
        }

        public async Task<Credit?> GetByIdAsync(int id)
        {
            return await _creditRepository.GetByIdAsync(id);
        }

        public async Task<Credit?> GetByFolioAsync(string folio)
        {
            if (string.IsNullOrWhiteSpace(folio)) return null;

            var credits = await _creditRepository.FindAsync(c => c.Folio == folio);
            return credits.FirstOrDefault();
        }

        public async Task<List<CreditProduct>> GetProductsAsync(int creditId)
        {
            var products = await _creditProductRepository.FindAsync(p => p.CreditId == creditId);
            return products.ToList();
        }

        public async Task<List<CreditPayment>> GetPaymentsAsync(int creditId)
        {
            var payments = await _creditPaymentRepository.FindAsync(p => p.CreditId == creditId);
            return payments.OrderByDescending(p => p.PaymentDate).ToList();
        }

        public async Task<bool> AddPaymentAsync(int creditId, decimal amount, PaymentMethod method, int userId, string? notes)
        {
            var credit = await _creditRepository.GetByIdAsync(creditId);
            if (credit == null) return false;

            if (credit.Status == 2 || credit.Status == 4)
                return false;

            var remaining = credit.RemainingBalance;
            if (amount > remaining)
                amount = remaining;

            if (amount <= 0) return false;

            return await AddPaymentInternalAsync(credit, amount, method, userId, notes);
        }

        /// <summary>
        /// Agrega un abono con pagos mixtos (múltiples métodos de pago)
        /// </summary>
        public async Task<bool> AddPaymentWithMixedAsync(int creditId, decimal amount, string paymentJson, int userId, string? notes)
        {
            var credit = await _creditRepository.GetByIdAsync(creditId);
            if (credit == null) return false;

            if (credit.Status == 2 || credit.Status == 4)
                return false;

            var remaining = credit.RemainingBalance;
            if (amount > remaining)
                amount = remaining;

            if (amount <= 0) return false;

            try
            {
                // Los pagos/abonos generan su propio folio tipo P con secuencial diario
                var terminalId = App.ConfigService?.PosTerminalConfig.TerminalId ?? "CAJA-01";
                var cajaId = int.TryParse(terminalId.Replace("CAJA-", ""), out var caja) ? caja : 1;
                var paymentFolio = await App.FolioService!.GenerarFolioPagoAsync(credit.BranchId, cajaId);

                // Crear registro de pago con JSON guardado en PaymentMethod
                var payment = new CreditPayment
                {
                    Folio = paymentFolio,
                    CreditId = creditId,
                    UserId = userId,
                    AmountPaid = amount,
                    PaymentMethod = paymentJson, // Guardar el JSON directamente
                    PaymentDate = DateTime.Now,
                    CashCloseFolio = string.Empty,
                    Notes = notes,
                    SyncStatus = 1
                };

                await _creditPaymentRepository.AddAsync(payment);

                // Actualizar saldo del crédito
                credit.TotalPaid += amount;
                credit.UpdatedAt = DateTime.Now;

                // Si se pagó todo, marcar como completado
                if (credit.TotalPaid >= credit.Total)
                {
                    credit.Status = 2; // Completado
                }

                await _creditRepository.UpdateAsync(credit);
                return true;
            }
            catch
            {
                return false;
            }
        }

        private async Task<bool> AddPaymentInternalAsync(Credit credit, decimal amount, PaymentMethod method, int userId, string? notes)
        {
            try
            {
                // Los pagos/abonos generan su propio folio tipo P con secuencial diario
                var terminalId = App.ConfigService?.PosTerminalConfig.TerminalId ?? "CAJA-01";
                var cajaId = int.TryParse(terminalId.Replace("CAJA-", ""), out var caja) ? caja : 1;
                var paymentFolio = await App.FolioService!.GenerarFolioPagoAsync(credit.BranchId, cajaId);

                // Convertir el enum a snake_case: TarjetaDebito -> tarjeta_debito
                var methodName = method.ToString();
                var snakeCaseMethod = System.Text.RegularExpressions.Regex.Replace(methodName, "([a-z])([A-Z])", "$1_$2").ToLower();

                // Serializar el método de pago a JSON con la misma estructura que pagos mixtos
                var paymentJson = System.Text.Json.JsonSerializer.Serialize(new Dictionary<string, decimal>
                {
                    { snakeCaseMethod, amount }
                });

                var payment = new CreditPayment
                {
                    Folio = paymentFolio,
                    CreditId = credit.Id,
                    UserId = userId,
                    AmountPaid = amount,
                    PaymentMethod = paymentJson,
                    PaymentDate = DateTime.Now,
                    CashCloseFolio = string.Empty,
                    Notes = notes,
                    SyncStatus = 1
                };

                await _creditPaymentRepository.AddAsync(payment);

                credit.TotalPaid += amount;
                credit.UpdatedAt = DateTime.Now;

                if (credit.TotalPaid >= credit.Total)
                {
                    credit.Status = 2;
                }

                await _creditRepository.UpdateAsync(credit);
                return true;
            }
            catch
            {
                return false;
            }
        }

        public async Task UpdateStatusAsync(int creditId)
        {
            var credit = await _creditRepository.GetByIdAsync(creditId);
            if (credit == null) return;

            if (credit.Status != 1) return;

            if (credit.IsOverdue)
            {
                credit.Status = 3;
                credit.UpdatedAt = DateTime.Now;
                await _creditRepository.UpdateAsync(credit);
            }
        }

        public async Task<TicketData?> RecoverTicketAsync(int creditId)
        {
            var credit = await _creditRepository.GetByIdAsync(creditId);
            if (credit?.TicketData == null) return null;

            return JsonCompressor.Decompress<TicketData>(credit.TicketData);
        }
    }
}
