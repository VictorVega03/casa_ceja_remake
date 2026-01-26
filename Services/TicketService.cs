using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
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
            return $"CRED-{now:MMddyyyy}-{consecutivo:D3}";
        }

        public string GenerateLayawayFolio(int branchId, int consecutivo)
        {
            var now = DateTime.Now;
            return $"APAR-{now:MMddyyyy}-{consecutivo:D3}";
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
            int userId,
            string userName,
            List<CartItem> items,
            PaymentMethod paymentMethod,
            decimal amountPaid,
            decimal change)
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
                    DiscountInfo = item.DiscountInfo
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
                    Phone = branchPhone
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
                    GrandTotal = subtotal,
                    ItemCount = itemCount
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

        public TicketData GenerateCreditTicket(
            string folio,
            string branchName,
            string branchAddress,
            string branchPhone,
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
                    Phone = branchPhone
                },
                Sale = new TicketSale
                {
                    Folio = folio,
                    DateTime = now,
                    UserName = userName
                },
                Products = items.Select(i => new TicketProduct
                {
                    Barcode = i.Barcode,
                    Name = i.ProductName,
                    Quantity = i.Quantity,
                    UnitPrice = i.FinalUnitPrice,
                    LineTotal = i.LineTotal
                }).ToList(),
                Totals = new TicketTotals
                {
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
                    Phone = branchPhone
                },
                Sale = new TicketSale
                {
                    Folio = folio,
                    DateTime = now,
                    UserName = userName
                },
                Products = items.Select(i => new TicketProduct
                {
                    Barcode = i.Barcode,
                    Name = i.ProductName,
                    Quantity = i.Quantity,
                    UnitPrice = i.FinalUnitPrice,
                    LineTotal = i.LineTotal
                }).ToList(),
                Totals = new TicketTotals
                {
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

        public string GenerateTicketText(TicketData ticket, int lineWidth = 40)
        {
            return GenerateTicketText(ticket, TicketType.Sale, lineWidth);
        }

        public string GenerateTicketText(TicketData ticket, TicketType type, int lineWidth = 40)
        {
            var lines = new List<string>();
            var separator = new string('-', lineWidth);

            lines.AddRange(FormatHeader(ticket, lineWidth));
            lines.Add(separator);

            switch (type)
            {
                case TicketType.Credit:
                    lines.AddRange(FormatCreditBody(ticket, lineWidth));
                    break;
                case TicketType.Layaway:
                    lines.AddRange(FormatLayawayBody(ticket, lineWidth));
                    break;
                default:
                    lines.AddRange(FormatSaleBody(ticket, lineWidth));
                    break;
            }

            lines.Add(separator);
            lines.AddRange(FormatFooter(ticket, type, lineWidth));

            return string.Join(Environment.NewLine, lines);
        }

        private List<string> FormatHeader(TicketData ticket, int lineWidth)
        {
            var lines = new List<string>
            {
                CenterText(ticket.Branch.Name, lineWidth)
            };

            if (!string.IsNullOrEmpty(ticket.Branch.Address))
                lines.Add(CenterText(ticket.Branch.Address, lineWidth));
            if (!string.IsNullOrEmpty(ticket.Branch.Phone))
                lines.Add(CenterText($"Tel: {ticket.Branch.Phone}", lineWidth));

            return lines;
        }

        private List<string> FormatSaleBody(TicketData ticket, int lineWidth)
        {
            var lines = new List<string>
            {
                $"Folio: {ticket.Sale.Folio}",
                $"Fecha: {ticket.Sale.DateTime:dd/MM/yyyy HH:mm}",
                $"Atendio: {ticket.Sale.UserName}",
                new string('-', lineWidth)
            };

            foreach (var product in ticket.Products)
            {
                lines.Add($"{product.Quantity} x {product.Name}");
                lines.Add($"  ${product.UnitPrice:N2} c/u = ${product.LineTotal:N2}");

                if (product.Discount > 0)
                    lines.Add($"  Desc: -${product.Discount:N2}");
            }

            lines.Add(new string('-', lineWidth));

            if (ticket.Totals.TotalDiscount > 0)
            {
                lines.Add($"Subtotal: ${ticket.Totals.Subtotal:N2}");
                lines.Add($"Descuento: -${ticket.Totals.TotalDiscount:N2}");
            }

            lines.Add($"TOTAL: ${ticket.Totals.GrandTotal:N2}");
            lines.Add($"Articulos: {ticket.Totals.ItemCount}");
            lines.Add(new string('-', lineWidth));
            lines.Add($"Pago: {ticket.Payment.MethodName}");
            lines.Add($"Recibido: ${ticket.Payment.Amount:N2}");

            if (ticket.Payment.Change > 0)
                lines.Add($"Cambio: ${ticket.Payment.Change:N2}");

            return lines;
        }

        private List<string> FormatCreditBody(TicketData ticket, int lineWidth)
        {
            var lines = new List<string>
            {
                CenterText("*** CREDITO ***", lineWidth),
                new string('-', lineWidth),
                $"Folio: {ticket.Sale.Folio}",
                $"Fecha: {ticket.Sale.DateTime:dd/MM/yyyy HH:mm}",
                $"Atendio: {ticket.Sale.UserName}"
            };

            lines.Add(new string('-', lineWidth));

            foreach (var product in ticket.Products)
            {
                lines.Add($"{product.Quantity} x {product.Name}");
                lines.Add($"  ${product.UnitPrice:N2} = ${product.LineTotal:N2}");
            }

            lines.Add(new string('-', lineWidth));
            lines.Add($"TOTAL:          ${ticket.Totals.GrandTotal:N2}");
            lines.Add($"ABONO INICIAL:  ${ticket.Payment.Amount:N2}");

            return lines;
        }

        private List<string> FormatLayawayBody(TicketData ticket, int lineWidth)
        {
            var lines = new List<string>
            {
                CenterText("*** APARTADO ***", lineWidth),
                new string('-', lineWidth),
                $"Folio: {ticket.Sale.Folio}",
                $"Fecha: {ticket.Sale.DateTime:dd/MM/yyyy HH:mm}",
                $"Atendio: {ticket.Sale.UserName}"
            };

            lines.Add(new string('-', lineWidth));
            lines.Add("PRODUCTOS APARTADOS:");

            foreach (var product in ticket.Products)
            {
                lines.Add($"{product.Quantity} x {product.Name}");
                lines.Add($"  ${product.UnitPrice:N2} = ${product.LineTotal:N2}");
            }

            lines.Add(new string('-', lineWidth));
            lines.Add($"TOTAL:          ${ticket.Totals.GrandTotal:N2}");
            lines.Add($"ABONO INICIAL:  ${ticket.Payment.Amount:N2}");

            return lines;
        }

        private List<string> FormatFooter(TicketData ticket, TicketType type, int lineWidth)
        {
            var lines = new List<string>();

            if (type == TicketType.Layaway)
            {
                lines.Add(new string('-', lineWidth));
                lines.Add(CenterText("*** CONSERVE ESTE TICKET ***", lineWidth));
                lines.Add(CenterText("*** PARA RECOGER SU MERCANCIA ***", lineWidth));
            }

            lines.Add(new string('-', lineWidth));
            lines.Add(CenterText("Gracias por su compra", lineWidth));

            return lines;
        }

        private string CenterText(string text, int width)
        {
            if (text.Length >= width) return text;
            int padding = (width - text.Length) / 2;
            return text.PadLeft(text.Length + padding).PadRight(width);
        }
    }
}