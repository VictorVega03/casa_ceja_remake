using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using CasaCejaRemake.Data.Repositories;
using CasaCejaRemake.Models;

namespace CasaCejaRemake.Services
{
    public class InventoryService
    {
        private readonly ProductRepository _productRepository;
        private readonly BaseRepository<Category> _categoryRepository;
        private readonly BaseRepository<Unit> _unitRepository;
        private readonly BaseRepository<ProductStock> _productStockRepository;

        public InventoryService(
            ProductRepository productRepository,
            BaseRepository<Category> categoryRepository,
            BaseRepository<Unit> unitRepository,
            BaseRepository<ProductStock> productStockRepository)
        {
            _productRepository = productRepository;
            _categoryRepository = categoryRepository;
            _unitRepository = unitRepository;
            _productStockRepository = productStockRepository;
        }

        // ============================
        // PRODUCTOS (CATÁLOGO UNIVERSAL)
        // ============================

        public async Task<List<Product>> SearchProductsAsync(string searchTerm, int? categoryId = null, int? unitId = null)
        {
            var results = await _productRepository.SearchAsync(searchTerm, categoryId, unitId);

            // Cargar nombres de categorías y unidades para mostrar
            var categories = await _categoryRepository.GetAllAsync();
            var units = await _unitRepository.GetAllAsync();

            var categoryDict = new Dictionary<int, string>();
            foreach (var c in categories) categoryDict[c.Id] = c.Name;

            var unitDict = new Dictionary<int, string>();
            foreach (var u in units) unitDict[u.Id] = u.Name;

            foreach (var product in results)
            {
                product.CategoryName = categoryDict.TryGetValue(product.CategoryId, out var catName) ? catName : "";
                product.UnitName = unitDict.TryGetValue(product.UnitId, out var unitName) ? unitName : "";
            }

            return results;
        }

        public async Task<Product?> GetProductByIdAsync(int id)
        {
            return await _productRepository.GetByIdAsync(id);
        }

        public async Task<int> SaveProductAsync(Product product)
        {
            product.UpdatedAt = DateTime.Now;
            product.SyncStatus = 1;

            if (product.Id == 0)
            {
                product.CreatedAt = DateTime.Now;
                return await _productRepository.AddAsync(product);
            }
            else
            {
                return await _productRepository.UpdateAsync(product);
            }
        }

        public async Task<bool> IsBarcodeUniqueAsync(string barcode, int currentProductId = 0)
        {
            var existing = await _productRepository.GetByBarcodeAsync(barcode);
            if (existing == null) return true;
            return existing.Id == currentProductId;
        }

        // ============================
        // CATEGORÍAS Y MEDIDAS
        // ============================

        public async Task<List<Category>> GetCategoriesAsync()
        {
            return await _categoryRepository.GetAllAsync();
        }

        public async Task<List<Unit>> GetUnitsAsync()
        {
            return await _unitRepository.GetAllAsync();
        }

        // ============================
        // STOCK POR SUCURSAL
        // ============================
        public async Task<int> GetProductStockAsync(int productId, int branchId)
        {
            var stockList = await _productStockRepository.FindAsync(x => x.ProductId == productId && x.BranchId == branchId);
            if (stockList.Count > 0)
                return stockList[0].Quantity;
            return 0;
        }

        public async Task SetProductStockAsync(int productId, int branchId, int quantity)
        {
            var stockList = await _productStockRepository.FindAsync(x => x.ProductId == productId && x.BranchId == branchId);
            if (stockList.Count > 0)
            {
                var stock = stockList[0];
                stock.Quantity = quantity;
                await _productStockRepository.UpdateAsync(stock);
            }
            else
            {
                var stock = new ProductStock
                {
                    ProductId = productId,
                    BranchId = branchId,
                    Quantity = quantity
                };
                await _productStockRepository.AddAsync(stock);
            }
        }
    }
}
