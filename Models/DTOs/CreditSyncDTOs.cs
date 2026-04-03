using System.Collections.Generic;
using System.Text.Json.Serialization;
using System;

namespace CasaCejaRemake.Models.DTOs
{
    public class CreditPushDto
    {
        [JsonPropertyName("folio")]
        public string Folio { get; set; } = string.Empty;

        [JsonPropertyName("branch_id")]
        public int BranchId { get; set; }

        [JsonPropertyName("user_id")]
        public int UserId { get; set; }

        [JsonPropertyName("customer_id")]
        public int CustomerId { get; set; }

        [JsonPropertyName("total")]
        public decimal Total { get; set; }

        [JsonPropertyName("total_paid")]
        public decimal TotalPaid { get; set; }

        [JsonPropertyName("months_to_pay")]
        public int MonthsToPay { get; set; }

        [JsonPropertyName("credit_date")]
        public DateTime CreditDate { get; set; }

        [JsonPropertyName("due_date")]
        public DateTime DueDate { get; set; }

        [JsonPropertyName("status")]
        public int Status { get; set; }

        [JsonPropertyName("notes")]
        public string? Notes { get; set; }

        [JsonPropertyName("ticket_data")]
        public byte[]? TicketData { get; set; }

        [JsonPropertyName("products")]
        public List<CreditProductPushDto> Products { get; set; } = new();
    }

    public class CreditProductPushDto
    {
        [JsonPropertyName("product_id")]
        public int ProductId { get; set; }

        [JsonPropertyName("barcode")]
        public string Barcode { get; set; } = string.Empty;

        [JsonPropertyName("product_name")]
        public string ProductName { get; set; } = string.Empty;

        [JsonPropertyName("quantity")]
        public int Quantity { get; set; }

        [JsonPropertyName("unit_price")]
        public decimal UnitPrice { get; set; }

        [JsonPropertyName("line_total")]
        public decimal LineTotal { get; set; }

        [JsonPropertyName("pricing_data")]
        public byte[]? PricingData { get; set; }
    }

    public class CreditPaymentPushDto
    {
        [JsonPropertyName("folio")]
        public string Folio { get; set; } = string.Empty;

        [JsonPropertyName("credit_folio")]
        public string CreditFolio { get; set; } = string.Empty;

        [JsonPropertyName("user_id")]
        public int UserId { get; set; }

        [JsonPropertyName("amount_paid")]
        public decimal AmountPaid { get; set; }

        [JsonPropertyName("payment_method")]
        public string PaymentMethod { get; set; } = string.Empty;

        [JsonPropertyName("payment_date")]
        public DateTime PaymentDate { get; set; }

        [JsonPropertyName("cash_close_folio")]
        public string CashCloseFolio { get; set; } = string.Empty;

        [JsonPropertyName("notes")]
        public string? Notes { get; set; }
    }
}