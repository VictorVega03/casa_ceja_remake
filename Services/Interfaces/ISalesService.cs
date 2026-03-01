using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using CasaCejaRemake.Models;
using CasaCejaRemake.Models.Results;

namespace CasaCejaRemake.Services.Interfaces
{
    /// <summary>
    /// Contrato público del servicio de ventas.
    /// </summary>
    public interface ISalesService
    {
        Task<StockValidationResult> ValidateStockAsync(List<CartItem> items);
        Task<int> GetNextConsecutiveAsync(int branchId);
        Task<SaleResult> ProcessSaleAsync(
            List<CartItem> items,
            PaymentMethod paymentMethod,
            decimal amountPaid,
            int userId,
            string userName,
            int branchId,
            decimal generalDiscount = 0,
            decimal generalDiscountPercent = 0,
            bool isGeneralDiscountPercentage = true);
        Task<SaleResult> ProcessSaleWithMixedPaymentAsync(
            List<CartItem> items,
            string paymentJson,
            decimal totalPaid,
            decimal changeGiven,
            int userId,
            string userName,
            int branchId,
            decimal generalDiscount = 0,
            decimal generalDiscountPercent = 0,
            bool isGeneralDiscountPercentage = true);
        Task<List<Product>> SearchProductsAsync(string searchTerm, int? categoryId = null);
        Task<Product?> GetProductByCodeAsync(string barcode);
        Task<CartItem?> CreateCartItemAsync(int productId, int quantity, int userId);
        Task<CartItem?> CreateCartItemWithPriceTypeAsync(int productId, int quantity, int userId, PriceType priceType);
        Task<(bool Success, string Message)> ApplySpecialPriceAsync(CartItem item);
        Task<(bool Success, string Message)> ApplyDealerPriceAsync(CartItem item);
        Task<(bool Success, string Message)> RevertToRetailPriceAsync(CartItem item);
        Task<Product?> GetProductByIdAsync(int productId);
        Task<List<Sale>> GetDailySalesAsync(int branchId);
        Task<decimal> GetDailySalesTotalAsync(int branchId);
        Task<List<Category>> GetCategoriesAsync();
        Task<List<Unit>> GetUnitsAsync();
        Task<List<Product>> SearchProductsWithUnitAsync(string searchTerm, int? categoryId = null, int? unitId = null);
        Task<List<Sale>> GetSalesHistoryPagedAsync(
            int branchId,
            int page,
            int pageSize,
            DateTime? startDate = null,
            DateTime? endDate = null);
        Task<int> GetSalesCountAsync(int branchId, DateTime? startDate = null, DateTime? endDate = null);
        Task<List<SaleProduct>> GetSaleProductsAsync(int saleId);
        Task<string?> GetUserNameAsync(int userId);
        Task<TicketData?> RecoverTicketAsync(int saleId);
    }
}
