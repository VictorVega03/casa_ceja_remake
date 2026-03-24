using System.Collections.Generic;
using System.Text.Json.Serialization;
using System;

namespace CasaCejaRemake.Models.DTOs
{
    public class StockOutputPushDto
    {
        [JsonPropertyName("folio")]
        public string Folio { get; set; } = string.Empty;

        [JsonPropertyName("origin_branch_id")]
        public int OriginBranchId { get; set; }

        [JsonPropertyName("destination_branch_id")]
        public int? DestinationBranchId { get; set; }

        [JsonPropertyName("user_id")]
        public int UserId { get; set; }

        [JsonPropertyName("type")]
        public string Type { get; set; } = "OTHER";

        [JsonPropertyName("total_amount")]
        public decimal TotalAmount { get; set; }

        [JsonPropertyName("output_date")]
        public DateTime OutputDate { get; set; }

        [JsonPropertyName("notes")]
        public string? Notes { get; set; }

        [JsonPropertyName("products")]
        public List<OutputProductPushDto> Products { get; set; } = new();
    }

    public class OutputProductPushDto
    {
        [JsonPropertyName("product_id")]
        public int ProductId { get; set; }

        [JsonPropertyName("barcode")]
        public string Barcode { get; set; } = string.Empty;

        [JsonPropertyName("product_name")]
        public string ProductName { get; set; } = string.Empty;

        [JsonPropertyName("quantity")]
        public int Quantity { get; set; }

        [JsonPropertyName("unit_cost")]
        public decimal UnitCost { get; set; }

        [JsonPropertyName("line_total")]
        public decimal LineTotal { get; set; }
    }
}