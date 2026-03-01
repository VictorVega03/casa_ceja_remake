using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CasaCejaRemake.Data;
using CasaCejaRemake.Data.Repositories;
using CasaCejaRemake.Data.Repositories.Interfaces;
using CasaCejaRemake.Helpers;
using CasaCejaRemake.Models;
using CasaCejaRemake.Services.Interfaces;

namespace CasaCejaRemake.Services
{
    public class CreditService : ICreditService
    {
        private readonly ICreditRepository _creditRepository;
        private readonly BaseRepository<CreditProduct> _creditProductRepository;
        private readonly ICreditPaymentRepository _creditPaymentRepository;
        private readonly ICustomerRepository _customerRepository;
        private readonly IBranchRepository _branchRepository;
        private readonly ITicketService _ticketService;
        private readonly IFolioService _folioService;
        private readonly IConfigService _configService;

        public CreditService(
            ICreditRepository creditRepository,
            DatabaseService databaseService,
            ICreditPaymentRepository creditPaymentRepository,
            ICustomerRepository customerRepository,
            IBranchRepository branchRepository,
            ITicketService ticketService,
            IFolioService folioService,
            IConfigService configService)
        {
            _creditRepository = creditRepository;
            _creditProductRepository = new BaseRepository<CreditProduct>(databaseService);
            _creditPaymentRepository = creditPaymentRepository;
            _customerRepository = customerRepository;
            _branchRepository = branchRepository;
            _ticketService = ticketService;
            _folioService = folioService;
            _configService = configService;
        }

        private (int cajaId, string terminalId) GetCajaInfo()
        {
            var terminalId = _configService.PosTerminalConfig.TerminalId ?? "CAJA-01";
            var cajaId = int.TryParse(terminalId.Replace("CAJA-", ""), out var caja) ? caja : 1;
            return (cajaId, terminalId);
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
                var (cajaId, _) = GetCajaInfo();
                string folio = await _folioService.GenerarFolioCreditoAsync(branchId, cajaId);
                var creditDate = DateTime.Now;
                var dueDate = creditDate.AddMonths(monthsToPay);

                var credit = new Credit
                {
                    Folio = folio,
                    CustomerId = customerId,
                    BranchId = branchId,
                    UserId = userId,
                    Total = total,
                    TotalPaid = 0,
                    MonthsToPay = monthsToPay,
                    CreditDate = creditDate,
                    DueDate = dueDate,
                    Status = 1,
                    Notes = notes,
                    SyncStatus = 1
                };

                await _creditRepository.AddAsync(credit);
                var creditId = credit.Id;

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
                    await AddPaymentInternalAsync(credit, initialPayment, paymentMethod, userId, "Abono inicial");

                var updatedCredit = await _creditRepository.GetByIdAsync(creditId);
                if (updatedCredit == null)
                    return (false, null, "Error al recargar crédito después del pago inicial");

                if (updatedCredit.TotalPaid >= updatedCredit.Total)
                    updatedCredit.Status = 2;

                // Nombre del cajero: el ViewModel lo pasa si lo tiene. Guardamos lo que esté en la BD.
                var dbUser = await _customerRepository.GetByIdAsync(userId); // fallback
                var ticketData = _ticketService.GenerateCreditTicket(
                    folio,
                    branch?.Name ?? "Sucursal",
                    branch?.Address ?? "",
                    string.Empty,
                    branch?.RazonSocial ?? "",
                    userId.ToString(),
                    customer.Name,
                    customer.Phone,
                    items,
                    total,
                    initialPayment,
                    updatedCredit.RemainingBalance,
                    dueDate,
                    monthsToPay,
                    paymentMethod);

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
                var (cajaId, _) = GetCajaInfo();
                var paymentFolio = await _folioService.GenerarFolioPagoAsync(credit.BranchId, cajaId);

                var payment = new CreditPayment
                {
                    Folio = paymentFolio,
                    CreditId = creditId,
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
                    credit.Status = 2;

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
                var (cajaId, _) = GetCajaInfo();
                var paymentFolio = await _folioService.GenerarFolioPagoAsync(credit.BranchId, cajaId);

                var methodName = method.ToString();
                var snakeCaseMethod = System.Text.RegularExpressions.Regex
                    .Replace(methodName, "([a-z])([A-Z])", "$1_$2").ToLower();

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
                    credit.Status = 2;

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
