using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using CasaCejaRemake.Data;
using CasaCejaRemake.Data.Repositories;
using CasaCejaRemake.Helpers;
using CasaCejaRemake.Models;

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

    public class SalesService
    {
        private readonly DatabaseService _databaseService;
        private readonly BaseRepository<Sale> _saleRepository;
        private readonly BaseRepository<SaleProduct> _saleProductRepository;
        private readonly BaseRepository<Product> _productRepository;
        private readonly BaseRepository<Branch> _branchRepository;
        private readonly TicketService _ticketService;

        public SalesService(DatabaseService databaseService)
        {
            _databaseService = databaseService;
            _saleRepository = new BaseRepository<Sale>(databaseService);
            _saleProductRepository = new BaseRepository<SaleProduct>(databaseService);
            _productRepository = new BaseRepository<Product>(databaseService);
            _branchRepository = new BaseRepository<Branch>(databaseService);
            _ticketService = new TicketService();
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
                // if (product.Stock < item.Quantity)
                // {
                //     result.IsValid = false;
                //     result.Errors.Add($"Stock insuficiente para '{item.ProductName}'. " +
                //         $"Disponible: {product.Stock}, Solicitado: {item.Quantity}");
                // }
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

        public async Task<SaleResult> ProcessSaleAsync(
            List<CartItem> items,
            PaymentMethod paymentMethod,
            decimal amountPaid,
            int userId,
            string userName,
            int branchId)
        {
            // Validar que hay items
            if (items == null || items.Count == 0)
            {
                return SaleResult.Error("El carrito esta vacio.");
            }

            // Validar stock
            var stockValidation = await ValidateStockAsync(items);
            if (!stockValidation.IsValid)
            {
                return SaleResult.Error(string.Join("\n", stockValidation.Errors));
            }

            // Calcular totales
            decimal total = 0;
            decimal totalDiscount = 0;

            foreach (var item in items)
            {
                total += item.LineTotal;
                totalDiscount += item.TotalDiscount * item.Quantity;
            }

            // Validar pago
            if (paymentMethod == PaymentMethod.Efectivo && amountPaid < total)
            {
                return SaleResult.Error($"El monto recibido (${amountPaid:N2}) es menor al total (${total:N2}).");
            }

            // Calcular cambio
            decimal change = paymentMethod == PaymentMethod.Efectivo ? amountPaid - total : 0;

            // Obtener informacion de sucursal
            var branch = await _branchRepository.GetByIdAsync(branchId);
            string branchName = branch?.Name ?? "Sucursal";
            string branchAddress = branch?.Address ?? "";
            string branchPhone = branch?.Email ?? ""; // Branch no tiene Phone, usando Email

            // Generar folio
            int consecutivo = await GetNextConsecutiveAsync(branchId);
            string folio = _ticketService.GenerateFolio(branchId, consecutivo);

            // Generar ticket (INMUTABLE - se genera antes de guardar)
            var ticketData = _ticketService.GenerateTicket(
                folio,
                branchId,
                branchName,
                branchAddress,
                branchPhone,
                userId,
                userName,
                items,
                paymentMethod,
                amountPaid,
                change
            );

            // Serializar y comprimir ticket
            string ticketJson = _ticketService.SerializeTicket(ticketData);
            byte[] ticketCompressed = JsonCompressor.Compress(ticketData);

            try
            {
                // Crear venta
                var sale = new Sale
                {
                    Folio = folio,
                    Total = total,
                    Subtotal = total + totalDiscount,
                    Discount = totalDiscount,
                    PaymentMethod = paymentMethod.ToString(),
                    AmountPaid = amountPaid,
                    ChangeGiven = change,
                    PaymentSummary = GetPaymentSummaryText(paymentMethod, amountPaid),
                    UserId = userId,
                    BranchId = branchId,
                    TicketData = ticketCompressed,
                    SyncStatus = 1,
                    SaleDate = DateTime.Now
                    // CreatedAt lo asigna automáticamente BaseRepository.AddAsync()
                };

                // Guardar venta
                var saleId = await _saleRepository.AddAsync(sale);

                // Guardar productos de la venta
                foreach (var item in items)
                {
                    // Crear SaleProduct
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
                        TotalDiscountAmount = item.TotalDiscount,
                        PriceType = item.PriceType,
                        DiscountInfo = item.DiscountInfo,
                        PricingData = JsonCompressor.Compress(item.PricingData)
                        // CreatedAt lo asigna automáticamente BaseRepository.AddAsync()
                    };

                    await _saleProductRepository.AddAsync(saleProduct);

                    // TODO: Implement stock update from inventory table
                    // Update stock entries/outputs here
                }

                // Generar texto del ticket para impresion
                string ticketText = _ticketService.GenerateTicketText(ticketData);

                return SaleResult.Ok(sale, ticketData, ticketText);
            }
            catch (Exception ex)
            {
                return SaleResult.Error($"Error al procesar la venta: {ex.Message}");
            }
        }

        /// <summary>
        /// Procesa una venta con pagos mixtos (múltiples métodos de pago)
        /// </summary>
        public async Task<SaleResult> ProcessSaleWithMixedPaymentAsync(
            List<CartItem> items,
            string paymentJson,
            decimal totalPaid,
            decimal changeGiven,
            int userId,
            string userName,
            int branchId)
        {
            // Validar que hay items
            if (items == null || items.Count == 0)
            {
                return SaleResult.Error("El carrito esta vacio.");
            }

            // Validar stock
            var stockValidation = await ValidateStockAsync(items);
            if (!stockValidation.IsValid)
            {
                return SaleResult.Error(string.Join("\n", stockValidation.Errors));
            }

            // Calcular totales
            decimal total = 0;
            decimal totalDiscount = 0;

            foreach (var item in items)
            {
                total += item.LineTotal;
                totalDiscount += item.TotalDiscount * item.Quantity;
            }

            // Validar que el pago cubre el total
            if (totalPaid < total)
            {
                return SaleResult.Error($"El monto pagado (${totalPaid:N2}) es menor al total (${total:N2}).");
            }

            // Obtener informacion de sucursal
            var branch = await _branchRepository.GetByIdAsync(branchId);
            string branchName = branch?.Name ?? "Sucursal";
            string branchAddress = branch?.Address ?? "";
            string branchPhone = branch?.Email ?? "";

            // Generar folio
            int consecutivo = await GetNextConsecutiveAsync(branchId);
            string folio = _ticketService.GenerateFolio(branchId, consecutivo);

            // Generar ticket con pagos mixtos
            var ticketData = _ticketService.GenerateTicketWithMixedPayment(
                folio,
                branchId,
                branchName,
                branchAddress,
                branchPhone,
                userId,
                userName,
                items,
                paymentJson,
                totalPaid,
                changeGiven
            );

            // Serializar y comprimir ticket
            byte[] ticketCompressed = JsonCompressor.Compress(ticketData);

            try
            {
                // Crear venta - PaymentMethod ahora es el JSON de pagos
                var sale = new Sale
                {
                    Folio = folio,
                    Total = total,
                    Subtotal = total + totalDiscount,
                    Discount = totalDiscount,
                    PaymentMethod = paymentJson, // JSON: {"efectivo": 500, "tarjeta_debito": 300}
                    AmountPaid = totalPaid,
                    ChangeGiven = changeGiven,
                    PaymentSummary = GetMixedPaymentSummaryText(paymentJson),
                    UserId = userId,
                    BranchId = branchId,
                    TicketData = ticketCompressed,
                    SyncStatus = 1,
                    SaleDate = DateTime.Now
                };

                // Guardar venta
                var saleId = await _saleRepository.AddAsync(sale);

                // Guardar productos de la venta
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
                        TotalDiscountAmount = item.TotalDiscount,
                        PriceType = item.PriceType,
                        DiscountInfo = item.DiscountInfo,
                        PricingData = JsonCompressor.Compress(item.PricingData)
                    };

                    await _saleProductRepository.AddAsync(saleProduct);
                }

                // Generar texto del ticket para impresion
                string ticketText = _ticketService.GenerateTicketText(ticketData);

                return SaleResult.Ok(sale, ticketData, ticketText);
            }
            catch (Exception ex)
            {
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
                {
                    matchesCategory = product.CategoryId == categoryId;
                }

                if (matchesSearch && matchesCategory)
                {
                    results.Add(product);
                }
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

            // Obtener categoria y unidad para el item
            string categoryName = "";
            string unitName = "";

            if (product.CategoryId > 0)
            {
                var categoryRepo = new BaseRepository<Category>(_databaseService);
                var category = await categoryRepo.GetByIdAsync(product.CategoryId);
                categoryName = category?.Name ?? "";
            }

            if (product.UnitId > 0)
            {
                var unitRepo = new BaseRepository<Unit>(_databaseService);
                var unit = await unitRepo.GetByIdAsync(product.UnitId);
                unitName = unit?.Name ?? "";
            }

            // Calcular precio segun reglas de negocio
            decimal listPrice = product.PriceRetail;
            decimal finalPrice = listPrice;
            decimal discount = 0;
            string priceType = "retail";
            string discountInfo = "";

            // Verificar precio mayoreo (solo aplica si cumple cantidad mínima)
            if (product.QualifiesForWholesale(quantity))
            {
                finalPrice = product.PriceWholesale;
                priceType = "wholesale";
                discountInfo = $"Precio mayoreo ({product.WholesaleQuantity}+ piezas)";
            }

            // NOTA: El precio especial NO se aplica automáticamente.
            // Debe ser activado manualmente por el usuario o por promociones específicas.
            // El precio especial está disponible en product.PriceSpecial si se necesita.

            // Si no es precio especial, calcular descuento si aplica
            if (priceType != "special")
            {
                discount = listPrice - finalPrice;
            }

            return new CartItem
            {
                ProductId = product.Id,
                Barcode = product.Barcode ?? "",
                ProductName = product.Name ?? "Sin nombre",
                CategoryName = categoryName,
                UnitName = unitName,
                Quantity = quantity,
                ListPrice = listPrice,
                FinalUnitPrice = finalPrice,
                TotalDiscount = discount,
                PriceType = priceType,
                DiscountInfo = discountInfo
            };
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
            {
                total += sale.Total;
            }
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
            var categoryRepo = new BaseRepository<Category>(_databaseService);
            return await categoryRepo.FindAsync(c => c.Active);
        }

        public async Task<List<Unit>> GetUnitsAsync()
        {
            var unitRepo = new BaseRepository<Unit>(_databaseService);
            return await unitRepo.FindAsync(u => u.Active);
        }

        public async Task<List<Product>> SearchProductsWithUnitAsync(string searchTerm, int? categoryId = null, int? unitId = null)
        {
            var products = await _productRepository.GetAllAsync();
            var results = new List<Product>();

            // Cargar categorías y unidades para nombres
            var categoryRepo = new BaseRepository<Category>(_databaseService);
            var unitRepo = new BaseRepository<Unit>(_databaseService);
            var categories = await categoryRepo.GetAllAsync();
            var units = await unitRepo.GetAllAsync();

            var categoryDict = new Dictionary<int, string>();
            foreach (var c in categories)
            {
                categoryDict[c.Id] = c.Name;
            }

            var unitDict = new Dictionary<int, string>();
            foreach (var u in units)
            {
                unitDict[u.Id] = u.Name;
            }

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
                {
                    matchesCategory = product.CategoryId == categoryId;
                }

                if (unitId.HasValue && unitId > 0)
                {
                    matchesUnit = product.UnitId == unitId;
                }

                if (matchesSearch && matchesCategory && matchesUnit)
                {
                    // Asignar nombres de categoría y unidad
                    product.CategoryName = categoryDict.TryGetValue(product.CategoryId, out var catName) ? catName : "";
                    product.UnitName = unitDict.TryGetValue(product.UnitId, out var unitName) ? unitName : "";
                    results.Add(product);
                }
            }

            return results;
        }

        public async Task<TicketData?> RecoverTicketAsync(int saleId)
        {
            var sale = await _saleRepository.GetByIdAsync(saleId);
            if (sale?.TicketData == null) return null;

            return JsonCompressor.Decompress<TicketData>(sale.TicketData);
        }
    }
}