using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using CasaCejaRemake.Helpers;
using CasaCejaRemake.Models;

namespace CasaCejaRemake.Services
{
    public enum TicketType
    {
        Sale = 1,
        Credit = 2,
        Layaway = 3
    }

    public enum PrinterType
    {
        Thermal = 1,
        Normal = 2
    }

    public class TicketData
    {
        // Informacion del negocio
        [JsonPropertyName("b")]
        public TicketBranch Branch { get; set; } = new();

        // Informacion de la venta
        [JsonPropertyName("s")]
        public TicketSale Sale { get; set; } = new();

        // Productos vendidos
        [JsonPropertyName("p")]
        public List<TicketProduct> Products { get; set; } = new();

        // Totales
        [JsonPropertyName("t")]
        public TicketTotals Totals { get; set; } = new();

        // Pago
        [JsonPropertyName("y")]
        public TicketPayment Payment { get; set; } = new();

        // Metadata
        [JsonPropertyName("m")]
        public TicketMetadata Meta { get; set; } = new();

        // Datos del cliente (para créditos/apartados)
        [JsonPropertyName("cn")]
        public string CustomerName { get; set; } = string.Empty;

        [JsonPropertyName("cp")]
        public string CustomerPhone { get; set; } = string.Empty;

        // Fecha de vencimiento (para créditos/apartados)
        [JsonPropertyName("dd")]
        public DateTime? DueDate { get; set; }

        // Etiqueta de tipo de ticket ("TICKET DE CREDITO", "TICKET DE APARTADO")
        [JsonPropertyName("ttl")]
        public string TicketTypeLabel { get; set; } = string.Empty;
    }

    public class TicketBranch
    {
        [JsonPropertyName("id")]
        public int Id { get; set; }

        [JsonPropertyName("n")]
        public string Name { get; set; } = string.Empty;

        [JsonPropertyName("a")]
        public string Address { get; set; } = string.Empty;

        [JsonPropertyName("ph")]
        public string Phone { get; set; } = string.Empty;

        [JsonPropertyName("rs")]
        public string RazonSocial { get; set; } = string.Empty;
    }

    public class TicketSale
    {
        [JsonPropertyName("f")]
        public string Folio { get; set; } = string.Empty;

        [JsonPropertyName("dt")]
        public DateTime DateTime { get; set; }

        [JsonPropertyName("uid")]
        public int UserId { get; set; }

        [JsonPropertyName("un")]
        public string UserName { get; set; } = string.Empty;
    }

    public class TicketProduct
    {
        [JsonPropertyName("id")]
        public int ProductId { get; set; }

        [JsonPropertyName("bc")]
        public string Barcode { get; set; } = string.Empty;

        [JsonPropertyName("n")]
        public string Name { get; set; } = string.Empty;

        [JsonPropertyName("q")]
        public int Quantity { get; set; }

        [JsonPropertyName("lp")]
        public decimal ListPrice { get; set; }

        [JsonPropertyName("up")]
        public decimal UnitPrice { get; set; }

        [JsonPropertyName("lt")]
        public decimal LineTotal { get; set; }

        [JsonPropertyName("d")]
        public decimal Discount { get; set; }

        [JsonPropertyName("pt")]
        public string PriceType { get; set; } = "retail";

        [JsonPropertyName("di")]
        public string DiscountInfo { get; set; } = string.Empty;

        // Indicadores de descuento para el ticket (sección 13 del doc de formatos)
        [JsonPropertyName("isp")]
        public bool IsSpecialPrice { get; set; }

        [JsonPropertyName("hcd")]
        public bool HasCategoryDiscount { get; set; }

        [JsonPropertyName("cdp")]
        public decimal CategoryDiscountPercent { get; set; }

        [JsonPropertyName("cda")]
        public decimal CategoryDiscountAmount { get; set; }
    }

    public class TicketTotals
    {
        [JsonPropertyName("st")]
        public decimal Subtotal { get; set; }

        [JsonPropertyName("td")]
        public decimal TotalDiscount { get; set; }

        [JsonPropertyName("tx")]
        public decimal Tax { get; set; }

        [JsonPropertyName("gt")]
        public decimal GrandTotal { get; set; }

        [JsonPropertyName("ic")]
        public int ItemCount { get; set; }
        
        // Descuento general
        [JsonPropertyName("gd")]
        public decimal GeneralDiscount { get; set; }
        
        [JsonPropertyName("gdp")]
        public decimal GeneralDiscountPercent { get; set; }
        
        [JsonPropertyName("gdi")]
        public bool IsGeneralDiscountPercentage { get; set; }
    }

    public class TicketPayment
    {
        [JsonPropertyName("m")]
        public int Method { get; set; }

        [JsonPropertyName("mn")]
        public string MethodName { get; set; } = string.Empty;

        [JsonPropertyName("a")]
        public decimal Amount { get; set; }

        [JsonPropertyName("c")]
        public decimal Change { get; set; }

        [JsonPropertyName("pd")]
        public string PaymentDetails { get; set; } = string.Empty; // JSON con desglose de pagos mixtos
    }

    public class TicketMetadata
    {
        [JsonPropertyName("v")]
        public string Version { get; set; } = "1.0";

        [JsonPropertyName("gen")]
        public DateTime GeneratedAt { get; set; }

        [JsonPropertyName("app")]
        public string AppVersion { get; set; } = "1.0.0";
    }

    /// <summary>
    /// DTO para tickets de abono a crédito/apartado (sección 8 del doc de formatos)
    /// </summary>
    public class PaymentTicketData
    {
        public string Folio { get; set; } = string.Empty;
        public string BranchName { get; set; } = string.Empty;
        public string BranchAddress { get; set; } = string.Empty;
        public string BranchRazonSocial { get; set; } = string.Empty;
        public DateTime PaymentDate { get; set; }
        public string UserName { get; set; } = string.Empty;
        public string CustomerName { get; set; } = string.Empty;
        /// <summary>Folio del crédito/apartado al que se abona</summary>
        public string OperationFolio { get; set; } = string.Empty;
        /// <summary>0=crédito, 1=apartado</summary>
        public int OperationType { get; set; }
        /// <summary>JSON con desglose de pagos mixtos</summary>
        public string PaymentDetails { get; set; } = string.Empty;
        public decimal TotalPaid { get; set; }
        public decimal RemainingBalance { get; set; }
    }

    public class TicketService
    {
        public string GenerateFolio(int branchId, int consecutivo)
        {
            var now = DateTime.Now;
            return $"{now:MMddyyyy}{branchId:D2}{consecutivo:D4}";
        }

        public string GenerateCreditFolio(int branchId, int consecutivo)
        {
            var now = DateTime.Now;
            return $"CRED-S{branchId}-{now:yyyyMMdd}-{consecutivo:D3}";
        }

        public string GenerateLayawayFolio(int branchId, int consecutivo)
        {
            var now = DateTime.Now;
            return $"APAR-S{branchId}-{now:yyyyMMdd}-{consecutivo:D3}";
        }

        public string GetPaymentMethodName(PaymentMethod method)
        {
            return method switch
            {
                PaymentMethod.Efectivo => "Efectivo",
                PaymentMethod.TarjetaDebito => "Tarjeta de Debito",
                PaymentMethod.TarjetaCredito => "Tarjeta de Credito",
                PaymentMethod.Transferencia => "Transferencia",
                _ => "Desconocido"
            };
        }

        public TicketData GenerateTicket(
            string folio,
            int branchId,
            string branchName,
            string branchAddress,
            string branchPhone,
            string branchRazonSocial,
            int userId,
            string userName,
            List<CartItem> items,
            PaymentMethod paymentMethod,
            decimal amountPaid,
            decimal change,
            decimal generalDiscount = 0,
            decimal generalDiscountPercent = 0,
            bool isGeneralDiscountPercentage = true)
        {
            var now = DateTime.Now;

            var ticketProducts = new List<TicketProduct>();
            decimal subtotal = 0;
            decimal totalDiscount = 0;
            int itemCount = 0;

            foreach (var item in items)
            {
                ticketProducts.Add(new TicketProduct
                {
                    ProductId = item.ProductId,
                    Barcode = item.Barcode,
                    Name = item.ProductName,
                    Quantity = item.Quantity,
                    ListPrice = item.ListPrice,
                    UnitPrice = item.FinalUnitPrice,
                    LineTotal = item.LineTotal,
                    Discount = item.TotalDiscount * item.Quantity,
                    PriceType = item.PriceType,
                    DiscountInfo = item.DiscountInfo,
                    IsSpecialPrice = item.PriceType == "special",
                    HasCategoryDiscount = item.PriceType == "category" || ExtractCategoryDiscount(item.DiscountInfo).HasValue,
                    CategoryDiscountPercent = ExtractCategoryDiscount(item.DiscountInfo) ?? 0,
                    CategoryDiscountAmount = ExtractCategoryDiscountAmount(item)
                });

                subtotal += item.LineTotal;
                totalDiscount += item.TotalDiscount * item.Quantity;
                itemCount += item.Quantity;
            }

            return new TicketData
            {
                Branch = new TicketBranch
                {
                    Id = branchId,
                    Name = branchName,
                    Address = branchAddress,
                    Phone = branchPhone,
                    RazonSocial = branchRazonSocial
                },
                Sale = new TicketSale
                {
                    Folio = folio,
                    DateTime = now,
                    UserId = userId,
                    UserName = userName
                },
                Products = ticketProducts,
                Totals = new TicketTotals
                {
                    Subtotal = subtotal + totalDiscount,
                    TotalDiscount = totalDiscount,
                    Tax = 0,
                    GrandTotal = subtotal - generalDiscount,
                    ItemCount = itemCount,
                    GeneralDiscount = generalDiscount,
                    GeneralDiscountPercent = generalDiscountPercent,
                    IsGeneralDiscountPercentage = isGeneralDiscountPercentage
                },
                Payment = new TicketPayment
                {
                    Method = (int)paymentMethod,
                    MethodName = GetPaymentMethodName(paymentMethod),
                    Amount = amountPaid,
                    Change = change
                },
                Meta = new TicketMetadata
                {
                    Version = "1.0",
                    GeneratedAt = now,
                    AppVersion = "1.0.0"
                }
            };
        }

        /// <summary>
        /// Genera ticket con pagos mixtos (múltiples métodos de pago)
        /// </summary>
        public TicketData GenerateTicketWithMixedPayment(
            string folio,
            int branchId,
            string branchName,
            string branchAddress,
            string branchPhone,
            string branchRazonSocial,
            int userId,
            string userName,
            List<CartItem> items,
            string paymentJson,
            decimal totalPaid,
            decimal change,
            decimal generalDiscount = 0,
            decimal generalDiscountPercent = 0,
            bool isGeneralDiscountPercentage = true)
        {
            var now = DateTime.Now;

            var ticketProducts = new List<TicketProduct>();
            decimal subtotal = 0;
            decimal totalDiscount = 0;
            int itemCount = 0;

            foreach (var item in items)
            {
                ticketProducts.Add(new TicketProduct
                {
                    ProductId = item.ProductId,
                    Barcode = item.Barcode,
                    Name = item.ProductName,
                    Quantity = item.Quantity,
                    ListPrice = item.ListPrice,
                    UnitPrice = item.FinalUnitPrice,
                    LineTotal = item.LineTotal,
                    Discount = item.TotalDiscount * item.Quantity,
                    PriceType = item.PriceType,
                    DiscountInfo = item.DiscountInfo,
                    IsSpecialPrice = item.PriceType == "special",
                    HasCategoryDiscount = item.PriceType == "category" || ExtractCategoryDiscount(item.DiscountInfo).HasValue,
                    CategoryDiscountPercent = ExtractCategoryDiscount(item.DiscountInfo) ?? 0,
                    CategoryDiscountAmount = ExtractCategoryDiscountAmount(item)
                });

                subtotal += item.LineTotal;
                totalDiscount += item.TotalDiscount * item.Quantity;
                itemCount += item.Quantity;
            }

            // Parsear el nombre del método de pago para mostrar
            string paymentMethodName = "Mixto";
            try
            {
                var payments = JsonSerializer.Deserialize<Dictionary<string, decimal>>(paymentJson);
                if (payments != null && payments.Count == 1)
                {
                    foreach (var key in payments.Keys)
                    {
                        paymentMethodName = key switch
                        {
                            "efectivo" => "Efectivo",
                            "tarjeta_debito" => "Tarjeta Débito",
                            "tarjeta_credito" => "Tarjeta Crédito",
                            "transferencia" => "Transferencia",
                            _ => key
                        };
                        break;
                    }
                }
                else if (payments != null && payments.Count > 1)
                {
                    paymentMethodName = "Pago Mixto";
                }
            }
            catch { /* usar default */ }

            return new TicketData
            {
                Branch = new TicketBranch
                {
                    Id = branchId,
                    Name = branchName,
                    Address = branchAddress,
                    Phone = branchPhone,
                    RazonSocial = branchRazonSocial
                },
                Sale = new TicketSale
                {
                    Folio = folio,
                    DateTime = now,
                    UserId = userId,
                    UserName = userName
                },
                Products = ticketProducts,
                Totals = new TicketTotals
                {
                    Subtotal = subtotal + totalDiscount,
                    TotalDiscount = totalDiscount,
                    Tax = 0,
                    GrandTotal = subtotal - generalDiscount,
                    ItemCount = itemCount,
                    GeneralDiscount = generalDiscount,
                    GeneralDiscountPercent = generalDiscountPercent,
                    IsGeneralDiscountPercentage = isGeneralDiscountPercentage
                },
                Payment = new TicketPayment
                {
                    Method = 0,
                    MethodName = paymentMethodName,
                    Amount = totalPaid,
                    Change = change,
                    PaymentDetails = paymentJson
                },
                Meta = new TicketMetadata
                {
                    Version = "1.0",
                    GeneratedAt = now,
                    AppVersion = "1.0.0"
                }
            };
        }

        public TicketData GenerateCreditTicket(
            string folio,
            string branchName,
            string branchAddress,
            string branchPhone,
            string branchRazonSocial,
            string userName,
            string customerName,
            string customerPhone,
            List<CartItem> items,
            decimal total,
            decimal initialPayment,
            decimal remainingBalance,
            DateTime dueDate,
            int monthsToPay,
            PaymentMethod paymentMethod)
        {
            var now = DateTime.Now;

            return new TicketData
            {
                Branch = new TicketBranch
                {
                    Name = branchName,
                    Address = branchAddress,
                    Phone = branchPhone,
                    RazonSocial = branchRazonSocial
                },
                Sale = new TicketSale
                {
                    Folio = folio,
                    DateTime = now,
                    UserName = userName
                },
                CustomerName = customerName,
                CustomerPhone = customerPhone,
                DueDate = dueDate,
                TicketTypeLabel = "TICKET DE CREDITO",
                Products = items.Select(i => new TicketProduct
                {
                    Barcode = i.Barcode,
                    Name = i.ProductName,
                    Quantity = i.Quantity,
                    ListPrice = i.ListPrice,
                    UnitPrice = i.FinalUnitPrice,
                    LineTotal = i.LineTotal,
                    Discount = i.TotalDiscount * i.Quantity,
                    PriceType = i.PriceType,
                    DiscountInfo = i.DiscountInfo,
                    IsSpecialPrice = i.PriceType == "special",
                    HasCategoryDiscount = i.PriceType == "category" || ExtractCategoryDiscount(i.DiscountInfo).HasValue,
                    CategoryDiscountPercent = ExtractCategoryDiscount(i.DiscountInfo) ?? 0,
                    CategoryDiscountAmount = ExtractCategoryDiscountAmount(i)
                }).ToList(),
                Totals = new TicketTotals
                {
                    Subtotal = items.Sum(i => i.ListPrice * i.Quantity),
                    TotalDiscount = items.Sum(i => i.TotalDiscount * i.Quantity),
                    GrandTotal = total,
                    ItemCount = items.Sum(i => i.Quantity)
                },
                Payment = new TicketPayment
                {
                    Method = (int)paymentMethod,
                    MethodName = GetPaymentMethodName(paymentMethod),
                    Amount = initialPayment
                },
                Meta = new TicketMetadata
                {
                    Version = "1.0",
                    GeneratedAt = now,
                    AppVersion = "1.0.0"
                }
            };
        }

        public TicketData GenerateLayawayTicket(
            string folio,
            string branchName,
            string branchAddress,
            string branchPhone,
            string branchRazonSocial,
            string userName,
            string customerName,
            string customerPhone,
            List<CartItem> items,
            decimal total,
            decimal initialPayment,
            decimal remainingBalance,
            DateTime pickupDate,
            int daysToPickup,
            PaymentMethod paymentMethod)
        {
            var now = DateTime.Now;

            return new TicketData
            {
                Branch = new TicketBranch
                {
                    Name = branchName,
                    Address = branchAddress,
                    Phone = branchPhone,
                    RazonSocial = branchRazonSocial
                },
                Sale = new TicketSale
                {
                    Folio = folio,
                    DateTime = now,
                    UserName = userName
                },
                CustomerName = customerName,
                CustomerPhone = customerPhone,
                DueDate = pickupDate,
                TicketTypeLabel = "TICKET DE APARTADO",
                Products = items.Select(i => new TicketProduct
                {
                    Barcode = i.Barcode,
                    Name = i.ProductName,
                    Quantity = i.Quantity,
                    ListPrice = i.ListPrice,
                    UnitPrice = i.FinalUnitPrice,
                    LineTotal = i.LineTotal,
                    Discount = i.TotalDiscount * i.Quantity,
                    PriceType = i.PriceType,
                    DiscountInfo = i.DiscountInfo,
                    IsSpecialPrice = i.PriceType == "special",
                    HasCategoryDiscount = i.PriceType == "category" || ExtractCategoryDiscount(i.DiscountInfo).HasValue,
                    CategoryDiscountPercent = ExtractCategoryDiscount(i.DiscountInfo) ?? 0,
                    CategoryDiscountAmount = ExtractCategoryDiscountAmount(i)
                }).ToList(),
                Totals = new TicketTotals
                {
                    Subtotal = items.Sum(i => i.ListPrice * i.Quantity),
                    TotalDiscount = items.Sum(i => i.TotalDiscount * i.Quantity),
                    GrandTotal = total,
                    ItemCount = items.Sum(i => i.Quantity)
                },
                Payment = new TicketPayment
                {
                    Method = (int)paymentMethod,
                    MethodName = GetPaymentMethodName(paymentMethod),
                    Amount = initialPayment
                },
                Meta = new TicketMetadata
                {
                    Version = "1.0",
                    GeneratedAt = now,
                    AppVersion = "1.0.0"
                }
            };
        }

        public string SerializeTicket(TicketData ticket)
        {
            var options = new JsonSerializerOptions
            {
                WriteIndented = false,
                DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
            };
            return JsonSerializer.Serialize(ticket, options);
        }

        public TicketData? DeserializeTicket(string json)
        {
            try
            {
                return JsonSerializer.Deserialize<TicketData>(json);
            }
            catch
            {
                return null;
            }
        }

        // =====================================================================
        // GENERACIÓN DE TEXTO — Delega a ThermalTicketTemplates
        // =====================================================================

        public string GenerateTicketText(TicketData ticket, int lineWidth = 32)
        {
            return GenerateTicketText(ticket, TicketType.Sale, lineWidth);
        }

        public string GenerateTicketText(TicketData ticket, TicketType type, int lineWidth = 32)
        {
            // Auto-load rfc, ticketFooter and lineWidth from config
            string rfc = ticket.Branch?.RazonSocial ?? string.Empty;
            string ticketFooter = string.Empty;
            try
            {
                var app = Avalonia.Application.Current as App;
                var config = app?.GetConfigService()?.PosTerminalConfig;
                if (config != null)
                {
                    if (!string.IsNullOrWhiteSpace(config.Rfc))
                        rfc = config.Rfc;
                    ticketFooter = config.TicketFooter ?? string.Empty;
                    if (lineWidth == 32 && config.TicketLineWidth > 0)
                        lineWidth = config.TicketLineWidth;
                }
            }
            catch { /* ignore */ }

            return GenerateTicketText(ticket, type, rfc, ticketFooter, lineWidth);
        }

        public string GenerateTicketText(TicketData ticket, TicketType type, string rfc, string ticketFooter, int lineWidth = 32)
        {
            return type switch
            {
                TicketType.Credit => ThermalTicketTemplates.FormatCreditTicket(ticket, rfc, ticketFooter, lineWidth),
                TicketType.Layaway => ThermalTicketTemplates.FormatLayawayTicket(ticket, rfc, ticketFooter, lineWidth),
                _ => ThermalTicketTemplates.FormatSaleTicket(ticket, rfc, ticketFooter, lineWidth)
            };
        }

        /// <summary>
        /// Genera texto de ticket de abono
        /// </summary>
        public string GeneratePaymentTicketText(PaymentTicketData data, string rfc = "", int lineWidth = 32)
        {
            // Auto-load rfc, ticketFooter and lineWidth from config (same as other ticket types)
            string effectiveRfc = rfc;
            string ticketFooter = string.Empty;
            try
            {
                var app = Avalonia.Application.Current as App;
                var config = app?.GetConfigService()?.PosTerminalConfig;
                if (config != null)
                {
                    ticketFooter = config.TicketFooter ?? string.Empty;
                    if (string.IsNullOrWhiteSpace(effectiveRfc) && !string.IsNullOrWhiteSpace(config.Rfc))
                        effectiveRfc = config.Rfc;
                    if (lineWidth == 32 && config.TicketLineWidth > 0)
                        lineWidth = config.TicketLineWidth;
                }
            }
            catch { /* ignore */ }

            // Fallback to BranchRazonSocial if rfc still empty
            if (string.IsNullOrWhiteSpace(effectiveRfc))
                effectiveRfc = data.BranchRazonSocial ?? string.Empty;

            return ThermalTicketTemplates.FormatPaymentTicket(data, effectiveRfc, ticketFooter, lineWidth);
        }

        /// <summary>
        /// Genera texto de reimpresión con historial de pagos (crédito o apartado)
        /// </summary>
        public string GenerateReprintWithHistoryText(
            TicketData ticket,
            TicketType type,
            List<string>? paymentDetailsJsonList,
            decimal totalPaid,
            string rfc = "",
            int lineWidth = 32)
        {
            string typeLabel = type == TicketType.Credit ? "TICKET DE CREDITO" : "TICKET DE APARTADO";
            return ThermalTicketTemplates.FormatReprintWithHistory(ticket, typeLabel, paymentDetailsJsonList, totalPaid, rfc, lineWidth);
        }

        public string GenerateHistoryTicketText(
            TicketData ticket,
            TicketType type,
            List<ViewModels.POS.PaymentHistoryItem> paymentHistory,
            decimal totalPaid,
            string rfc = "",
            int lineWidth = 32)
        {
            // Auto-load config
            string effectiveRfc = rfc;
            try
            {
                var app = Avalonia.Application.Current as App;
                var config = app?.GetConfigService()?.PosTerminalConfig;
                if (config != null)
                {
                    if (string.IsNullOrWhiteSpace(effectiveRfc) && !string.IsNullOrWhiteSpace(config.Rfc))
                        effectiveRfc = config.Rfc;
                    if (lineWidth == 32 && config.TicketLineWidth > 0)
                        lineWidth = config.TicketLineWidth;
                }
            }
            catch { /* ignore */ }

            if (string.IsNullOrWhiteSpace(effectiveRfc))
                effectiveRfc = ticket.Branch.RazonSocial ?? string.Empty;

            string typeLabel = type == TicketType.Credit ? "TICKET DE CREDITO" : "TICKET DE APARTADO";
            return ThermalTicketTemplates.FormatHistoryTicket(ticket, typeLabel, paymentHistory, totalPaid, effectiveRfc, lineWidth);
        }

        /// <summary>
        /// Genera el texto del ticket para corte de caja.
        /// Mantiene la firma existente para compatibilidad con CashCloseView.
        /// </summary>
        public string GenerateCashCloseTicketText(
            string branchName,
            string branchAddress,
            string branchPhone,
            string folio,
            string userName,
            DateTime openingDate,
            DateTime closeDate,
            decimal openingCash,
            decimal totalCash,
            decimal totalDebit,
            decimal totalCredit,
            decimal totalTransfer,
            decimal totalChecks,
            decimal layawayCash,
            decimal creditCash,
            decimal creditTotalCreated,
            decimal layawayTotalCreated,
            decimal totalExpenses,
            decimal totalIncome,
            decimal expectedCash,
            decimal declaredAmount,
            decimal difference,
            int salesCount,
            List<(string Concept, decimal Amount)>? expenses = null,
            List<(string Concept, decimal Amount)>? incomes = null,
            int lineWidth = 32)
        {
            // Auto-load lineWidth from config
            try
            {
                var app = Avalonia.Application.Current as App;
                var config = app?.GetConfigService()?.PosTerminalConfig;
                if (config != null && lineWidth == 32 && config.TicketLineWidth > 0)
                    lineWidth = config.TicketLineWidth;
            }
            catch { /* ignore */ }

            return ThermalTicketTemplates.FormatCashCloseTicket(
                branchName, branchAddress, folio, userName,
                openingDate, closeDate,
                openingCash, totalCash, totalDebit, totalCredit,
                totalTransfer, totalChecks, layawayCash, creditCash,
                creditTotalCreated, layawayTotalCreated,
                totalExpenses, totalIncome, expectedCash, declaredAmount, difference,
                salesCount, expenses, incomes, lineWidth);
        }

        // =====================================================================
        // HELPERS
        // =====================================================================

        /// <summary>
        /// Extrae el porcentaje de descuento de categoría del string DiscountInfo.
        /// Busca patrones como "10% desc." o "Cat 15%".
        /// </summary>
        private static decimal? ExtractCategoryDiscount(string discountInfo)
        {
            if (string.IsNullOrEmpty(discountInfo)) return null;
            // Buscar "X% desc." en el DiscountInfo
            var match = Regex.Match(discountInfo, @"(\d+(?:\.\d+)?)%\s*desc\.");
            if (match.Success && decimal.TryParse(match.Groups[1].Value, out decimal percent))
                return percent;
            return null;
        }

        /// <summary>
        /// Calcula el monto de descuento por categoría por unidad a partir del CartItem.
        /// Si el PriceType es "category" o el DiscountInfo contiene un porcentaje,
        /// calcula: ListPrice * (percent / 100).
        /// </summary>
        private static decimal ExtractCategoryDiscountAmount(CartItem item)
        {
            if (string.IsNullOrEmpty(item.DiscountInfo)) return 0;
            var percent = ExtractCategoryDiscount(item.DiscountInfo);
            if (percent.HasValue && percent.Value > 0)
            {
                // El descuento de categoría se calcula sobre el precio base
                return item.ListPrice * (percent.Value / 100m);
            }
            return 0;
        }
    }
}