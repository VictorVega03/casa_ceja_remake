using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace CasaCejaRemake.Services
{
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

    public enum PaymentMethod
    {
        Efectivo = 1,
        TarjetaDebito = 2,
        TarjetaCredito = 3,
        Transferencia = 4
    }

    public class TicketService
    {
        public string GenerateFolio(int branchId, int consecutivo)
        {
            var now = DateTime.Now;
            return $"{now:MMddyyyy}{branchId:D2}{consecutivo:D4}";
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
            var lines = new List<string>();
            var separator = new string('-', lineWidth);

            // Encabezado
            lines.Add(CenterText(ticket.Branch.Name, lineWidth));
            if (!string.IsNullOrEmpty(ticket.Branch.Address))
                lines.Add(CenterText(ticket.Branch.Address, lineWidth));
            if (!string.IsNullOrEmpty(ticket.Branch.Phone))
                lines.Add(CenterText($"Tel: {ticket.Branch.Phone}", lineWidth));
            
            lines.Add(separator);

            // Info de venta
            lines.Add($"Folio: {ticket.Sale.Folio}");
            lines.Add($"Fecha: {ticket.Sale.DateTime:dd/MM/yyyy HH:mm}");
            lines.Add($"Atendio: {ticket.Sale.UserName}");

            lines.Add(separator);

            // Productos
            foreach (var product in ticket.Products)
            {
                lines.Add($"{product.Quantity} x {product.Name}");
                
                var priceInfo = $"  ${product.UnitPrice:N2} c/u = ${product.LineTotal:N2}";
                lines.Add(priceInfo);

                if (product.Discount > 0)
                {
                    lines.Add($"  Desc: -${product.Discount:N2}");
                }
            }

            lines.Add(separator);

            // Totales
            if (ticket.Totals.TotalDiscount > 0)
            {
                lines.Add($"Subtotal: ${ticket.Totals.Subtotal:N2}");
                lines.Add($"Descuento: -${ticket.Totals.TotalDiscount:N2}");
            }
            
            lines.Add($"TOTAL: ${ticket.Totals.GrandTotal:N2}");
            lines.Add($"Articulos: {ticket.Totals.ItemCount}");

            lines.Add(separator);

            // Pago
            lines.Add($"Pago: {ticket.Payment.MethodName}");
            lines.Add($"Recibido: ${ticket.Payment.Amount:N2}");
            if (ticket.Payment.Change > 0)
            {
                lines.Add($"Cambio: ${ticket.Payment.Change:N2}");
            }

            lines.Add(separator);
            lines.Add(CenterText("Gracias por su compra", lineWidth));

            return string.Join(Environment.NewLine, lines);
        }

        private string CenterText(string text, int width)
        {
            if (text.Length >= width) return text;
            int padding = (width - text.Length) / 2;
            return text.PadLeft(text.Length + padding).PadRight(width);
        }
    }
}