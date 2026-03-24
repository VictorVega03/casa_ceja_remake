using System.Collections.Generic;
using System.Text.Json.Serialization;
using System;

namespace CasaCejaRemake.Models.DTOs
{
    public class SalePushDto
    {
        [JsonPropertyName("folio")]
        public string Folio { get; set; } = string.Empty;

        [JsonPropertyName("branch_id")]
        public int BranchId { get; set; }

        [JsonPropertyName("user_id")]
        public int UserId { get; set; }

        [JsonPropertyName("subtotal")]
        public decimal Subtotal { get; set; }

        [JsonPropertyName("discount")]
        public decimal Discount { get; set; }

        [JsonPropertyName("total")]
        public decimal Total { get; set; }

        [JsonPropertyName("amount_paid")]
        public decimal AmountPaid { get; set; }

        [JsonPropertyName("change_given")]
        public decimal ChangeGiven { get; set; }

        [JsonPropertyName("payment_method")]
        public string PaymentMethod { get; set; } = string.Empty;

        [JsonPropertyName("payment_summary")]
        public string PaymentSummary { get; set; } = string.Empty;

        [JsonPropertyName("cash_close_folio")]
        public string CashCloseFolio { get; set; } = string.Empty;

        [JsonPropertyName("sale_date")]
        public DateTime SaleDate { get; set; }

        [JsonPropertyName("ticket_data")]
        public byte[]? TicketData { get; set; }

        [JsonPropertyName("products")]
        public List<SaleProductPushDto> Products { get; set; } = new();
    }

    public class SaleProductPushDto
    {
        [JsonPropertyName("product_id")]
        public int ProductId { get; set; }

        [JsonPropertyName("barcode")]
        public string Barcode { get; set; } = string.Empty;

        [JsonPropertyName("product_name")]
        public string ProductName { get; set; } = string.Empty;

        [JsonPropertyName("quantity")]
        public int Quantity { get; set; }

        [JsonPropertyName("list_price")]
        public decimal ListPrice { get; set; }

        [JsonPropertyName("final_unit_price")]
        public decimal FinalUnitPrice { get; set; }

        [JsonPropertyName("line_total")]
        public decimal LineTotal { get; set; }

        [JsonPropertyName("total_discount_amount")]
        public decimal TotalDiscountAmount { get; set; }

        [JsonPropertyName("price_type")]
        public string PriceType { get; set; } = string.Empty;

        [JsonPropertyName("discount_info")]
        public string DiscountInfo { get; set; } = string.Empty;

        [JsonPropertyName("pricing_data")]
        public byte[]? PricingData { get; set; }
    }
}