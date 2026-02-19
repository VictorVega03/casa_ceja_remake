using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using CasaCejaRemake.Models;
using CasaCejaRemake.Services;

namespace CasaCejaRemake.Helpers
{
    /// <summary>
    /// Plantillas de tickets para impresora térmica (40 caracteres de ancho).
    /// Formatos basados en "Formatos Tickets Casa Ceja.md".
    /// </summary>
    public static class ThermalTicketTemplates
    {
        private const string EMPRESA = "CASA CEJA";
        private const string URL_FACTURACION = "https://cm-papeleria.com/public/facturacion";
        private const string HEADER_ARTICULOS = "Articulo        Can    P.Unit    Importe";

        // =====================================================================
        // TICKET DE VENTA — Sección 2
        // =====================================================================
        public static string FormatSaleTicket(TicketData ticket, string rfc = "", string ticketFooter = "", int lineWidth = 40)
        {
            var lines = new List<string>();
            var sep = new string('-', lineWidth);
            var sepEqual = new string('=', lineWidth);

            // ── Encabezado ──
            lines.Add(sepEqual);
            lines.AddRange(BuildHeader(ticket.Branch.Name, ticket.Branch.Address,
                ticket.Sale.DateTime, ticket.Sale.Folio, lineWidth));
            lines.Add("");

            // ── Columnas de artículos ──
            lines.Add(HEADER_ARTICULOS);
            lines.Add(sep);

            foreach (var p in ticket.Products)
            {
                lines.Add(FormatProductLine(p, lineWidth));
            }

            lines.Add(sep);

            // ── Subtotal y descuentos ──
            lines.Add(FormatAmountLine("SUBTOTAL $", ticket.Totals.Subtotal, lineWidth));

            // Calcular descuento por categoría y precio especial a partir de los productos
            decimal descCategoria = ticket.Products
                .Where(p => p.HasCategoryDiscount)
                .Sum(p => p.CategoryDiscountAmount * p.Quantity);
            decimal descEspecial = ticket.Products
                .Where(p => p.IsSpecialPrice)
                .Sum(p => p.Discount - (p.HasCategoryDiscount ? p.CategoryDiscountAmount * p.Quantity : 0));

            if (descCategoria > 0)
                lines.Add(FormatAmountLine("DESC. POR CATEGORIA", -descCategoria, lineWidth));

            if (descEspecial > 0)
                lines.Add(FormatAmountLine("DESC. PRECIO ESPECIAL", -descEspecial, lineWidth));

            if (ticket.Totals.GeneralDiscount > 0)
            {
                string label = ticket.Totals.IsGeneralDiscountPercentage
                    ? $"DESCUENTO DE VENTA ({ticket.Totals.GeneralDiscountPercent}%)"
                    : "DESCUENTO DE VENTA";
                lines.Add(FormatAmountLine(label, -ticket.Totals.GeneralDiscount, lineWidth));
            }

            lines.Add(FormatAmountLine("TOTAL FINAL $", ticket.Totals.GrandTotal, lineWidth));
            lines.Add(sep);

            // ── Pagos ──
            lines.AddRange(BuildPaymentBlock(ticket.Payment, lineWidth));
            lines.Add(sep);

            // ── Pie ──
            lines.Add("");
            lines.Add(CenterText($"LE ATENDIO: {ticket.Sale.UserName}", lineWidth));
            lines.Add(CenterText($"NO DE ARTICULOS: {ticket.Totals.ItemCount:D5}", lineWidth));
            lines.Add(CenterText("GRACIAS POR SU COMPRA", lineWidth));
            lines.Add("");

            lines.AddRange(BuildFooter(rfc, ticketFooter, lineWidth));

            return string.Join(Environment.NewLine, lines);
        }

        // =====================================================================
        // TICKET DE CRÉDITO — Sección 7
        // =====================================================================
        public static string FormatCreditTicket(TicketData ticket, string rfc = "", string ticketFooter = "", int lineWidth = 40)
        {
            return FormatCreditLayawayTicket(ticket, "TICKET DE CREDITO", rfc, ticketFooter, lineWidth);
        }

        // =====================================================================
        // TICKET DE APARTADO — Sección 6
        // =====================================================================
        public static string FormatLayawayTicket(TicketData ticket, string rfc = "", string ticketFooter = "", int lineWidth = 40)
        {
            return FormatCreditLayawayTicket(ticket, "TICKET DE APARTADO", rfc, ticketFooter, lineWidth);
        }

        /// <summary>
        /// Formato compartido para crédito y apartado — Secciones 6 y 7
        /// </summary>
        private static string FormatCreditLayawayTicket(TicketData ticket, string typeLabel, string rfc, string ticketFooter, int lineWidth)
        {
            var lines = new List<string>();
            var sep = new string('-', lineWidth);

            // ── Encabezado ──
            lines.AddRange(BuildHeader(ticket.Branch.Name, ticket.Branch.Address,
                ticket.Sale.DateTime, ticket.Sale.Folio, lineWidth));
            lines.Add("");
            lines.Add(CenterText(typeLabel, lineWidth));
            lines.Add("");

            // ── Artículos ──
            lines.Add(HEADER_ARTICULOS);
            lines.Add(sep);

            foreach (var p in ticket.Products)
            {
                lines.Add(FormatProductLine(p, lineWidth));
            }

            lines.Add(sep);

            // ── Subtotal y descuentos ──
            lines.Add(FormatAmountLine("SUBTOTAL $", ticket.Totals.Subtotal, lineWidth));

            decimal descCategoria = ticket.Products
                .Where(p => p.HasCategoryDiscount)
                .Sum(p => p.CategoryDiscountAmount * p.Quantity);
            decimal descEspecial = ticket.Products
                .Where(p => p.IsSpecialPrice)
                .Sum(p => p.Discount - (p.HasCategoryDiscount ? p.CategoryDiscountAmount * p.Quantity : 0));

            if (descCategoria > 0)
                lines.Add(FormatAmountLine("DESC. POR CATEGORIA", -descCategoria, lineWidth));

            if (descEspecial > 0)
                lines.Add(FormatAmountLine("DESC. PRECIO ESPECIAL", -descEspecial, lineWidth));

            // NO hay descuento de venta en créditos/apartados
            lines.Add(FormatAmountLine("TOTAL $", ticket.Totals.GrandTotal, lineWidth));
            lines.Add("");

            // ── Pagos ──
            lines.AddRange(BuildPaymentBlock(ticket.Payment, lineWidth));
            lines.Add(sep);

            // ── POR PAGAR ──
            decimal porPagar = ticket.Totals.GrandTotal - ticket.Payment.Amount;
            if (porPagar < 0) porPagar = 0;
            lines.Add(FormatAmountLine("POR PAGAR $", porPagar, lineWidth));
            lines.Add("");

            // ── Pie ──
            lines.Add(CenterText($"LE ATENDIO: {ticket.Sale.UserName}", lineWidth));
            lines.Add(CenterText($"NO DE ARTICULOS: {ticket.Totals.ItemCount:D5}", lineWidth));

            // Fecha de vencimiento
            if (ticket.DueDate.HasValue)
            {
                lines.Add(CenterText("FECHA DE VENCIMIENTO:", lineWidth));
                lines.Add(CenterText(ticket.DueDate.Value.ToString("dd/MM/yyyy"), lineWidth));
            }

            // Cliente
            if (!string.IsNullOrEmpty(ticket.CustomerName))
            {
                lines.Add(CenterText("CLIENTE:", lineWidth));
                lines.Add(CenterText(ticket.CustomerName, lineWidth));
            }

            if (!string.IsNullOrEmpty(ticket.CustomerPhone))
            {
                lines.Add(CenterText("NUMERO TELEFONICO:", lineWidth));
                lines.Add(CenterText(ticket.CustomerPhone, lineWidth));
            }

            lines.Add("");
            lines.AddRange(BuildFooter(rfc, ticketFooter, lineWidth));

            return string.Join(Environment.NewLine, lines);
        }

        // =====================================================================
        // TICKET DE ABONO — Sección 8
        // =====================================================================
        public static string FormatPaymentTicket(PaymentTicketData data, string rfc = "", int lineWidth = 40)
        {
            var lines = new List<string>();
            var sep = new string('-', lineWidth);

            // ── Encabezado ──
            lines.AddRange(BuildHeader(data.BranchName, data.BranchAddress,
                data.PaymentDate, data.Folio, lineWidth));
            lines.Add(CenterText("TICKET DE ABONO", lineWidth));
            lines.Add("");

            // ── Concepto ──
            lines.Add("CONCEPTO:");
            string concepto = data.OperationType == 0
                ? $"  CREDITO CON FOLIO: {data.OperationFolio}"
                : $"  APARTADO CON FOLIO: {data.OperationFolio}";
            lines.Add(concepto);
            lines.Add("");

            // ── Pagos ──
            lines.Add(sep);
            lines.AddRange(BuildPaymentBlockFromJson(data.PaymentDetails, lineWidth));
            lines.Add(sep);
            lines.Add(FormatAmountLine("TOTAL ABONADO", data.TotalPaid, lineWidth));
            lines.Add(sep);
            lines.Add(FormatAmountLine("POR PAGAR $", data.RemainingBalance, lineWidth));
            lines.Add("");

            // ── Pie ──
            lines.Add(CenterText($"LE ATENDIO: {data.UserName}", lineWidth));
            lines.Add(CenterText("GRACIAS POR SU PREFERENCIA", lineWidth));
            lines.Add("");

            if (!string.IsNullOrWhiteSpace(rfc))
            {
                lines.Add(CenterText($"RFC: {rfc}", lineWidth));
            }

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
            int lineWidth = 40)
        {
            var lines = new List<string>();
            var sep = new string('-', lineWidth);

            // ── Encabezado ──
            lines.Add(CenterText(EMPRESA, lineWidth));
            lines.Add("");
            lines.Add(CenterText($"SUCURSAL: {branchName.ToUpper()}", lineWidth));
            lines.Add("");
            lines.Add(CenterText($"CZ FOLIO:  {folio}", lineWidth));
            lines.Add("");
            lines.Add(sep);
            lines.Add(FormatAmountLine("FECHA DE APERTURA:", openingDate.ToString("dd/MM/yyyy HH:mm"), lineWidth));
            lines.Add(FormatAmountLine("FECHA DE CORTE:", closeDate.ToString("dd/MM/yyyy HH:mm"), lineWidth));
            lines.Add(sep);
            lines.Add("");

            // ── Fondo y Total CZ ──
            decimal totalCZ = totalCash + totalDebit + totalCredit + totalChecks + totalTransfer + surplus;
            decimal efectivoDirecto = totalCash - totalExpenses;

            lines.Add(FormatAmountLine("FONDO DE APERTURA:", openingCash, lineWidth));
            lines.Add("");
            lines.Add(FormatAmountLine("TOTAL CZ:", totalCZ, lineWidth));
            lines.Add(sep);

            // ── Efectivo desglosado ──
            lines.Add(FormatAmountLine("EFECTIVO DE CREDITOS:", creditCash, lineWidth));
            lines.Add(FormatAmountLine("EFECTIVO DE APARTADOS:", layawayCash, lineWidth));
            lines.Add(FormatAmountLine("EFECTIVO DIRECTO:", efectivoDirecto, lineWidth));
            lines.Add(sep);
            lines.Add("");

            // ── Tarjetas y otros ──
            lines.Add(sep);
            lines.Add(FormatAmountLine("TOTAL T. DEBITO", totalDebit, lineWidth));
            lines.Add(FormatAmountLine("TOTAL T. CREDITO", totalCredit, lineWidth));
            lines.Add(FormatAmountLine("TOTAL CHEQUES", totalChecks, lineWidth));
            lines.Add(FormatAmountLine("TOTAL TRANSFERENCIAS", totalTransfer, lineWidth));
            lines.Add(sep);
            lines.Add("");

            // ── Sobrante, Gastos, Ingresos, Efectivo Total ──
            lines.Add(sep);
            lines.Add(FormatAmountLine("SOBRANTE:", surplus, lineWidth));
            lines.Add(FormatAmountLine("GASTOS:", totalExpenses, lineWidth));
            lines.Add(FormatAmountLine("INGRESOS:", totalIncome, lineWidth));

            decimal efectivoTotal = openingCash + totalCash + layawayCash + creditCash + totalIncome - totalExpenses;
            lines.Add(FormatAmountLine("EFECTIVO TOTAL:", efectivoTotal, lineWidth));
            lines.Add(sep);

            // ── Detalle de gastos si los hay ──
            if (expenses != null && expenses.Count > 0)
            {
                lines.Add("");
                lines.Add("DETALLE DE GASTOS:");
                foreach (var e in expenses)
                {
                    lines.Add(FormatAmountLine($"  {e.Concept}:", e.Amount, lineWidth));
                }
            }

            // ── Detalle de ingresos si los hay ──
            if (incomes != null && incomes.Count > 0)
            {
                lines.Add("");
                lines.Add("DETALLE DE INGRESOS:");
                foreach (var i in incomes)
                {
                    lines.Add(FormatAmountLine($"  {i.Concept}:", i.Amount, lineWidth));
                }
            }

            // ── Firma ──
            lines.Add("");
            lines.Add("");
            lines.Add("");
            lines.Add(sep);
            lines.Add($"CAJERO:{userName}");

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
            int lineWidth = 40)
        {
            var lines = new List<string>();
            var sep = new string('-', lineWidth);

            // ── Encabezado ──
            lines.AddRange(BuildHeader(ticket.Branch.Name, ticket.Branch.Address,
                ticket.Sale.DateTime, ticket.Sale.Folio, lineWidth));
            lines.Add("");
            lines.Add(CenterText(typeLabel, lineWidth));
            lines.Add("");

            // ── Artículos (precio tal como fue guardado, sin reconstruir descuentos) ──
            lines.Add(HEADER_ARTICULOS);
            lines.Add(sep);

            foreach (var p in ticket.Products)
            {
                // En reimpresión mostramos UnitPrice como fue guardado
                string namePart = p.Name;
                if (namePart.Length > 20) namePart = namePart.Substring(0, 20);

                string line = $"{namePart,-20} {p.Quantity,3} {p.UnitPrice,9:N2} {p.LineTotal,9:N2}";
                if (line.Length > lineWidth) line = line.Substring(0, lineWidth);
                lines.Add(line);
            }

            lines.Add(sep);
            lines.Add(FormatAmountLine("Total", ticket.Totals.GrandTotal, lineWidth));
            lines.Add(sep);

            // ── Historial de pagos ──
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

            // ── POR PAGAR ──
            decimal porPagar = ticket.Totals.GrandTotal - totalPaid;
            if (porPagar < 0) porPagar = 0;
            lines.Add(FormatAmountLine("POR PAGAR $", porPagar, lineWidth));
            lines.Add("");

            // ── Pie ──
            lines.Add(CenterText($"LE ATENDIO: {ticket.Sale.UserName}", lineWidth));
            lines.Add(CenterText($"NO DE ARTICULOS: {ticket.Totals.ItemCount:D5}", lineWidth));
            lines.Add("");

            if (!string.IsNullOrWhiteSpace(rfc))
            {
                lines.Add(CenterText($"RFC: {rfc}", lineWidth));
            }

            return string.Join(Environment.NewLine, lines);
        }

        // =====================================================================
        // REIMPRIMIR VENTA — Sección 12 (térmica, mismos datos desde BD)
        // =====================================================================
        public static string FormatReprintSaleTicket(TicketData ticket, string rfc = "", string ticketFooter = "", int lineWidth = 40)
        {
            // Mismo formato que venta pero sin abrir cajón (controlado por PrintService)
            return FormatSaleTicket(ticket, rfc, ticketFooter, lineWidth);
        }

        // =====================================================================
        // HISTORIAL DE CORTES — Sección 11 (mismo formato que CZ)
        // =====================================================================
        // Se usa FormatCashCloseTicket directamente ya que el formato es idéntico

        // =====================================================================
        //                      HELPERS PRIVADOS
        // =====================================================================

        /// <summary>
        /// Construye el encabezado estándar: CASA CEJA, sucursal, dirección, fecha, folio
        /// </summary>
        private static List<string> BuildHeader(string branchName, string address, DateTime date, string folio, int lineWidth)
        {
            var lines = new List<string>
            {
                CenterText(EMPRESA, lineWidth),
                CenterText($"Sucursal: {branchName.ToUpper()}", lineWidth)
            };

            if (!string.IsNullOrWhiteSpace(address))
                lines.Add(CenterText(address, lineWidth));

            lines.Add(CenterText(date.ToString("dd/MM/yyyy HH:mm"), lineWidth));
            lines.Add(CenterText($"FOLIO: {folio}", lineWidth));

            return lines;
        }

        /// <summary>
        /// Construye las líneas de pago a partir del TicketPayment
        /// </summary>
        private static List<string> BuildPaymentBlock(TicketPayment payment, int lineWidth)
        {
            var lines = new List<string>();

            // Si hay detalles de pago mixto, desglosar
            if (!string.IsNullOrEmpty(payment.PaymentDetails))
            {
                lines.AddRange(BuildPaymentBlockFromJson(payment.PaymentDetails, lineWidth));
            }
            else
            {
                // Pago simple
                string label = payment.MethodName switch
                {
                    "Efectivo" => "EFECTIVO ENTREGADO",
                    "Tarjeta de Debito" or "Tarjeta Débito" => "PAGO T. DEBITO",
                    "Tarjeta de Credito" or "Tarjeta Crédito" => "PAGO T.CREDITO",
                    "Transferencia" => "PAGO TRANSFERENCIA",
                    "Cheques" => "PAGO CHEQUES",
                    _ => $"PAGO {payment.MethodName.ToUpper()}"
                };
                lines.Add(FormatAmountLine(label, payment.Amount, lineWidth));
            }

            // Cambio
            if (payment.Change > 0)
            {
                lines.Add(FormatAmountLine("SU CAMBIO $", payment.Change, lineWidth));
            }

            return lines;
        }

        /// <summary>
        /// Construye las líneas de pago a partir de un JSON de pagos mixtos
        /// </summary>
        private static List<string> BuildPaymentBlockFromJson(string paymentJson, int lineWidth)
        {
            var lines = new List<string>();

            if (string.IsNullOrEmpty(paymentJson))
                return lines;

            try
            {
                var payments = JsonSerializer.Deserialize<Dictionary<string, decimal>>(paymentJson);
                if (payments == null) return lines;

                foreach (var kvp in payments)
                {
                    if (kvp.Value <= 0) continue;

                    string label = kvp.Key switch
                    {
                        "efectivo" => "EFECTIVO ENTREGADO",
                        "tarjeta_debito" => "PAGO T. DEBITO",
                        "tarjeta_credito" => "PAGO T.CREDITO",
                        "transferencia" => "PAGO TRANSFERENCIA",
                        "cheques" => "PAGO CHEQUES",
                        _ => $"PAGO {kvp.Key.ToUpper()}"
                    };
                    lines.Add(FormatAmountLine(label, kvp.Value, lineWidth));
                }
            }
            catch
            {
                // Si el JSON no es un diccionario, intentar mostrar como texto simple
                lines.Add($"  {paymentJson}");
            }

            return lines;
        }

        /// <summary>
        /// Construye el pie del ticket: RFC, pie personalizado, URL facturación
        /// </summary>
        private static List<string> BuildFooter(string rfc, string ticketFooter, int lineWidth)
        {
            var lines = new List<string>();
            var sep = new string('-', lineWidth);

            if (!string.IsNullOrWhiteSpace(rfc))
            {
                lines.Add(CenterText($"RFC: {rfc}", lineWidth));
                lines.Add("");
            }

            if (!string.IsNullOrWhiteSpace(ticketFooter))
            {
                lines.Add(sep);
                lines.Add(CenterText(ticketFooter, lineWidth));
                lines.Add(sep);
                lines.Add("");
            }

            lines.Add(CenterText("SI DESEA FACTURAR ESTA COMPRA INGRESE A", lineWidth));
            lines.Add(CenterText(URL_FACTURACION, lineWidth));

            return lines;
        }

        /// <summary>
        /// Formatea una línea de producto según sección 13 del doc:
        /// {NombreProducto}{*ESP}{*CAT10%} {Cant} {PrecioOriginal} {Importe}
        /// </summary>
        private static string FormatProductLine(TicketProduct product, int lineWidth)
        {
            // Construir indicadores de descuento
            string indicators = "";
            if (product.IsSpecialPrice)
                indicators += "*ESP";
            if (product.HasCategoryDiscount)
                indicators += $"*CAT{product.CategoryDiscountPercent:0}%";

            string name = product.Name + indicators;

            // Columnas: nombre(variable), cant(3), precioUnit(9), importe(9)
            // Espacios entre columnas: 1
            int fixedWidth = 3 + 1 + 9 + 1 + 9; // cant + sp + pUnit + sp + importe = 23
            int nameMaxWidth = lineWidth - fixedWidth - 1; // -1 para espacio antes de cant

            if (name.Length > nameMaxWidth)
                name = name.Substring(0, nameMaxWidth);

            // P.Unit muestra el ListPrice (precio original/menudeo) — según spec §2
            decimal displayPrice = product.ListPrice > 0 ? product.ListPrice : product.UnitPrice;

            string paddedName = name.PadRight(nameMaxWidth);
            string line = $"{paddedName} {product.Quantity,3} {displayPrice,9:N2} {product.LineTotal,9:N2}";
            return line;
        }

        /// <summary>
        /// Alinea label a la izquierda y monto a la derecha: "LABEL          $1,234.56"
        /// </summary>
        private static string FormatAmountLine(string label, decimal amount, int lineWidth)
        {
            string amountStr = $"${amount:N2}";
            int spaces = lineWidth - label.Length - amountStr.Length;
            if (spaces < 1) spaces = 1;
            return $"{label}{new string(' ', spaces)}{amountStr}";
        }

        /// <summary>
        /// Alinea label (string) a la izquierda y valor (string) a la derecha
        /// </summary>
        private static string FormatAmountLine(string label, string value, int lineWidth)
        {
            int spaces = lineWidth - label.Length - value.Length;
            if (spaces < 1) spaces = 1;
            return $"{label}{new string(' ', spaces)}{value}";
        }

        /// <summary>
        /// Centra un texto en el ancho especificado
        /// </summary>
        private static string CenterText(string text, int width)
        {
            if (text.Length >= width) return text;
            int padding = (width - text.Length) / 2;
            return text.PadLeft(text.Length + padding).PadRight(width);
        }
    }
}
