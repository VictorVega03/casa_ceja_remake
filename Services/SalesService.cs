using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CasaCejaRemake.Data.Repositories.Interfaces;
using CasaCejaRemake.Helpers;
using CasaCejaRemake.Models;
using CasaCejaRemake.Services.Interfaces;

namespace CasaCejaRemake.Services
{
    public class SaleResult
    {
        public bool Success { get; set; }
        public string? ErrorMessage { get; set; }
        public Sale? Sale { get; set; }
        public TicketData? Ticket { get; set; }
        public string? TicketText { get; set; }

        public static SaleResult Ok(Sale sale, TicketData ticket, string ticketText)
        {
            return new SaleResult
            {
                Success = true,
                Sale = sale,
                Ticket = ticket,
                TicketText = ticketText
            };
        }

        public static SaleResult Error(string message)
        {
            return new SaleResult
            {
                Success = false,
                ErrorMessage = message
            };
        }
    }

    public class StockValidationResult
    {
        public bool IsValid { get; set; }
        public List<string> Errors { get; set; } = new();
    }

    public class SalesService : ISalesService
    {
        private readonly ISaleRepository _saleRepository;
        private readonly ISaleProductRepository _saleProductRepository;
        private readonly IProductRepository _productRepository;
        private readonly IBranchRepository _branchRepository;
        private readonly ICategoryRepository _categoryRepository;
        private readonly IUnitRepository _unitRepository;
        private readonly IUserRepository _userRepository;
        private readonly ITicketService _ticketService;
        private readonly PricingService _pricingService;
        private readonly IFolioService _folioService;
        private readonly IConfigService _configService;

        public SalesService(
            ISaleRepository saleRepository,
            ISaleProductRepository saleProductRepository,
            IProductRepository productRepository,
            IBranchRepository branchRepository,
            ICategoryRepository categoryRepository,
            IUnitRepository unitRepository,
            IUserRepository userRepository,
            ITicketService ticketService,
            PricingService pricingService,
            IFolioService folioService,
            IConfigService configService)
        {
            _saleRepository = saleRepository;
            _saleProductRepository = saleProductRepository;
            _productRepository = productRepository;
            _branchRepository = branchRepository;
            _categoryRepository = categoryRepository;
            _unitRepository = unitRepository;
            _userRepository = userRepository;
            _ticketService = ticketService;
            _pricingService = pricingService;
            _folioService = folioService;
            _configService = configService;
        }

        public async Task<StockValidationResult> ValidateStockAsync(List<CartItem> items)
        {
            var result = new StockValidationResult { IsValid = true };

            foreach (var item in items)
            {
                var product = await _productRepository.GetByIdAsync(item.ProductId);
                if (product == null)
                {
                    result.IsValid = false;
                    result.Errors.Add($"Producto '{item.ProductName}' no encontrado.");
                    continue;
                }

                // TODO: Implement stock validation from inventory table
            }

            return result;
        }

        public async Task<int> GetNextConsecutiveAsync(int branchId)
        {
            var today = DateTime.Today;
            var sales = await _saleRepository.FindAsync(s =>
                s.BranchId == branchId &&
                s.CreatedAt >= today);

            return sales.Count + 1;
        }

        private (int cajaId, string terminalId) GetCajaInfo()
        {
            var terminalId = _configService.PosTerminalConfig.TerminalId ?? "CAJA-01";
            var cajaId = int.TryParse(terminalId.Replace("CAJA-", ""), out var caja) ? caja : 1;
            return (cajaId, terminalId);
        }

        public async Task<SaleResult> ProcessSaleAsync(
            List<CartItem> items,
            PaymentMethod paymentMethod,
            decimal amountPaid,
            int userId,
            string userName,
            int branchId,
            decimal generalDiscount = 0,
            decimal generalDiscountPercent = 0,
            bool isGeneralDiscountPercentage = true)
        {
            if (items == null || items.Count == 0)
                return SaleResult.Error("El carrito esta vacio.");

            var stockValidation = await ValidateStockAsync(items);
            if (!stockValidation.IsValid)
                return SaleResult.Error(string.Join("\n", stockValidation.Errors));

            decimal total = 0;
            decimal totalDiscount = 0;

            foreach (var item in items)
            {
                total += item.LineTotal;
                totalDiscount += item.TotalDiscount * item.Quantity;
            }

            var finalTotal = total - generalDiscount;

            if (paymentMethod == PaymentMethod.Efectivo && amountPaid < finalTotal)
                return SaleResult.Error($"El monto recibido (${amountPaid:N2}) es menor al total (${finalTotal:N2}).");

            decimal change = paymentMethod == PaymentMethod.Efectivo ? amountPaid - finalTotal : 0;

            var branch = await _branchRepository.GetByIdAsync(branchId);
            string branchName = branch?.Name ?? "Sucursal";
            string branchAddress = branch?.Address ?? "";
            string branchRazonSocial = branch?.RazonSocial ?? "";

            var (cajaId, _) = GetCajaInfo();
            string folio = await _folioService.GenerarFolioVentaAsync(branchId, cajaId);

            var ticketData = _ticketService.GenerateTicket(
                folio, branchId, branchName, branchAddress,
                string.Empty, branchRazonSocial,
                userId, userName, items, paymentMethod,
                amountPaid, change, generalDiscount,
                generalDiscountPercent, isGeneralDiscountPercentage);

            string ticketJson = _ticketService.SerializeTicket(ticketData);
            byte[] ticketCompressed = JsonCompressor.Compress(ticketData);

            try
            {
                var sale = new Sale
                {
                    Folio = folio,
                    Total = finalTotal,
                    Subtotal = total + totalDiscount,
                    Discount = totalDiscount + generalDiscount,
                    PaymentMethod = paymentMethod.ToString(),
                    AmountPaid = amountPaid,
                    ChangeGiven = change,
                    PaymentSummary = GetPaymentSummaryText(paymentMethod, amountPaid),
                    UserId = userId,
                    BranchId = branchId,
                    TicketData = ticketCompressed,
                    SyncStatus = 1,
                    SaleDate = DateTime.Now
                };

                Console.WriteLine($"[SalesService] Guardando venta con {items.Count} productos");
                var saleId = await _saleRepository.AddAsync(sale);
                Console.WriteLine($"[SalesService] Venta guardada con ID: {saleId}");

                int productCount = 0;
                foreach (var item in items)
                {
                    var saleProduct = new SaleProduct
                    {
                        SaleId = saleId,
                        ProductId = item.ProductId,
                        Barcode = item.Barcode,
                        ProductName = item.ProductName,
                        Quantity = item.Quantity,
                        ListPrice = item.ListPrice,
                        FinalUnitPrice = item.FinalUnitPrice,
                        LineTotal = item.LineTotal,
                        TotalDiscountAmount = item.TotalDiscount * item.Quantity,
                        PriceType = item.PriceType,
                        DiscountInfo = item.DiscountInfo,
                        PricingData = JsonCompressor.Compress(item.PricingData)
                    };

                    await _saleProductRepository.AddAsync(saleProduct);
                    productCount++;
                }

                Console.WriteLine($"[SalesService] Total de productos guardados: {productCount}");

                string ticketText = _ticketService.GenerateTicketText(ticketData);
                return SaleResult.Ok(sale, ticketData, ticketText);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[SalesService] ERROR en ProcessSaleAsync: {ex.Message}");
                Console.WriteLine($"[SalesService] StackTrace: {ex.StackTrace}");
                return SaleResult.Error($"Error al procesar la venta: {ex.Message}");
            }
        }

        public async Task<SaleResult> ProcessSaleWithMixedPaymentAsync(
            List<CartItem> items,
            string paymentJson,
            decimal totalPaid,
            decimal changeGiven,
            int userId,
            string userName,
            int branchId,
            decimal generalDiscount = 0,
            decimal generalDiscountPercent = 0,
            bool isGeneralDiscountPercentage = true)
        {
            if (items == null || items.Count == 0)
                return SaleResult.Error("El carrito esta vacio.");

            var stockValidation = await ValidateStockAsync(items);
            if (!stockValidation.IsValid)
                return SaleResult.Error(string.Join("\n", stockValidation.Errors));

            decimal total = 0;
            decimal totalDiscount = 0;

            foreach (var item in items)
            {
                total += item.LineTotal;
                totalDiscount += item.TotalDiscount * item.Quantity;
            }

            var finalTotal = total - generalDiscount;

            if (totalPaid < finalTotal)
                return SaleResult.Error($"El monto pagado (${totalPaid:N2}) es menor al total (${finalTotal:N2}).");

            var branch = await _branchRepository.GetByIdAsync(branchId);
            string branchName = branch?.Name ?? "Sucursal";
            string branchAddress = branch?.Address ?? "";
            string branchRazonSocial = branch?.RazonSocial ?? "";

            var (cajaId, _) = GetCajaInfo();
            string folio = await _folioService.GenerarFolioVentaAsync(branchId, cajaId);

            var ticketData = _ticketService.GenerateTicketWithMixedPayment(
                folio, branchId, branchName, branchAddress,
                string.Empty, branchRazonSocial,
                userId, userName, items, paymentJson,
                totalPaid, changeGiven, generalDiscount,
                generalDiscountPercent, isGeneralDiscountPercentage);

            byte[] ticketCompressed = JsonCompressor.Compress(ticketData);

            try
            {
                var sale = new Sale
                {
                    Folio = folio,
                    Total = finalTotal,
                    Subtotal = total + totalDiscount,
                    Discount = totalDiscount + generalDiscount,
                    PaymentMethod = paymentJson,
                    AmountPaid = totalPaid,
                    ChangeGiven = changeGiven,
                    PaymentSummary = GetMixedPaymentSummaryText(paymentJson),
                    UserId = userId,
                    BranchId = branchId,
                    TicketData = ticketCompressed,
                    SyncStatus = 1,
                    SaleDate = DateTime.Now
                };

                Console.WriteLine($"[SalesService.MixedPayment] Guardando venta con {items.Count} productos");
                var saleId = await _saleRepository.AddAsync(sale);
                Console.WriteLine($"[SalesService.MixedPayment] Venta guardada con ID: {saleId}");

                int productCount = 0;
                foreach (var item in items)
                {
                    var saleProduct = new SaleProduct
                    {
                        SaleId = saleId,
                        ProductId = item.ProductId,
                        Barcode = item.Barcode,
                        ProductName = item.ProductName,
                        Quantity = item.Quantity,
                        ListPrice = item.ListPrice,
                        FinalUnitPrice = item.FinalUnitPrice,
                        LineTotal = item.LineTotal,
                        TotalDiscountAmount = item.TotalDiscount * item.Quantity,
                        PriceType = item.PriceType,
                        DiscountInfo = item.DiscountInfo,
                        PricingData = JsonCompressor.Compress(item.PricingData)
                    };

                    await _saleProductRepository.AddAsync(saleProduct);
                    productCount++;
                }

                Console.WriteLine($"[SalesService.MixedPayment] Total de productos guardados: {productCount}");

                string ticketText = _ticketService.GenerateTicketText(ticketData);
                return SaleResult.Ok(sale, ticketData, ticketText);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[SalesService.MixedPayment] ERROR: {ex.Message}");
                Console.WriteLine($"[SalesService.MixedPayment] StackTrace: {ex.StackTrace}");
                return SaleResult.Error($"Error al procesar la venta: {ex.Message}");
            }
        }

        private string GetMixedPaymentSummaryText(string paymentJson)
        {
            try
            {
                var payments = System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, decimal>>(paymentJson);
                if (payments == null || payments.Count == 0)
                    return "Sin datos de pago";

                var parts = new List<string>();
                foreach (var kvp in payments)
                {
                    string methodName = kvp.Key switch
                    {
                        "efectivo" => "Efectivo",
                        "tarjeta_debito" => "T. Débito",
                        "tarjeta_credito" => "T. Crédito",
                        "transferencia" => "Transfer.",
                        _ => kvp.Key
                    };
                    parts.Add($"{methodName} ${kvp.Value:N2}");
                }
                return string.Join(" + ", parts);
            }
            catch
            {
                return paymentJson;
            }
        }

        public async Task<List<Product>> SearchProductsAsync(string searchTerm, int? categoryId = null)
        {
            var products = await _productRepository.GetAllAsync();
            var results = new List<Product>();

            foreach (var product in products)
            {
                if (!product.Active) continue;

                bool matchesSearch = true;
                bool matchesCategory = true;

                if (!string.IsNullOrWhiteSpace(searchTerm))
                {
                    matchesSearch =
                        (product.Barcode?.Contains(searchTerm, StringComparison.OrdinalIgnoreCase) ?? false) ||
                        (product.Name?.Contains(searchTerm, StringComparison.OrdinalIgnoreCase) ?? false);
                }

                if (categoryId.HasValue && categoryId > 0)
                    matchesCategory = product.CategoryId == categoryId;

                if (matchesSearch && matchesCategory)
                    results.Add(product);
            }

            return results;
        }

        public async Task<Product?> GetProductByCodeAsync(string barcode)
        {
            if (string.IsNullOrWhiteSpace(barcode)) return null;

            var products = await _productRepository.FindAsync(p =>
                p.Barcode == barcode && p.Active);

            return products.Count > 0 ? products[0] : null;
        }

        public async Task<CartItem?> CreateCartItemAsync(int productId, int quantity, int userId)
        {
            var product = await _productRepository.GetByIdAsync(productId);
            if (product == null) return null;

            Category? category = null;
            string categoryName = "";
            string unitName = "";

            if (product.CategoryId > 0)
            {
                category = await _categoryRepository.GetByIdAsync(product.CategoryId);
                categoryName = category?.Name ?? "";
            }

            if (product.UnitId > 0)
            {
                var unit = await _unitRepository.GetByIdAsync(product.UnitId);
                unitName = unit?.Name ?? "";
            }

            var priceCalc = _pricingService.CalculatePrice(product, quantity, category);

            string priceType = priceCalc.CategoryDiscountPercent > 0
                ? "category"
                : priceCalc.AppliedPriceType.ToString().ToLower();

            return new CartItem
            {
                ProductId = product.Id,
                Barcode = product.Barcode ?? "",
                ProductName = product.Name ?? "Sin nombre",
                CategoryName = categoryName,
                UnitName = unitName,
                Quantity = quantity,
                ListPrice = priceCalc.ListPrice,
                FinalUnitPrice = priceCalc.FinalPrice,
                TotalDiscount = priceCalc.TotalDiscount,
                PriceType = priceType,
                DiscountInfo = priceCalc.DiscountInfo
            };
        }

        public async Task<CartItem?> CreateCartItemWithPriceTypeAsync(
            int productId,
            int quantity,
            int userId,
            PriceType priceType)
        {
            var product = await _productRepository.GetByIdAsync(productId);
            if (product == null) return null;

            Category? category = null;
            string categoryName = "";
            string unitName = "";

            if (product.CategoryId > 0)
            {
                category = await _categoryRepository.GetByIdAsync(product.CategoryId);
                categoryName = category?.Name ?? "";
            }

            if (product.UnitId > 0)
            {
                var unit = await _unitRepository.GetByIdAsync(product.UnitId);
                unitName = unit?.Name ?? "";
            }

            var priceCalc = _pricingService.CalculatePrice(product, quantity, category, priceType);

            return new CartItem
            {
                ProductId = product.Id,
                Barcode = product.Barcode ?? "",
                ProductName = product.Name ?? "Sin nombre",
                CategoryName = categoryName,
                UnitName = unitName,
                Quantity = quantity,
                ListPrice = priceCalc.ListPrice,
                FinalUnitPrice = priceCalc.FinalPrice,
                TotalDiscount = priceCalc.TotalDiscount,
                PriceType = priceCalc.AppliedPriceType.ToString().ToLower(),
                DiscountInfo = priceCalc.DiscountInfo
            };
        }

        public async Task<(bool Success, string Message)> ApplySpecialPriceAsync(CartItem item)
        {
            var product = await _productRepository.GetByIdAsync(item.ProductId);
            if (product == null)
                return (false, "Producto no encontrado.");

            return _pricingService.ApplySpecialPrice(item, product);
        }

        public async Task<(bool Success, string Message)> ApplyDealerPriceAsync(CartItem item)
        {
            var product = await _productRepository.GetByIdAsync(item.ProductId);
            if (product == null)
                return (false, "Producto no encontrado.");

            return _pricingService.ApplyDealerPrice(item, product);
        }

        public async Task<(bool Success, string Message)> RevertToRetailPriceAsync(CartItem item)
        {
            var product = await _productRepository.GetByIdAsync(item.ProductId);
            if (product == null)
                return (false, "Producto no encontrado.");

            if (item.PriceType != "special" && item.PriceType != "dealer")
                return (false, "El producto no tiene un precio especial o vendedor aplicado.");

            var oldPrice = item.FinalUnitPrice;
            var oldPriceType = item.PriceType == "special" ? "especial" : "vendedor";

            Category? category = null;
            if (product.CategoryId > 0)
                category = await _categoryRepository.GetByIdAsync(product.CategoryId);

            var priceCalc = _pricingService.CalculatePrice(product, item.Quantity, category);

            string priceType = priceCalc.CategoryDiscountPercent > 0
                ? "category"
                : priceCalc.AppliedPriceType.ToString().ToLower();

            item.FinalUnitPrice = priceCalc.FinalPrice;
            item.TotalDiscount = priceCalc.TotalDiscount;
            item.PriceType = priceType;
            item.DiscountInfo = priceCalc.DiscountInfo;

            var newPrice = priceCalc.FinalPrice;
            var newPriceLabel = string.IsNullOrEmpty(priceCalc.DiscountInfo) ? "menudeo" : priceCalc.DiscountInfo;

            return (true, $"✓ Precio {oldPriceType} removido de \"{item.ProductName}\"\n\n" +
                $"Precio anterior: ${oldPrice:N2}\n" +
                $"Precio actual ({newPriceLabel}): ${newPrice:N2}");
        }

        public async Task<Product?> GetProductByIdAsync(int productId)
        {
            return await _productRepository.GetByIdAsync(productId);
        }

        public async Task<List<Sale>> GetDailySalesAsync(int branchId)
        {
            var today = DateTime.Today;
            return await _saleRepository.FindAsync(s =>
                s.BranchId == branchId &&
                s.CreatedAt >= today);
        }

        public async Task<decimal> GetDailySalesTotalAsync(int branchId)
        {
            var sales = await GetDailySalesAsync(branchId);
            decimal total = 0;
            foreach (var sale in sales)
                total += sale.Total;
            return total;
        }

        private string GetPaymentSummaryText(PaymentMethod paymentMethod, decimal amount)
        {
            return paymentMethod switch
            {
                PaymentMethod.Efectivo => $"Efectivo ${amount:N2}",
                PaymentMethod.TarjetaDebito => $"Tarjeta Débito ${amount:N2}",
                PaymentMethod.TarjetaCredito => $"Tarjeta Crédito ${amount:N2}",
                PaymentMethod.Transferencia => $"Transferencia ${amount:N2}",
                _ => $"${amount:N2}"
            };
        }

        public async Task<List<Category>> GetCategoriesAsync()
        {
            return await _categoryRepository.FindAsync(c => c.Active);
        }

        public async Task<List<Unit>> GetUnitsAsync()
        {
            return await _unitRepository.FindAsync(u => u.Active);
        }

        public async Task<List<Product>> SearchProductsWithUnitAsync(string searchTerm, int? categoryId = null, int? unitId = null)
        {
            var products = await _productRepository.GetAllAsync();
            var categories = await _categoryRepository.GetAllAsync();
            var units = await _unitRepository.GetAllAsync();

            var categoryDict = categories.ToDictionary(c => c.Id, c => c.Name);
            var unitDict = units.ToDictionary(u => u.Id, u => u.Name);

            var results = new List<Product>();

            foreach (var product in products)
            {
                if (!product.Active) continue;

                bool matchesSearch = true;
                bool matchesCategory = true;
                bool matchesUnit = true;

                if (!string.IsNullOrWhiteSpace(searchTerm))
                {
                    matchesSearch =
                        (product.Barcode?.Contains(searchTerm, StringComparison.OrdinalIgnoreCase) ?? false) ||
                        (product.Name?.Contains(searchTerm, StringComparison.OrdinalIgnoreCase) ?? false);
                }

                if (categoryId.HasValue && categoryId > 0)
                    matchesCategory = product.CategoryId == categoryId;

                if (unitId.HasValue && unitId > 0)
                    matchesUnit = product.UnitId == unitId;

                if (matchesSearch && matchesCategory && matchesUnit)
                {
                    product.CategoryName = categoryDict.TryGetValue(product.CategoryId, out var catName) ? catName : "";
                    product.UnitName = unitDict.TryGetValue(product.UnitId, out var unitName) ? unitName : "";
                    results.Add(product);
                }
            }

            return results;
        }

        public async Task<List<Sale>> GetSalesHistoryPagedAsync(
            int branchId,
            int page,
            int pageSize,
            DateTime? startDate = null,
            DateTime? endDate = null)
        {
            var sales = await _saleRepository.FindAsync(s => s.BranchId == branchId);

            if (startDate.HasValue)
                sales = sales.Where(s => s.SaleDate.Date >= startDate.Value.Date).ToList();
            if (endDate.HasValue)
                sales = sales.Where(s => s.SaleDate.Date <= endDate.Value.Date).ToList();

            return sales
                .OrderByDescending(s => s.SaleDate)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToList();
        }

        public async Task<int> GetSalesCountAsync(
            int branchId,
            DateTime? startDate = null,
            DateTime? endDate = null)
        {
            var sales = await _saleRepository.FindAsync(s => s.BranchId == branchId);

            if (startDate.HasValue)
                sales = sales.Where(s => s.SaleDate.Date >= startDate.Value.Date).ToList();
            if (endDate.HasValue)
                sales = sales.Where(s => s.SaleDate.Date <= endDate.Value.Date).ToList();

            return sales.Count;
        }

        public async Task<List<SaleProduct>> GetSaleProductsAsync(int saleId)
        {
            return await _saleProductRepository.FindAsync(sp => sp.SaleId == saleId);
        }

        public async Task<string?> GetUserNameAsync(int userId)
        {
            var user = await _userRepository.GetByIdAsync(userId);
            return user?.Name ?? $"Usuario #{userId}";
        }

        public async Task<TicketData?> RecoverTicketAsync(int saleId)
        {
            var sale = await _saleRepository.GetByIdAsync(saleId);
            if (sale?.TicketData == null) return null;

            return JsonCompressor.Decompress<TicketData>(sale.TicketData);
        }
    }
}