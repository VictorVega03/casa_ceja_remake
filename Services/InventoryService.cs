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
        private readonly BaseRepository<StockEntry> _entryRepo;
        private readonly BaseRepository<StockOutput> _outputRepo;
        private readonly BaseRepository<EntryProduct> _entryProductRepo;
        private readonly BaseRepository<OutputProduct> _outputProductRepo;
        private readonly BaseRepository<Supplier> _supplierRepo;
        private readonly BaseRepository<Branch> _branchRepo;

        public InventoryService(
            ProductRepository productRepository,
            BaseRepository<Category> categoryRepository,
            BaseRepository<Unit> unitRepository,
            BaseRepository<ProductStock> productStockRepository,
            BaseRepository<StockEntry> entryRepo,
            BaseRepository<StockOutput> outputRepo,
            BaseRepository<EntryProduct> entryProductRepo,
            BaseRepository<OutputProduct> outputProductRepo,
            BaseRepository<Supplier> supplierRepo,
            BaseRepository<Branch> branchRepo)
        {
            _productRepository = productRepository;
            _categoryRepository = categoryRepository;
            _unitRepository = unitRepository;
            _productStockRepository = productStockRepository;
            _entryRepo = entryRepo;
            _outputRepo = outputRepo;
            _entryProductRepo = entryProductRepo;
            _outputProductRepo = outputProductRepo;
            _supplierRepo = supplierRepo;
            _branchRepo = branchRepo;
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

        public async Task<int> SaveCategoryAsync(Category category)
        {
            category.UpdatedAt = DateTime.Now;
            category.SyncStatus = 1;

            if (category.Id == 0)
            {
                category.CreatedAt = DateTime.Now;
                return await _categoryRepository.AddAsync(category);
            }
            else
            {
                return await _categoryRepository.UpdateAsync(category);
            }
        }

        public async Task<bool> DeleteCategoryAsync(int id)
        {
            var category = await _categoryRepository.GetByIdAsync(id);
            if (category == null) return false;

            // Optional: verify if used in products
            var products = await _productRepository.FindAsync(p => p.CategoryId == id);
            if (products.Count > 0) return false;

            return await _categoryRepository.DeleteAsync(category) > 0;
        }

        public async Task<int> SaveUnitAsync(Unit unit)
        {
            unit.UpdatedAt = DateTime.Now;
            unit.SyncStatus = 1;

            if (unit.Id == 0)
            {
                unit.CreatedAt = DateTime.Now;
                return await _unitRepository.AddAsync(unit);
            }
            else
            {
                return await _unitRepository.UpdateAsync(unit);
            }
        }

        public async Task<bool> DeleteUnitAsync(int id)
        {
            var unit = await _unitRepository.GetByIdAsync(id);
            if (unit == null) return false;

            // Optional: verify if used in products
            var products = await _productRepository.FindAsync(p => p.UnitId == id);
            if (products.Count > 0) return false;

            return await _unitRepository.DeleteAsync(unit) > 0;
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

        // ============================
        // PROVEEDORES
        // ============================

        public async Task<List<Supplier>> GetSuppliersAsync()
        {
            return await _supplierRepo.FindAsync(s => s.Active);
        }

        // ============================
        // CREAR ENTRADA
        // ============================

        /// <summary>
        /// Persiste una entrada de mercancía completa y actualiza el stock de la sucursal.
        /// </summary>
        public async Task<int> CreateEntryAsync(StockEntry entry, List<EntryProduct> products)
        {
            entry.CreatedAt = DateTime.Now;
            entry.UpdatedAt = DateTime.Now;
            entry.SyncStatus = 1;

            var entryId = await _entryRepo.AddAsync(entry);

            foreach (var product in products)
            {
                product.EntryId = entryId;
                product.CreatedAt = DateTime.Now;
                await _entryProductRepo.AddAsync(product);
            }

            await UpdateStockOnEntryAsync(products, entry.BranchId);
            return entryId;
        }

        private async Task UpdateStockOnEntryAsync(List<EntryProduct> products, int branchId)
        {
            foreach (var item in products)
            {
                var existing = await _productStockRepository.FindAsync(
                    x => x.ProductId == item.ProductId && x.BranchId == branchId);

                if (existing.Count > 0)
                {
                    var stock = existing[0];
                    stock.Quantity += item.Quantity;
                    stock.UpdatedAt = DateTime.Now;
                    await _productStockRepository.UpdateAsync(stock);
                }
                else
                {
                    await _productStockRepository.AddAsync(new ProductStock
                    {
                        ProductId = item.ProductId,
                        BranchId = branchId,
                        Quantity = item.Quantity,
                        UpdatedAt = DateTime.Now
                    });
                }
            }
        }

        // ============================
        // HISTORIAL Y DETALLES
        // ============================
        public async Task<List<StockEntry>> GetEntriesAsync(int branchId, DateTime startDate, DateTime endDate)
        {
            var endOfDay = endDate.Date.AddDays(1).AddSeconds(-1);
            return await _entryRepo.FindAsync(x => x.BranchId == branchId && x.EntryDate >= startDate.Date && x.EntryDate <= endOfDay);
        }

        public async Task<List<StockOutput>> GetOutputsAsync(int branchId, DateTime startDate, DateTime endDate)
        {
            var endOfDay = endDate.Date.AddDays(1).AddSeconds(-1);
            return await _outputRepo.FindAsync(x => x.OriginBranchId == branchId && x.OutputDate >= startDate.Date && x.OutputDate <= endOfDay);
        }

        public async Task<List<EntryProduct>> GetEntryProductsAsync(int entryId)
        {
            return await _entryProductRepo.FindAsync(x => x.EntryId == entryId);
        }

        public async Task<List<OutputProduct>> GetOutputProductsAsync(int outputId)
        {
            return await _outputProductRepo.FindAsync(x => x.OutputId == outputId);
        }

        public async Task<string> GetSupplierNameAsync(int supplierId)
        {
            if (supplierId <= 0) return "Desconocido";
            var s = await _supplierRepo.GetByIdAsync(supplierId);
            return s?.Name ?? "Desconocido";
        }

        public async Task<string> GetBranchNameAsync(int branchId)
        {
            if (branchId <= 0) return "Desconocido";
            var b = await _branchRepo.GetByIdAsync(branchId);
            return b?.Name ?? "Desconocido";
        }

        public async Task<List<Branch>> GetBranchesAsync()
        {
            return await _branchRepo.FindAsync(b => b.Active);
        }

        // ============================
        // CREAR SALIDA / TRASPASO
        // ============================

        /// <summary>
        /// Persiste una salida de mercancía y descuenta el stock de la sucursal origen.
        /// </summary>
        public async Task<int> CreateOutputAsync(StockOutput output, List<OutputProduct> products)
        {
            output.CreatedAt = DateTime.Now;
            output.UpdatedAt = DateTime.Now;
            output.SyncStatus = 1;

            var outputId = await _outputRepo.AddAsync(output);

            foreach (var product in products)
            {
                product.OutputId = outputId;
                product.CreatedAt = DateTime.Now;
                await _outputProductRepo.AddAsync(product);
            }

            await DeductStockOnOutputAsync(products, output.OriginBranchId);
            return outputId;
        }

        private async Task DeductStockOnOutputAsync(List<OutputProduct> products, int branchId)
        {
            foreach (var item in products)
            {
                var existing = await _productStockRepository.FindAsync(
                    x => x.ProductId == item.ProductId && x.BranchId == branchId);

                if (existing.Count > 0)
                {
                    var stock = existing[0];
                    stock.Quantity = Math.Max(0, stock.Quantity - item.Quantity);
                    stock.UpdatedAt = DateTime.Now;
                    await _productStockRepository.UpdateAsync(stock);
                }
                // If no stock record exists, nothing to deduct
            }
        }

        // ============================
        // CONFIRMAR ENTRADA
        // ============================

        /// <summary>
        /// Devuelve entradas sin confirmar para la sucursal destino.
        /// </summary>
        public async Task<List<StockEntry>> GetPendingEntriesAsync(int branchId)
        {
            return await _entryRepo.FindAsync(
                x => x.BranchId == branchId && x.ConfirmedAt == null);
        }

        /// <summary>
        /// Marca una entrada como confirmada por el usuario.
        /// </summary>
        public async Task ConfirmEntryAsync(int entryId, int confirmedByUserId)
        {
            var entry = await _entryRepo.GetByIdAsync(entryId);
            if (entry == null) return;

            entry.ConfirmedByUserId = confirmedByUserId;
            entry.ConfirmedAt = DateTime.Now;
            entry.UpdatedAt = DateTime.Now;
            await _entryRepo.UpdateAsync(entry);
        }
    }
}
