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
    public class LayawayService : ILayawayService
    {
        private readonly DatabaseService _databaseService;
        private readonly BaseRepository<Layaway> _layawayRepository;
        private readonly BaseRepository<LayawayProduct> _layawayProductRepository;
        private readonly BaseRepository<LayawayPayment> _layawayPaymentRepository;
        private readonly BaseRepository<Customer> _customerRepository;
        private readonly BaseRepository<Branch> _branchRepository;
        private readonly TicketService _ticketService;

        public LayawayService(DatabaseService databaseService)
        {
            _databaseService = databaseService;
            _layawayRepository = new BaseRepository<Layaway>(databaseService);
            _layawayProductRepository = new BaseRepository<LayawayProduct>(databaseService);
            _layawayPaymentRepository = new BaseRepository<LayawayPayment>(databaseService);
            _customerRepository = new BaseRepository<Customer>(databaseService);
            _branchRepository = new BaseRepository<Branch>(databaseService);
            _ticketService = new TicketService();
        }



        public async Task<(bool Success, Layaway? Layaway, string? Error)> CreateLayawayAsync(
            List<CartItem> items,
            int customerId,
            int daysToPickup,
            decimal initialPayment,
            PaymentMethod paymentMethod,
            int userId,
            int branchId,
            string? notes)
        {
            if (items == null || !items.Any())
                return (false, null, "No hay productos para crear el apartado.");

            if (daysToPickup <= 0)
                return (false, null, "Los dias para recoger deben ser mayor a 0.");

            var customer = await _customerRepository.GetByIdAsync(customerId);
            if (customer == null)
                return (false, null, "Cliente no encontrado.");

            var branch = await _branchRepository.GetByIdAsync(branchId);

            decimal total = items.Sum(i => i.LineTotal);
            
            if (initialPayment < 0)
                return (false, null, "El abono inicial no puede ser negativo.");
            
            if (initialPayment > total)
                return (false, null, "El abono inicial no puede ser mayor al total.");

            if (initialPayment <= 0)
                return (false, null, "Se requiere un abono inicial para crear el apartado.");

            try
            {
                // Generar folio usando FolioService
                // Extraer cajaId del TerminalId configurado (ej: "CAJA-01" -> 1)
                var terminalId = App.ConfigService?.PosTerminalConfig.TerminalId ?? "CAJA-01";
                var cajaId = int.TryParse(terminalId.Replace("CAJA-", ""), out var caja) ? caja : 1;
                string folio = await App.FolioService!.GenerarFolioApartadoAsync(branchId, cajaId);
                var layawayDate = DateTime.Now;
                var pickupDate = layawayDate.AddDays(daysToPickup);

                // Crear apartado (sin TotalPaid inicial, se agregará con el pago)
                var layaway = new Layaway
                {
                    Folio = folio,
                    CustomerId = customerId,
                    BranchId = branchId,
                    UserId = userId,
                    DeliveryUserId = null,
                    Total = total,
                    TotalPaid = 0, // Se actualizará con el pago inicial
                    LayawayDate = layawayDate,
                    PickupDate = pickupDate,
                    DeliveryDate = null,
                    Status = 1, // Pending
                    Notes = notes,
                    SyncStatus = 1
                };

                // Guardar apartado
                await _layawayRepository.AddAsync(layaway);
                var layawayId = layaway.Id; // SQLite actualiza el ID automáticamente

                // Guardar productos del apartado
                foreach (var item in items)
                {
                    var layawayProduct = new LayawayProduct
                    {
                        LayawayId = layawayId,
                        ProductId = item.ProductId,
                        Barcode = item.Barcode,
                        ProductName = item.ProductName,
                        Quantity = item.Quantity,
                        UnitPrice = item.FinalUnitPrice,
                        LineTotal = item.LineTotal,
                        PricingData = item.PricingData
                    };

                    await _layawayProductRepository.AddAsync(layawayProduct);
                }

                // Guardar el pago inicial
                await AddPaymentInternalAsync(layaway, initialPayment, paymentMethod, userId, "Abono inicial");

                // Recargar el layaway actualizado de la BD
                var updatedLayaway = await _layawayRepository.GetByIdAsync(layawayId);
                if (updatedLayaway == null)
                {
                    return (false, null, "Error al recargar apartado después del pago inicial");
                }

                // Generar y guardar ticket data
                var currentUser = (Avalonia.Application.Current as App)?.GetAuthService()?.CurrentUser;
                var ticketData = _ticketService.GenerateLayawayTicket(
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
                    updatedLayaway.RemainingBalance,
                    pickupDate,
                    daysToPickup,
                    paymentMethod
                );

                byte[] ticketCompressed = JsonCompressor.Compress(ticketData);
                updatedLayaway.TicketData = ticketCompressed;
                
                // Actualizar el layaway con el ticket
                await _layawayRepository.UpdateAsync(updatedLayaway);

                return (true, updatedLayaway, null);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[LayawayService] Error al crear apartado: {ex.Message}");
                Console.WriteLine($"[LayawayService] StackTrace: {ex.StackTrace}");
                return (false, null, $"Error al crear el apartado: {ex.Message}");
            }
        }

        public async Task<List<Layaway>> GetPendingByCustomerAsync(int customerId)
        {
            var layaways = await _layawayRepository.FindAsync(l => 
                l.CustomerId == customerId && 
                (l.Status == 1 || l.Status == 3)); // Pending or Expired
            
            return layaways.OrderByDescending(l => l.LayawayDate).ToList();
        }

        public async Task<List<Layaway>> GetPendingByBranchAsync(int branchId)
        {
            var layaways = await _layawayRepository.FindAsync(l => 
                l.BranchId == branchId && 
                (l.Status == 1 || l.Status == 3)); // Pending or Expired
            
            return layaways.OrderByDescending(l => l.LayawayDate).ToList();
        }

        public async Task<List<Layaway>> SearchAsync(int? customerId, int? status, int branchId)
        {
            var layaways = await _layawayRepository.FindAsync(l => l.BranchId == branchId);

            if (customerId.HasValue)
                layaways = layaways.Where(l => l.CustomerId == customerId.Value).ToList();

            if (status.HasValue)
                layaways = layaways.Where(l => l.Status == status.Value).ToList();

            return layaways.OrderByDescending(l => l.LayawayDate).ToList();
        }

        public async Task<Layaway?> GetByIdAsync(int id)
        {
            return await _layawayRepository.GetByIdAsync(id);
        }

        public async Task<Layaway?> GetByFolioAsync(string folio)
        {
            if (string.IsNullOrWhiteSpace(folio)) return null;

            var layaways = await _layawayRepository.FindAsync(l => l.Folio == folio);
            return layaways.FirstOrDefault();
        }

        public async Task<List<LayawayProduct>> GetProductsAsync(int layawayId)
        {
            var products = await _layawayProductRepository.FindAsync(p => p.LayawayId == layawayId);
            return products.ToList();
        }

        public async Task<List<LayawayPayment>> GetPaymentsAsync(int layawayId)
        {
            var payments = await _layawayPaymentRepository.FindAsync(p => p.LayawayId == layawayId);
            return payments.OrderByDescending(p => p.PaymentDate).ToList();
        }

        public async Task<bool> AddPaymentAsync(int layawayId, decimal amount, PaymentMethod method, int userId, string? notes)
        {
            var layaway = await _layawayRepository.GetByIdAsync(layawayId);
            if (layaway == null) return false;

            if (layaway.Status == 2 || layaway.Status == 4)
                return false;

            var remaining = layaway.RemainingBalance;
            if (amount > remaining)
                amount = remaining;

            if (amount <= 0) return false;

            return await AddPaymentInternalAsync(layaway, amount, method, userId, notes);
        }

        /// <summary>
        /// Agrega un abono con pagos mixtos (múltiples métodos de pago)
        /// </summary>
        public async Task<bool> AddPaymentWithMixedAsync(int layawayId, decimal amount, string paymentJson, int userId, string? notes)
        {
            var layaway = await _layawayRepository.GetByIdAsync(layawayId);
            if (layaway == null) return false;

            if (layaway.Status == 2 || layaway.Status == 4)
                return false;

            var remaining = layaway.RemainingBalance;
            if (amount > remaining)
                amount = remaining;

            if (amount <= 0) return false;

            try
            {
                // Los pagos/abonos generan su propio folio tipo P con secuencial diario
                var terminalId = App.ConfigService?.PosTerminalConfig.TerminalId ?? "CAJA-01";
                var cajaId = int.TryParse(terminalId.Replace("CAJA-", ""), out var caja) ? caja : 1;
                var paymentFolio = await App.FolioService!.GenerarFolioPagoAsync(layaway.BranchId, cajaId);

                // Crear registro de pago con JSON guardado en PaymentMethod
                var payment = new LayawayPayment
                {
                    Folio = paymentFolio,
                    LayawayId = layawayId,
                    UserId = userId,
                    AmountPaid = amount,
                    PaymentMethod = paymentJson, // Guardar el JSON directamente
                    PaymentDate = DateTime.Now,
                    CashCloseFolio = string.Empty,
                    Notes = notes,
                    SyncStatus = 1
                };

                await _layawayPaymentRepository.AddAsync(payment);

                // Actualizar saldo del apartado
                layaway.TotalPaid += amount;
                layaway.UpdatedAt = DateTime.Now;

                await _layawayRepository.UpdateAsync(layaway);
                return true;
            }
            catch
            {
                return false;
            }
        }

        private async Task<bool> AddPaymentInternalAsync(Layaway layaway, decimal amount, PaymentMethod method, int userId, string? notes)
        {
            try
            {
                // Los pagos/abonos generan su propio folio tipo P con secuencial diario
                var terminalId = App.ConfigService?.PosTerminalConfig.TerminalId ?? "CAJA-01";
                var cajaId = int.TryParse(terminalId.Replace("CAJA-", ""), out var caja) ? caja : 1;
                var paymentFolio = await App.FolioService!.GenerarFolioPagoAsync(layaway.BranchId, cajaId);

                // Convertir el enum a snake_case: TarjetaDebito -> tarjeta_debito
                var methodName = method.ToString();
                var snakeCaseMethod = System.Text.RegularExpressions.Regex.Replace(methodName, "([a-z])([A-Z])", "$1_$2").ToLower();

                // Serializar el método de pago a JSON con la misma estructura que pagos mixtos
                var paymentJson = System.Text.Json.JsonSerializer.Serialize(new Dictionary<string, decimal>
                {
                    { snakeCaseMethod, amount }
                });

                var payment = new LayawayPayment
                {
                    Folio = paymentFolio,
                    LayawayId = layaway.Id,
                    UserId = userId,
                    AmountPaid = amount,
                    PaymentMethod = paymentJson,
                    PaymentDate = DateTime.Now,
                    CashCloseFolio = string.Empty, // Se actualizara en el corte
                    Notes = notes,
                    SyncStatus = 1
                };

                await _layawayPaymentRepository.AddAsync(payment);

                // Actualizar total pagado siempre
                layaway.TotalPaid += amount;
                layaway.UpdatedAt = DateTime.Now;
                await _layawayRepository.UpdateAsync(layaway);

                return true;
            }
            catch
            {
                return false;
            }
        }

        public async Task<bool> MarkAsDeliveredAsync(int layawayId, int deliveryUserId)
        {
            var layaway = await _layawayRepository.GetByIdAsync(layawayId);
            if (layaway == null) return false;

            if (layaway.RemainingBalance > 0)
                return false;

            if (layaway.Status != 1)
                return false;

            layaway.Status = 2; // Delivered
            layaway.DeliveryDate = DateTime.Now;
            layaway.DeliveryUserId = deliveryUserId;
            layaway.UpdatedAt = DateTime.Now;

            await _layawayRepository.UpdateAsync(layaway);
            return true;
        }

        public async Task UpdateStatusAsync(int layawayId)
        {
            var layaway = await _layawayRepository.GetByIdAsync(layawayId);
            if (layaway == null) return;

            if (layaway.Status != 1) return;

            if (layaway.IsExpired)
            {
                layaway.Status = 3;
                layaway.UpdatedAt = DateTime.Now;
                await _layawayRepository.UpdateAsync(layaway);
            }
        }

        public async Task<TicketData?> RecoverTicketAsync(int layawayId)
        {
            var layaway = await _layawayRepository.GetByIdAsync(layawayId);
            if (layaway?.TicketData == null) return null;

            return JsonCompressor.Decompress<TicketData>(layaway.TicketData);
        }
    }
}
