using System.Collections.Generic;
using System.Text.Json.Serialization;
using System;

namespace CasaCejaRemake.Models.DTOs
{
    public class LayawayPushDto
    {
        [JsonPropertyName("folio")]
        public string Folio { get; set; } = string.Empty;

        [JsonPropertyName("branch_id")]
        public int BranchId { get; set; }

        [JsonPropertyName("user_id")]
        public int UserId { get; set; }

        [JsonPropertyName("delivery_user_id")]
        public int? DeliveryUserId { get; set; }

        [JsonPropertyName("customer_id")]
        public int CustomerId { get; set; }

        [JsonPropertyName("total")]
        public decimal Total { get; set; }

        [JsonPropertyName("total_paid")]
        public decimal TotalPaid { get; set; }

        [JsonPropertyName("layaway_date")]
        public DateTime LayawayDate { get; set; }

        [JsonPropertyName("pickup_date")]
        public DateTime? PickupDate { get; set; }

        [JsonPropertyName("delivery_date")]
        public DateTime? DeliveryDate { get; set; }

        [JsonPropertyName("status")]
        public int Status { get; set; }

        [JsonPropertyName("notes")]
        public string? Notes { get; set; }

        [JsonPropertyName("ticket_data")]
        public byte[]? TicketData { get; set; }

        [JsonPropertyName("products")]
        public List<LayawayProductPushDto> Products { get; set; } = new();
    }

    public class LayawayProductPushDto
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
}