using System.Collections.Generic;
using System.Text.Json.Serialization;
using System;
using CasaCejaRemake.Models;

namespace CasaCejaRemake.Models.DTOs
{
    public class StockEntryPushDto
    {
        [JsonPropertyName("folio")]
        public string Folio { get; set; } = string.Empty;

        [JsonPropertyName("folio_output")]
        public string? FolioOutput { get; set; }

        [JsonPropertyName("branch_id")]
        public int BranchId { get; set; }

        [JsonPropertyName("supplier_id")]
        public int SupplierId { get; set; }

        [JsonPropertyName("user_id")]
        public int UserId { get; set; }

        [JsonPropertyName("entry_type")]
        public string EntryType { get; set; } = StockEntryType.Purchase;

        [JsonPropertyName("total_amount")]
        public decimal TotalAmount { get; set; }

        [JsonPropertyName("entry_date")]
        public DateTime EntryDate { get; set; }

        [JsonPropertyName("notes")]
        public string? Notes { get; set; }

        [JsonPropertyName("products")]
        public List<EntryProductPushDto> Products { get; set; } = new();
    }

    public class EntryProductPushDto
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