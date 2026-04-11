using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace CasaCejaRemake.Models.DTOs
{
    // ── Request: registrar salida/traspaso ───────────────────────────────
    public class StockOutputRequest
    {
        [JsonPropertyName("branch_id")]
        public int BranchId { get; set; }

        [JsonPropertyName("folio")]
        public string Folio { get; set; } = string.Empty;

        [JsonPropertyName("destination_branch_id")]
        public int DestinationBranchId { get; set; }

        [JsonPropertyName("user_id")]
        public int UserId { get; set; }

        [JsonPropertyName("total_amount")]
        public decimal TotalAmount { get; set; }

        [JsonPropertyName("output_date")]
        public DateTime OutputDate { get; set; }

        [JsonPropertyName("notes")]
        public string? Notes { get; set; }

        [JsonPropertyName("products")]
        public List<StockOutputProductRequest> Products { get; set; } = new();
    }

    public class StockOutputProductRequest
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

    // ── Response: traspasos pendientes ───────────────────────────────────
    public class PendingTransferDto
    {
        [JsonPropertyName("id")]
        public int Id { get; set; }

        [JsonPropertyName("folio")]
        public string Folio { get; set; } = string.Empty;

        [JsonPropertyName("folio_output")]
        public string FolioOutput { get; set; } = string.Empty;

        [JsonPropertyName("origin_branch_id")]
        public int OriginBranchId { get; set; }

        [JsonPropertyName("origin_branch_name")]
        public string OriginBranchName { get; set; } = string.Empty;

        [JsonPropertyName("entry_date")]
        public DateTime EntryDate { get; set; }

        [JsonPropertyName("total_amount")]
        public decimal TotalAmount { get; set; }

        [JsonPropertyName("notes")]
        public string? Notes { get; set; }

        [JsonPropertyName("products")]
        public List<PendingTransferProductDto> Products { get; set; } = new();
    }

    public class PendingTransferProductDto
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
    }

    // ── Request: confirmar traspaso con cantidades reales ────────────────
    public class ConfirmTransferRequest
    {
        [JsonPropertyName("confirmed_by_user_id")]
        public int ConfirmedByUserId { get; set; }

        [JsonPropertyName("products")]
        public List<ConfirmTransferProductRequest> Products { get; set; } = new();
    }

    public class ConfirmTransferProductRequest
    {
        [JsonPropertyName("product_id")]
        public int ProductId { get; set; }

        [JsonPropertyName("quantity")]
        public int Quantity { get; set; }
    }

    // ── Response genérica de folio aceptado ──────────────────────────────
    public class FolioResponse
    {
        [JsonPropertyName("folio")]
        public string Folio { get; set; } = string.Empty;
    }
}