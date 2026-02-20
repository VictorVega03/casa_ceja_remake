using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using CasaCejaRemake.Models;
using CasaCejaRemake.Services;

namespace CasaCejaRemake.Helpers
{
    /// <summary>
    /// Plantillas de tickets para impresora térmica.
    /// Formatos basados en "Formatos Tickets Casa Ceja.md" y sistema legacy.
    /// Ancho por defecto: 32 caracteres (Xprinter 58mm).
    /// </summary>
    public static class ThermalTicketTemplates
    {
        private const string EMPRESA = "CASA CEJA";
        private const string URL_FACTURACION = "https://cm-papeleria.com/public/facturacion";

        // =====================================================================
        // COLUMNAS DINÁMICAS
        // =====================================================================
        /// <summary>
        /// Calcula anchos de columna según el ancho de línea.
        /// Retorna (nameWidth, qtyWidth, priceWidth, totalWidth)
        /// </summary>
        private static (int nameW, int qtyW, int priceW, int totalW) GetColumnWidths(int lineWidth)
        {
            int qtyW = 3;    // "  1" a "999"
            int priceW = 8;  // " 1295.00"
            int totalW = 8;  // " 1165.50"
            int spaces = 3;  // 3 espacios separadores
            int nameW = lineWidth - qtyW - priceW - totalW - spaces;
            if (nameW < 6) nameW = 6;
            return (nameW, qtyW, priceW, totalW);
        }

        /// <summary>
        /// Genera el encabezado de columnas dinámicamente
        /// </summary>
        private static string BuildColumnHeader(int lineWidth)
        {
            var (nameW, qtyW, priceW, totalW) = GetColumnWidths(lineWidth);
            string name = "PRODUCTO".PadRight(nameW);
            if (name.Length > nameW) name = name.Substring(0, nameW);
            string qty = "CAN".PadLeft(qtyW);
            string price = "P.UNIT".PadLeft(priceW);
            string total = "P.TOTAL".PadLeft(totalW);
            return $"{name} {qty} {price} {total}";
        }

        // =====================================================================
        // TICKET DE VENTA — Sección 2
        // =====================================================================
        public static string FormatSaleTicket(TicketData ticket, string rfc = "", string ticketFooter = "", int lineWidth = 32)
        {
            var lines = new List<string>();
            var sep = new string('-', lineWidth);

            // ── Encabezado ──
            lines.AddRange(BuildHeader(ticket.Branch.Name, ticket.Branch.Address,
                ticket.Sale.DateTime, ticket.Sale.Folio, lineWidth));
            lines.Add("");

            // ── Artículos ──
            lines.Add(BuildColumnHeader(lineWidth));
            lines.Add(sep);

            foreach (var p in ticket.Products)
                lines.Add(FormatProductLine(p, lineWidth));

            lines.Add(sep);

            // ── Subtotal y descuentos (sin flecha) ──
            lines.Add(FormatAmountLine("SUBTOTAL $", ticket.Totals.Subtotal, lineWidth));

            decimal descCategoria = ticket.Products
                .Where(p => p.HasCategoryDiscount)
                .Sum(p => p.CategoryDiscountAmount * p.Quantity);
            decimal descEspecial = ticket.Products
                .Where(p => p.IsSpecialPrice)
                .Sum(p => p.Discount - (p.HasCategoryDiscount ? p.CategoryDiscountAmount * p.Quantity : 0));

            if (descCategoria > 0)
                lines.Add(FormatAmountLine("DESC. CATEG", -descCategoria, lineWidth));

            if (descEspecial > 0)
                lines.Add(FormatAmountLine("DESC. P.ESP", -descEspecial, lineWidth));

            if (ticket.Totals.GeneralDiscount > 0)
            {
                string label = ticket.Totals.IsGeneralDiscountPercentage
                    ? $"DESC. VENTA ({ticket.Totals.GeneralDiscountPercent}%)"
                    : "DESC. VENTA";
                lines.Add(FormatAmountLine(label, -ticket.Totals.GeneralDiscount, lineWidth));
            }

            // Solo TOTAL A PAGAR lleva flecha
            lines.Add(FormatArrowLine("TOTAL A PAGAR $", ticket.Totals.GrandTotal, lineWidth));
            lines.Add(sep);

            // ── Pagos ──
            lines.AddRange(BuildPaymentBlock(ticket.Payment, lineWidth));
            lines.Add(sep);

            // ── Pie ──
            lines.Add("");
            lines.Add($"LE ATENDIO: {ticket.Sale.UserName}");
            lines.Add($"NO DE ARTICULOS: {ticket.Totals.ItemCount:D5}");
            lines.Add("GRACIAS POR SU COMPRA");
            lines.Add("");

            lines.AddRange(BuildFooter(rfc, ticketFooter, lineWidth));

            return string.Join(Environment.NewLine, lines);
        }

        // =====================================================================
        // TICKET DE CRÉDITO — Sección 7
        // =====================================================================
        public static string FormatCreditTicket(TicketData ticket, string rfc = "", string ticketFooter = "", int lineWidth = 32)
        {
            return FormatCreditLayawayTicket(ticket, "TICKET DE CREDITO", rfc, ticketFooter, lineWidth);
        }

        // =====================================================================
        // TICKET DE APARTADO — Sección 6
        // =====================================================================
        public static string FormatLayawayTicket(TicketData ticket, string rfc = "", string ticketFooter = "", int lineWidth = 32)
        {
            return FormatCreditLayawayTicket(ticket, "TICKET DE APARTADO", rfc, ticketFooter, lineWidth);
        }

        private static string FormatCreditLayawayTicket(TicketData ticket, string typeLabel, string rfc, string ticketFooter, int lineWidth)
        {
            var lines = new List<string>();
            var sep = new string('-', lineWidth);

            lines.AddRange(BuildHeader(ticket.Branch.Name, ticket.Branch.Address,
                ticket.Sale.DateTime, ticket.Sale.Folio, lineWidth));
            lines.Add("");
            lines.Add(CenterText(typeLabel, lineWidth));
            lines.Add("");

            // ── Artículos ──
            lines.Add(BuildColumnHeader(lineWidth));
            lines.Add(sep);

            foreach (var p in ticket.Products)
                lines.Add(FormatProductLine(p, lineWidth));

            lines.Add(sep);

            // ── Subtotal y descuentos (sin flecha) ──
            lines.Add(FormatAmountLine("SUBTOTAL $", ticket.Totals.Subtotal, lineWidth));

            decimal descCategoria = ticket.Products
                .Where(p => p.HasCategoryDiscount)
                .Sum(p => p.CategoryDiscountAmount * p.Quantity);
            decimal descEspecial = ticket.Products
                .Where(p => p.IsSpecialPrice)
                .Sum(p => p.Discount - (p.HasCategoryDiscount ? p.CategoryDiscountAmount * p.Quantity : 0));

            if (descCategoria > 0)
                lines.Add(FormatAmountLine("DESC. CATEG", -descCategoria, lineWidth));
            if (descEspecial > 0)
                lines.Add(FormatAmountLine("DESC. P.ESP", -descEspecial, lineWidth));

            // Solo TOTAL lleva flecha
            lines.Add(FormatArrowLine("TOTAL $", ticket.Totals.GrandTotal, lineWidth));
            lines.Add(sep);

            // ── Pagos ──
            lines.AddRange(BuildPaymentBlock(ticket.Payment, lineWidth));
            lines.Add(sep);

            // ── POR PAGAR ──
            decimal porPagar = ticket.Totals.GrandTotal - ticket.Payment.Amount;
            if (porPagar < 0) porPagar = 0;
            lines.Add(FormatArrowLine("POR PAGAR $", porPagar, lineWidth));
            lines.Add("");

            // ── Pie ──
            lines.Add($"LE ATENDIO: {ticket.Sale.UserName}");
            lines.Add($"NO DE ARTICULOS: {ticket.Totals.ItemCount:D5}");

            if (ticket.DueDate.HasValue)
            {
                lines.Add("FECHA DE VENCIMIENTO:");
                lines.Add(ticket.DueDate.Value.ToString("dd/MM/yyyy"));
            }

            if (!string.IsNullOrEmpty(ticket.CustomerName))
            {
                lines.Add("");
                lines.Add(ticket.CustomerName);
            }

            if (!string.IsNullOrEmpty(ticket.CustomerPhone))
                lines.Add($"TEL: {ticket.CustomerPhone}");

            lines.Add("");
            lines.AddRange(BuildFooter(rfc, ticketFooter, lineWidth));

            return string.Join(Environment.NewLine, lines);
        }

        // =====================================================================
        // TICKET DE ABONO — Sección 8
        // =====================================================================
        public static string FormatPaymentTicket(PaymentTicketData data, string rfc = "", int lineWidth = 32)
        {
            var lines = new List<string>();
            var sep = new string('-', lineWidth);

            lines.AddRange(BuildHeader(data.BranchName, data.BranchAddress,
                data.PaymentDate, data.Folio, lineWidth));
            lines.Add(CenterText("TICKET DE ABONO", lineWidth));
            lines.Add("");

            lines.Add("CONCEPTO:");
            string concepto = data.OperationType == 0
                ? $"  CREDITO: {data.OperationFolio}"
                : $"  APARTADO: {data.OperationFolio}";
            lines.Add(concepto);
            lines.Add("");

            lines.Add(sep);
            lines.AddRange(BuildPaymentBlockFromJson(data.PaymentDetails, lineWidth));
            lines.Add(sep);
            lines.Add(FormatArrowLine("TOTAL ABONADO", data.TotalPaid, lineWidth));
            lines.Add(sep);
            lines.Add(FormatArrowLine("POR PAGAR $", data.RemainingBalance, lineWidth));
            lines.Add("");

            lines.Add($"LE ATENDIO: {data.UserName}");
            lines.Add("GRACIAS POR SU PREFERENCIA");
            lines.Add("");

            if (!string.IsNullOrWhiteSpace(rfc))
                lines.Add($"RFC: {rfc}");

            return string.Join(Environment.NewLine, lines);
        }

        // =====================================================================
        // TICKET DE CORTE CZ — Sección 4
        // =====================================================================
        public static string FormatCashCloseTicket(
            string branchName,
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
            decimal totalExpenses,
            decimal totalIncome,
            decimal surplus,
            List<(string Concept, decimal Amount)>? expenses = null,
            List<(string Concept, decimal Amount)>? incomes = null,
            int lineWidth = 32)
        {
            var lines = new List<string>();
            var sep = new string('-', lineWidth);

            lines.Add(CenterText(EMPRESA, lineWidth));
            lines.Add("");
            lines.Add($"SUCURSAL: {branchName.ToUpper()}");
            lines.Add("");
            lines.Add($"CZ FOLIO: {folio}");
            lines.Add("");
            lines.Add(sep);
            lines.Add(FormatLabelValue("APERTURA:", openingDate.ToString("dd/MM/yy HH:mm"), lineWidth));
            lines.Add(FormatLabelValue("CORTE:", closeDate.ToString("dd/MM/yy HH:mm"), lineWidth));
            lines.Add(sep);
            lines.Add("");

            decimal totalCZ = totalCash + totalDebit + totalCredit + totalChecks + totalTransfer + surplus;
            decimal efectivoDirecto = totalCash - totalExpenses;

            lines.Add(FormatAmountLine("FONDO APERTURA:", openingCash, lineWidth));
            lines.Add("");
            lines.Add(FormatAmountLine("TOTAL CZ:", totalCZ, lineWidth));
            lines.Add(sep);

            lines.Add(FormatAmountLine("EFECT. CREDITOS:", creditCash, lineWidth));
            lines.Add(FormatAmountLine("EFECT. APARTADOS:", layawayCash, lineWidth));
            lines.Add(FormatAmountLine("EFECT. DIRECTO:", efectivoDirecto, lineWidth));
            lines.Add(sep);
            lines.Add("");

            lines.Add(sep);
            lines.Add(FormatAmountLine("T. DEBITO:", totalDebit, lineWidth));
            lines.Add(FormatAmountLine("T. CREDITO:", totalCredit, lineWidth));
            lines.Add(FormatAmountLine("CHEQUES:", totalChecks, lineWidth));
            lines.Add(FormatAmountLine("TRANSFERENCIAS:", totalTransfer, lineWidth));
            lines.Add(sep);
            lines.Add("");

            lines.Add(sep);
            lines.Add(FormatAmountLine("SOBRANTE:", surplus, lineWidth));
            lines.Add(FormatAmountLine("GASTOS:", totalExpenses, lineWidth));
            lines.Add(FormatAmountLine("INGRESOS:", totalIncome, lineWidth));

            decimal efectivoTotal = openingCash + totalCash + layawayCash + creditCash + totalIncome - totalExpenses;
            lines.Add(FormatAmountLine("EFECT. TOTAL:", efectivoTotal, lineWidth));
            lines.Add(sep);

            if (expenses != null && expenses.Count > 0)
            {
                lines.Add("");
                lines.Add("DETALLE DE GASTOS:");
                foreach (var e in expenses)
                    lines.Add(FormatAmountLine($"  {e.Concept}:", e.Amount, lineWidth));
            }

            if (incomes != null && incomes.Count > 0)
            {
                lines.Add("");
                lines.Add("DETALLE DE INGRESOS:");
                foreach (var i in incomes)
                    lines.Add(FormatAmountLine($"  {i.Concept}:", i.Amount, lineWidth));
            }

            lines.Add("");
            lines.Add("");
            lines.Add("");
            lines.Add(sep);
            lines.Add($"CAJERO: {userName}");

            return string.Join(Environment.NewLine, lines);
        }

        // =====================================================================
        // REIMPRIMIR CRÉDITO/APARTADO CON HISTORIAL — Secciones 9/10
        // =====================================================================
        public static string FormatReprintWithHistory(
            TicketData ticket,
            string typeLabel,
            List<string>? paymentDetailsJsonList,
            decimal totalPaid,
            string rfc = "",
            int lineWidth = 32)
        {
            var lines = new List<string>();
            var sep = new string('-', lineWidth);

            lines.AddRange(BuildHeader(ticket.Branch.Name, ticket.Branch.Address,
                ticket.Sale.DateTime, ticket.Sale.Folio, lineWidth));
            lines.Add("");
            lines.Add(CenterText(typeLabel, lineWidth));
            lines.Add("");

            lines.Add(BuildColumnHeader(lineWidth));
            lines.Add(sep);

            foreach (var p in ticket.Products)
                lines.Add(FormatProductLine(p, lineWidth));

            lines.Add(sep);
            lines.Add(FormatArrowLine("TOTAL", ticket.Totals.GrandTotal, lineWidth));
            lines.Add(sep);

            if (paymentDetailsJsonList != null && paymentDetailsJsonList.Count > 0)
            {
                lines.Add("HISTORIAL DE PAGOS:");
                lines.Add("");
                foreach (var paymentJson in paymentDetailsJsonList)
                {
                    lines.AddRange(BuildPaymentBlockFromJson(paymentJson, lineWidth));
                    lines.Add(sep);
                }
            }

            decimal porPagar = ticket.Totals.GrandTotal - totalPaid;
            if (porPagar < 0) porPagar = 0;
            lines.Add(FormatArrowLine("POR PAGAR $", porPagar, lineWidth));
            lines.Add("");

            lines.Add($"LE ATENDIO: {ticket.Sale.UserName}");
            lines.Add($"NO DE ARTICULOS: {ticket.Totals.ItemCount:D5}");
            lines.Add("");

            if (!string.IsNullOrWhiteSpace(rfc))
                lines.Add($"RFC: {rfc}");

            return string.Join(Environment.NewLine, lines);
        }

        // =====================================================================
        // REIMPRIMIR VENTA — Sección 12
        // =====================================================================
        public static string FormatReprintSaleTicket(TicketData ticket, string rfc = "", string ticketFooter = "", int lineWidth = 32)
        {
            return FormatSaleTicket(ticket, rfc, ticketFooter, lineWidth);
        }

        // =====================================================================
        //                      HELPERS PRIVADOS
        // =====================================================================

        private static List<string> BuildHeader(string branchName, string address, DateTime date, string folio, int lineWidth)
        {
            // Título grande: espaciar letras para que resalte
            string bigTitle = string.Join(" ", EMPRESA.ToCharArray());
            var lines = new List<string>
            {
                CenterText(bigTitle, lineWidth),
                "",   // línea en blanco para que resalte el título
                $"SUCURSAL: {branchName.ToUpper()}"
            };

            if (!string.IsNullOrWhiteSpace(address))
                lines.Add(address);

            lines.Add(date.ToString("dd/MM/yyyy hh:mm tt"));
            lines.Add($"FOLIO: {folio}");

            return lines;
        }

        private static List<string> BuildPaymentBlock(TicketPayment payment, int lineWidth)
        {
            var lines = new List<string>();

            if (!string.IsNullOrEmpty(payment.PaymentDetails))
            {
                lines.AddRange(BuildPaymentBlockFromJson(payment.PaymentDetails, lineWidth));
            }
            else
            {
                string label = payment.MethodName switch
                {
                    "Efectivo" => "EFECTIVO",
                    "Tarjeta de Debito" or "Tarjeta Débito" => "T. DEBITO",
                    "Tarjeta de Credito" or "Tarjeta Crédito" => "T. CREDITO",
                    "Transferencia" => "TRANSFERENCIA",
                    "Cheques" => "CHEQUES",
                    _ => payment.MethodName.ToUpper()
                };
                lines.Add(FormatArrowLine(label, payment.Amount, lineWidth));
            }

            if (payment.Change > 0)
                lines.Add(FormatArrowLine("SU CAMBIO $", payment.Change, lineWidth));

            return lines;
        }

        private static List<string> BuildPaymentBlockFromJson(string paymentJson, int lineWidth)
        {
            var lines = new List<string>();
            if (string.IsNullOrEmpty(paymentJson)) return lines;

            try
            {
                var payments = JsonSerializer.Deserialize<Dictionary<string, decimal>>(paymentJson);
                if (payments == null) return lines;

                foreach (var kvp in payments)
                {
                    if (kvp.Value <= 0) continue;
                    string label = kvp.Key switch
                    {
                        "efectivo" => "EFECTIVO",
                        "tarjeta_debito" => "T. DEBITO",
                        "tarjeta_credito" => "T. CREDITO",
                        "transferencia" => "TRANSFERENCIA",
                        "cheques" => "CHEQUES",
                        _ => kvp.Key.ToUpper()
                    };
                    lines.Add(FormatArrowLine(label, kvp.Value, lineWidth));
                }
            }
            catch
            {
                lines.Add($"  {paymentJson}");
            }

            return lines;
        }

        private static List<string> BuildFooter(string rfc, string ticketFooter, int lineWidth)
        {
            var lines = new List<string>();

            if (!string.IsNullOrWhiteSpace(ticketFooter))
                lines.Add(ticketFooter);

            if (!string.IsNullOrWhiteSpace(rfc))
                lines.Add($"RFC: {rfc}");

            lines.Add("");
            lines.Add("SI DESEA FACTURAR INGRESE A:");
            lines.Add(URL_FACTURACION);

            return lines;
        }

        /// <summary>
        /// Línea de producto en columnas dinámicas:
        /// {Nombre+indicadores} {Cant} {P.Unit} {P.Total}
        /// </summary>
        private static string FormatProductLine(TicketProduct product, int lineWidth)
        {
            string indicators = "";
            if (product.IsSpecialPrice)
                indicators += "*ESP";
            if (product.HasCategoryDiscount)
                indicators += $"*CAT{product.CategoryDiscountPercent:0}%";

            string name = product.Name + indicators;

            var (nameW, qtyW, priceW, totalW) = GetColumnWidths(lineWidth);

            if (name.Length > nameW)
                name = name.Substring(0, nameW);

            decimal displayPrice = product.ListPrice > 0 ? product.ListPrice : product.UnitPrice;

            string paddedName = name.PadRight(nameW);
            return $"{paddedName} {product.Quantity.ToString().PadLeft(qtyW)} {displayPrice.ToString("F2").PadLeft(priceW)} {product.LineTotal.ToString("F2").PadLeft(totalW)}";
        }

        /// <summary>
        /// Formato flecha: {LABEL} ------>{espacios}{monto}
        /// Si no cabe la flecha, usa formato simple sin flecha.
        /// </summary>
        private static string FormatArrowLine(string label, decimal amount, int lineWidth)
        {
            string arrow = "------>";
            string amountStr = amount.ToString("F2");

            string withArrow = $"{label} {arrow}";
            int totalNeeded = withArrow.Length + 1 + amountStr.Length;

            // Si cabe con flecha, usar formato flecha
            if (totalNeeded <= lineWidth)
            {
                int spaces = lineWidth - withArrow.Length - amountStr.Length;
                if (spaces < 1) spaces = 1;
                return $"{withArrow}{new string(' ', spaces)}{amountStr}";
            }

            // Si no cabe, formato simple: label + espacios + monto
            int simpleSpaces = lineWidth - label.Length - amountStr.Length;
            if (simpleSpaces < 1) simpleSpaces = 1;
            return $"{label}{new string(' ', simpleSpaces)}{amountStr}";
        }

        /// <summary>
        /// Label izquierda, $monto derecha (para corte de caja)
        /// </summary>
        private static string FormatAmountLine(string label, decimal amount, int lineWidth)
        {
            string amountStr = $"${amount:N2}";
            int spaces = lineWidth - label.Length - amountStr.Length;
            if (spaces < 1) spaces = 1;
            return $"{label}{new string(' ', spaces)}{amountStr}";
        }

        private static string FormatLabelValue(string label, string value, int lineWidth)
        {
            int spaces = lineWidth - label.Length - value.Length;
            if (spaces < 1) spaces = 1;
            return $"{label}{new string(' ', spaces)}{value}";
        }

        private static string CenterText(string text, int width)
        {
            if (text.Length >= width) return text;
            int padding = (width - text.Length) / 2;
            return text.PadLeft(text.Length + padding).PadRight(width);
        }
    }
}
