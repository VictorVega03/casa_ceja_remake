using System.Collections.Generic;
using System.Text.Json.Serialization;
using System;

namespace CasaCejaRemake.Models.DTOs
{
    public class CashClosePushDto
    {
        [JsonPropertyName("folio")]
        public string Folio { get; set; } = string.Empty;

        [JsonPropertyName("branch_id")]
        public int BranchId { get; set; }

        [JsonPropertyName("user_id")]
        public int UserId { get; set; }

        [JsonPropertyName("opening_cash")]
        public decimal OpeningCash { get; set; }

        [JsonPropertyName("total_cash")]
        public decimal TotalCash { get; set; }

        [JsonPropertyName("total_debit_card")]
        public decimal TotalDebitCard { get; set; }

        [JsonPropertyName("total_credit_card")]
        public decimal TotalCreditCard { get; set; }

        [JsonPropertyName("total_checks")]
        public decimal TotalChecks { get; set; }

        [JsonPropertyName("total_transfers")]
        public decimal TotalTransfers { get; set; }

        [JsonPropertyName("layaway_cash")]
        public decimal LayawayCash { get; set; }

        [JsonPropertyName("credit_cash")]
        public decimal CreditCash { get; set; }

        [JsonPropertyName("credit_total_created")]
        public decimal CreditTotalCreated { get; set; }

        [JsonPropertyName("layaway_total_created")]
        public decimal LayawayTotalCreated { get; set; }

        [JsonPropertyName("expenses")]
        public string Expenses { get; set; } = "[]";

        [JsonPropertyName("income")]
        public string Income { get; set; } = "[]";

        [JsonPropertyName("surplus")]
        public decimal Surplus { get; set; }

        [JsonPropertyName("expected_cash")]
        public decimal ExpectedCash { get; set; }

        [JsonPropertyName("total_sales")]
        public decimal TotalSales { get; set; }

        [JsonPropertyName("notes")]
        public string Notes { get; set; } = string.Empty;

        [JsonPropertyName("opening_date")]
        public DateTime OpeningDate { get; set; }

        [JsonPropertyName("close_date")]
        public DateTime CloseDate { get; set; }

        [JsonPropertyName("movements")]
        public List<CashMovementPushDto> Movements { get; set; } = new();
    }

    public class CashMovementPushDto
    {
        [JsonPropertyName("type")]
        public string Type { get; set; } = string.Empty;

        [JsonPropertyName("concept")]
        public string Concept { get; set; } = string.Empty;

        [JsonPropertyName("amount")]
        public decimal Amount { get; set; }

        [JsonPropertyName("user_id")]
        public int UserId { get; set; }
    }
}