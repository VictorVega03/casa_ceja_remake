using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace CasaCejaRemake.Models.DTOs
{
    public class ProductStockDto
    {
        [JsonPropertyName("id")]
        public int Id { get; set; }

        [JsonPropertyName("product_id")]
        public int ProductId { get; set; }

        [JsonPropertyName("branch_id")]
        public int BranchId { get; set; }

        [JsonPropertyName("quantity")]
        public decimal Quantity { get; set; }

        [JsonPropertyName("product")]
        public ProductStockProductDto? Product { get; set; }

        [JsonPropertyName("branch")]
        public ProductStockBranchDto? Branch { get; set; }
    }

    public class ProductStockProductDto
    {
        [JsonPropertyName("id")]
        public int Id { get; set; }

        [JsonPropertyName("barcode")]
        public string Barcode { get; set; } = string.Empty;

        [JsonPropertyName("name")]
        public string Name { get; set; } = string.Empty;

        [JsonPropertyName("price_retail")]
        public decimal PriceRetail { get; set; }

        [JsonPropertyName("category_id")]
        public int? CategoryId { get; set; }
    }

    public class ProductStockBranchDto
    {
        [JsonPropertyName("id")]
        public int Id { get; set; }

        [JsonPropertyName("name")]
        public string Name { get; set; } = string.Empty;
    }

    public class PagedProductStockResponse
    {
        [JsonPropertyName("data")]
        public List<ProductStockDto> Data { get; set; } = new();

        [JsonPropertyName("total")]
        public int Total { get; set; }

        [JsonPropertyName("current_page")]
        public int CurrentPage { get; set; }

        [JsonPropertyName("last_page")]
        public int LastPage { get; set; }
    }
}
