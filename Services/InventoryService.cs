using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using CasaCejaRemake.Data.Repositories;
using CasaCejaRemake.Models;
using CasaCejaRemake.Models.DTOs;

namespace CasaCejaRemake.Services
{
    public class InventoryService
    {
        private readonly ProductRepository _productRepository;
        private readonly BaseRepository<Category> _categoryRepository;
        private readonly BaseRepository<Unit> _unitRepository;
        private readonly ApiClient _apiClient;
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
            BaseRepository<Branch> branchRepo,
            ApiClient apiClient)
        {
            _productRepository = productRepository;
            _categoryRepository = categoryRepository;
            _unitRepository = unitRepository;
            _apiClient = apiClient;
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

        public async Task<(bool Success, string Message, int Id)> SaveProductAsync(Product product)
        {
            product.UpdatedAt = DateTime.Now;

            var payload = new
            {
                barcode            = product.Barcode,
                name               = product.Name,
                category_id        = product.CategoryId > 0 ? (int?)product.CategoryId : null,
                unit_id            = product.UnitId > 0 ? (int?)product.UnitId : null,
                presentation       = product.Presentation,
                iva                = product.Iva,
                price_retail       = product.PriceRetail,
                price_wholesale    = product.PriceWholesale,
                wholesale_quantity = product.WholesaleQuantity,
                price_special      = product.PriceSpecial,
                price_dealer       = product.PriceDealer,
                active             = product.Active,
            };

            ApiResult<Product>? response;
            bool isNew = product.Id == 0;
            if (isNew)
            {
                product.CreatedAt = DateTime.Now;
                response = await _apiClient.PostAsync<Product>("/api/v1/admin/products", payload);
            }
            else
            {
                response = await _apiClient.PutAsync<Product>($"/api/v1/admin/products/{product.Id}", payload);
            }

            if (response?.IsNetworkError == true)
                return (false, "Sin conexión al servidor. Verifica tu red e intenta de nuevo.", 0);

            if (response?.IsServerError == true)
                return (false, response.ServerMessage ?? "No se pudo guardar el producto en el servidor.", 0);

            if (response?.IsSuccess != true || response.Data == null)
                return (false, response?.ServerMessage ?? "No se pudo guardar el producto en el servidor.", 0);

            // Servidor confirmó — guardar local con Id del servidor
            product.Id         = response.Data.Id;
            product.SyncStatus = 2;
            product.LastSync   = DateTime.Now;
            await _productRepository.UpsertAsync(product);
            return (true, response.ServerMessage ?? string.Empty, product.Id);
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

        public async Task<(bool Success, string Message, int Id)> SaveCategoryAsync(Category category)
        {
            category.UpdatedAt = DateTime.Now;

            var payload = new { name = category.Name, active = category.Active };
            ApiResult<Category>? response;
            bool isNew = category.Id == 0;
            if (isNew)
            {
                category.CreatedAt = DateTime.Now;
                response = await _apiClient.PostAsync<Category>("/api/v1/admin/categories", payload);
            }
            else
            {
                response = await _apiClient.PutAsync<Category>($"/api/v1/admin/categories/{category.Id}", payload);
            }

            if (response?.IsNetworkError == true)
                return (false, "Sin conexión al servidor. Verifica tu red e intenta de nuevo.", 0);

            if (response?.IsServerError == true)
                return (false, response.ServerMessage ?? "No se pudo guardar la categoría.", 0);

            if (response?.IsSuccess != true || response.Data == null)
                return (false, response?.ServerMessage ?? "No se pudo guardar la categoría.", 0);

            // Servidor confirmó — guardar local con Id del servidor
            category.Id         = response.Data.Id;
            category.SyncStatus = 2;
            category.LastSync   = DateTime.Now;
            await _categoryRepository.UpsertAsync(category);
            return (true, response.ServerMessage ?? string.Empty, category.Id);
        }

        public async Task<(bool Success, string Message)> DeactivateCategoryAsync(Category category)
        {
            if (_apiClient == null)
                return (false, "Sin conexión al servidor. Verifica tu red e intenta de nuevo.");

            var payload = new { name = category.Name, active = false };
            var response = await _apiClient.PutAsync<Category>($"/api/v1/admin/categories/{category.Id}", payload);

            if (response?.IsNetworkError == true)
                return (false, "Sin conexión al servidor. Verifica tu red e intenta de nuevo.");

            if (response?.IsServerError == true || response?.IsSuccess != true)
                return (false, response?.ServerMessage ?? "No se pudo desactivar la categoría.");

            category.Active     = false;
            category.UpdatedAt  = DateTime.Now;
            category.SyncStatus = 2;
            category.LastSync   = DateTime.Now;
            await _categoryRepository.UpdateAsync(category);
            return (true, $"Categoría '{category.Name}' eliminada exitosamente.");
        }

        public async Task<bool> DeleteCategoryAsync(int id)
        {
            var category = await _categoryRepository.GetByIdAsync(id);
            if (category == null) return false;

            var products = await _productRepository.FindAsync(p => p.CategoryId == id);
            if (products.Count > 0) return false;

            return await _categoryRepository.DeleteAsync(category) > 0;
        }

        public async Task<(bool Success, string Message, int Id)> SaveUnitAsync(Unit unit)
        {
            unit.UpdatedAt = DateTime.Now;

            var payload = new { name = unit.Name, active = unit.Active };
            ApiResult<Unit>? response;
            bool isNew = unit.Id == 0;
            if (isNew)
            {
                unit.CreatedAt = DateTime.Now;
                response = await _apiClient.PostAsync<Unit>("/api/v1/admin/units", payload);
            }
            else
            {
                response = await _apiClient.PutAsync<Unit>($"/api/v1/admin/units/{unit.Id}", payload);
            }

            if (response?.IsNetworkError == true)
                return (false, "Sin conexión al servidor. Verifica tu red e intenta de nuevo.", 0);

            if (response?.IsServerError == true)
                return (false, response.ServerMessage ?? "No se pudo guardar la medida.", 0);

            if (response?.IsSuccess != true || response.Data == null)
                return (false, response?.ServerMessage ?? "No se pudo guardar la medida.", 0);

            // Servidor confirmó — guardar local con Id del servidor
            unit.Id         = response.Data.Id;
            unit.SyncStatus = 2;
            unit.LastSync   = DateTime.Now;
            await _unitRepository.UpsertAsync(unit);
            return (true, response.ServerMessage ?? string.Empty, unit.Id);
        }

        public async Task<(bool Success, string Message)> DeactivateUnitAsync(Unit unit)
        {
            if (_apiClient == null)
                return (false, "Sin conexión al servidor. Verifica tu red e intenta de nuevo.");

            var payload = new { name = unit.Name, active = false };
            var response = await _apiClient.PutAsync<Unit>($"/api/v1/admin/units/{unit.Id}", payload);

            if (response?.IsNetworkError == true)
                return (false, "Sin conexión al servidor. Verifica tu red e intenta de nuevo.");

            if (response?.IsServerError == true || response?.IsSuccess != true)
                return (false, response?.ServerMessage ?? "No se pudo desactivar la medida.");

            unit.Active     = false;
            unit.UpdatedAt  = DateTime.Now;
            unit.SyncStatus = 2;
            unit.LastSync   = DateTime.Now;
            await _unitRepository.UpdateAsync(unit);
            return (true, $"Medida '{unit.Name}' eliminada exitosamente.");
        }

        public async Task<bool> DeleteUnitAsync(int id)
        {
            var unit = await _unitRepository.GetByIdAsync(id);
            if (unit == null) return false;

            var products = await _productRepository.FindAsync(p => p.UnitId == id);
            if (products.Count > 0) return false;

            return await _unitRepository.DeleteAsync(unit) > 0;
        }

        // ============================
        // STOCK POR SUCURSAL
        // ============================
        public async Task<(List<ProductStockItem> Items, bool IsFromCache)> GetStockByProductAsync(int productId, string barcode)
        {
            try
            {
                var response = await _apiClient.GetAsync<System.Text.Json.JsonElement>(
                    $"/api/v1/stock/product/{barcode}");

                if (response?.IsSuccess == true)
                {
                    var stockArray = response.Data.GetProperty("stock");
                    foreach (var item in stockArray.EnumerateArray())
                    {
                        var branchId = item.GetProperty("branch_id").GetInt32();
                        var quantity = item.GetProperty("quantity").GetInt32();

                        var existing = await _productStockRepository.FindAsync(
                            x => x.ProductId == productId && x.BranchId == branchId);

                        if (existing.Count > 0)
                        {
                            existing[0].Quantity = quantity;
                            existing[0].UpdatedAt = DateTime.Now;
                            await _productStockRepository.UpdateAsync(existing[0]);
                        }
                        else
                        {
                            await _productStockRepository.AddAsync(new ProductStock
                            {
                                ProductId = productId,
                                BranchId = branchId,
                                Quantity = quantity,
                                UpdatedAt = DateTime.Now,
                            });
                        }
                    }

                    var result = new List<ProductStockItem>();
                    foreach (var item in stockArray.EnumerateArray())
                    {
                        result.Add(new ProductStockItem
                        {
                            BranchId = item.GetProperty("branch_id").GetInt32(),
                            BranchName = item.GetProperty("branch_name").GetString() ?? "",
                            Quantity = item.GetProperty("quantity").GetInt32(),
                        });
                    }

                    return (result, false);
                }
            }
            catch
            {
            }

            var cached = await _productStockRepository.FindAsync(x => x.ProductId == productId);
            var branchMap = await GetBranchNameMapAsync();
            var cachedResult = cached.Select(x => new ProductStockItem
            {
                BranchId = x.BranchId,
                BranchName = branchMap.TryGetValue(x.BranchId, out var name) ? name : $"Sucursal {x.BranchId}",
                Quantity = x.Quantity,
            }).ToList();

            return (cachedResult, true);
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
        /// Persiste una entrada de mercancía completa, actualiza el stock de la sucursal,
        /// e intenta subir al servidor inmediatamente. Si falla, queda con SyncStatus=1
        /// para que SyncService lo reintente después.
        /// </summary>
        public async Task<(int EntryId, bool SyncedToServer)> CreateEntryAsync(StockEntry entry, List<EntryProduct> products)
        {
            // Respetar SyncStatus si ya viene seteado (ej: TRANSFER confirmada = 2)
            var alreadySynced = entry.SyncStatus == 2;
            if (!alreadySynced)
            {
                entry.SyncStatus = 1;
            }

            entry.CreatedAt = DateTime.Now;
            entry.UpdatedAt = DateTime.Now;

            var entryId = await _entryRepo.AddAsync(entry);

            foreach (var product in products)
            {
                product.EntryId = entryId;
                product.CreatedAt = DateTime.Now;
                await _entryProductRepo.AddAsync(product);
                await UpdateStockOnEntryAsync(product.ProductId, entry.BranchId, product.Quantity);
            }

            // Solo intentar push inmediato para entradas PURCHASE (offline-first)
            // Las TRANSFER ya vienen del servidor, no necesitan push
            if (alreadySynced)
            {
                Console.WriteLine($"[CreateEntryAsync] Entrada {entry.Folio} (TRANSFER) guardada localmente — ya sincronizada");
                return (entryId, true);
            }

            var synced = await TryPushEntryToServerAsync(entry, products);
            if (synced)
            {
                entry.SyncStatus = 2;
                entry.LastSync = DateTime.Now;
                await _entryRepo.UpdateAsync(entry);
                Console.WriteLine($"[CreateEntryAsync] Entrada {entry.Folio} sincronizada con servidor");
            }
            else
            {
                Console.WriteLine($"[CreateEntryAsync] Entrada {entry.Folio} guardada offline (SyncStatus=1)");
            }

            return (entryId, synced);
        }

        /// <summary>
        /// Intenta subir una entrada PURCHASE al servidor usando el endpoint de sync push.
        /// Retorna true si el servidor aceptó el folio.
        /// </summary>
        private async Task<bool> TryPushEntryToServerAsync(StockEntry entry, List<EntryProduct> products)
        {
            try
            {
                var dto = new StockEntryPushDto
                {
                    Folio = entry.Folio,
                    FolioOutput = entry.FolioOutput,
                    BranchId = entry.BranchId,
                    SupplierId = entry.SupplierId,
                    UserId = entry.UserId,
                    EntryType = entry.EntryType,
                    TotalAmount = entry.TotalAmount,
                    EntryDate = entry.EntryDate,
                    Notes = entry.Notes,
                    Products = products.Select(p => new EntryProductPushDto
                    {
                        ProductId = p.ProductId,
                        Barcode = p.Barcode,
                        ProductName = p.ProductName,
                        Quantity = p.Quantity,
                        UnitCost = p.UnitCost,
                        LineTotal = p.LineTotal,
                    }).ToList()
                };

                var body = new { branch_id = entry.BranchId, records = new List<StockEntryPushDto> { dto } };
                var response = await _apiClient.PostAsync<PushResponse>("/api/v1/sync/push/stock-entries", body);

                if (response?.Data != null && response.Data.Accepted.Contains(entry.Folio))
                    return true;

                if (response?.Data?.Rejected != null)
                {
                    foreach (var r in response.Data.Rejected)
                        Console.WriteLine($"[TryPushEntryToServerAsync] Rechazado folio={r.Folio}: {r.Reason}");
                }

                return false;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[TryPushEntryToServerAsync] Error: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Suma la cantidad de un producto en el stock de la sucursal.
        /// Crea el registro si no existe todavía.
        /// </summary>
        private async Task UpdateStockOnEntryAsync(int productId, int branchId, int quantity)
        {
            Console.WriteLine($"[UpdateStockOnEntryAsync] Iniciando — ProductId={productId}, BranchId={branchId}, Quantity={quantity}");

            try
            {
                var existing = await _productStockRepository.FindAsync(
                    x => x.ProductId == productId && x.BranchId == branchId);

                Console.WriteLine($"[UpdateStockOnEntryAsync] Registros existentes encontrados: {existing.Count}");

                if (existing.Count > 0)
                {
                    existing[0].Quantity += quantity;
                    existing[0].UpdatedAt = DateTime.Now;
                    existing[0].SyncStatus = 1;
                    await _productStockRepository.UpdateAsync(existing[0]);
                    Console.WriteLine($"[UpdateStockOnEntryAsync] Stock actualizado — nuevo total: {existing[0].Quantity}");
                }
                else
                {
                    var newStock = new ProductStock
                    {
                        ProductId = productId,
                        BranchId = branchId,
                        Quantity = quantity,
                        UpdatedAt = DateTime.Now,
                        SyncStatus = 1,
                    };
                    var newId = await _productStockRepository.AddAsync(newStock);
                    Console.WriteLine($"[UpdateStockOnEntryAsync] Nuevo registro de stock creado con Id={newId}");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[UpdateStockOnEntryAsync] ERROR — {ex.Message}");
                throw;
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

        public async Task<List<StockEntry>> GetAllEntriesAsync(DateTime startDate, DateTime endDate)
        {
            var endOfDay = endDate.Date.AddDays(1).AddSeconds(-1);
            return await _entryRepo.FindAsync(x =>
                x.EntryDate >= startDate.Date && x.EntryDate <= endOfDay);
        }

        public async Task<List<StockEntry>> GetEntriesFromServerAsync(int branchId, DateTime startDate, DateTime endDate)
        {
            var all = await PullStockEntriesFromServerAsync(branchId);
            if (all.Count == 0)
                return await GetEntriesAsync(branchId, startDate, endDate);

            await CacheServerEntriesAsync(all);
            return await GetEntriesAsync(branchId, startDate, endDate);
        }

        public async Task<List<StockOutput>> GetOutputsAsync(int branchId, DateTime startDate, DateTime endDate)
        {
            var endOfDay = endDate.Date.AddDays(1).AddSeconds(-1);
            return await _outputRepo.FindAsync(x => x.OriginBranchId == branchId && x.OutputDate >= startDate.Date && x.OutputDate <= endOfDay);
        }

        public async Task<List<StockOutput>> GetAllOutputsAsync(DateTime startDate, DateTime endDate)
        {
            var endOfDay = endDate.Date.AddDays(1).AddSeconds(-1);
            return await _outputRepo.FindAsync(x =>
                x.OutputDate >= startDate.Date && x.OutputDate <= endOfDay);
        }

        public async Task<List<StockOutput>> GetOutputsFromServerAsync(int branchId, DateTime startDate, DateTime endDate)
        {
            var all = await PullStockOutputsFromServerAsync(branchId);
            if (all.Count == 0)
                return await GetOutputsAsync(branchId, startDate, endDate);

            await CacheServerOutputsAsync(all);
            return await GetOutputsAsync(branchId, startDate, endDate);
        }

        public async Task<List<StockOutput>> GetOutputsInvolvingBranchFromServerAsync(int branchId, DateTime startDate, DateTime endDate)
        {
            var all = await PullStockOutputsFromServerAsync(branchId);
            if (all.Count > 0)
                await CacheServerOutputsAsync(all);

            var endOfDay = endDate.Date.AddDays(1).AddSeconds(-1);
            return await _outputRepo.FindAsync(x =>
                (x.OriginBranchId == branchId || x.DestinationBranchId == branchId)
                && x.OutputDate >= startDate.Date
                && x.OutputDate <= endOfDay);
        }

        private async Task CacheServerEntriesAsync(List<StockEntry> entries)
        {
            foreach (var entry in entries.Where(e => !string.IsNullOrWhiteSpace(e.Folio)))
            {
                var existing = (await _entryRepo.FindAsync(e => e.Folio == entry.Folio)).FirstOrDefault();
                entry.Id = existing?.Id ?? 0;
                entry.SyncStatus = 2;
                entry.LastSync = DateTime.Now;

                if (existing == null)
                    await _entryRepo.AddAsync(entry);
                else
                    await _entryRepo.UpdateAsync(entry);
            }
        }

        private async Task CacheServerOutputsAsync(List<StockOutput> outputs)
        {
            foreach (var output in outputs.Where(o => !string.IsNullOrWhiteSpace(o.Folio)))
            {
                var existing = (await _outputRepo.FindAsync(o => o.Folio == output.Folio)).FirstOrDefault();
                output.Id = existing?.Id ?? 0;
                output.SyncStatus = 2;
                output.LastSync = DateTime.Now;

                if (existing == null)
                    await _outputRepo.AddAsync(output);
                else
                    await _outputRepo.UpdateAsync(output);
            }
        }

        private async Task<List<StockEntry>> PullStockEntriesFromServerAsync(int branchId)
        {
            var elements = await PullJsonPagesAsync("stock-entries", branchId);
            return elements.Select(MapStockEntry).ToList();
        }

        private async Task<List<StockOutput>> PullStockOutputsFromServerAsync(int branchId)
        {
            var elements = await PullJsonPagesAsync("stock-outputs", branchId);
            return elements.Select(MapStockOutput).ToList();
        }

        private async Task<List<JsonElement>> PullJsonPagesAsync(string entity, int branchId)
        {
            var result = new List<JsonElement>();
            var page = 1;

            try
            {
                while (true)
                {
                    var endpoint = $"/api/v1/sync/pull/{entity}?since=0&page={page}&branch_id={branchId}";
                    var response = await _apiClient.GetAsync<JsonElement>(endpoint);

                    if (response?.IsSuccess != true)
                        break;

                    var data = response.Data;
                    if (!data.TryGetProperty("data", out var items) || items.ValueKind != JsonValueKind.Array)
                        break;

                    foreach (var item in items.EnumerateArray())
                    {
                        result.Add(item.Clone());
                    }

                    var currentPage = GetInt(data, "current_page", page);
                    var lastPage = GetInt(data, "last_page", currentPage);
                    if (currentPage >= lastPage)
                        break;

                    page++;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[InventoryService] Error leyendo {entity} desde servidor: {ex.Message}");
            }

            return result;
        }

        private static StockEntry MapStockEntry(JsonElement item)
        {
            return new StockEntry
            {
                Id = GetInt(item, "id", 0),
                Folio = GetString(item, "folio"),
                FolioOutput = GetNullableString(item, "folio_output"),
                BranchId = GetInt(item, "branch_id", 0),
                SupplierId = GetInt(item, "supplier_id", 0),
                UserId = GetInt(item, "user_id", 0),
                TotalAmount = GetDecimal(item, "total_amount", 0),
                EntryDate = GetDateTime(item, "entry_date", DateTime.Now),
                EntryType = GetString(item, "entry_type", StockEntryType.Purchase),
                Notes = GetNullableString(item, "notes"),
                ConfirmedByUserId = GetNullableInt(item, "confirmed_by_user_id"),
                ConfirmedAt = GetNullableDateTime(item, "confirmed_at"),
                CreatedAt = GetDateTime(item, "created_at", DateTime.Now),
                UpdatedAt = GetDateTime(item, "updated_at", DateTime.Now),
                SyncStatus = 2,
                LastSync = DateTime.Now,
            };
        }

        private static StockOutput MapStockOutput(JsonElement item)
        {
            return new StockOutput
            {
                Id = GetInt(item, "id", 0),
                Folio = GetString(item, "folio"),
                OriginBranchId = GetInt(item, "origin_branch_id", 0),
                DestinationBranchId = GetInt(item, "destination_branch_id", 0),
                UserId = GetInt(item, "user_id", 0),
                TotalAmount = GetDecimal(item, "total_amount", 0),
                OutputDate = GetDateTime(item, "output_date", DateTime.Now),
                Notes = GetNullableString(item, "notes"),
                Status = GetString(item, "status", "PENDING"),
                ConfirmedByUserId = GetNullableInt(item, "confirmed_by_user_id"),
                ConfirmedAt = GetNullableDateTime(item, "confirmed_at"),
                CreatedAt = GetDateTime(item, "created_at", DateTime.Now),
                UpdatedAt = GetDateTime(item, "updated_at", DateTime.Now),
                SyncStatus = 2,
                LastSync = DateTime.Now,
            };
        }

        private static int GetInt(JsonElement item, string propertyName, int fallback)
        {
            var value = GetProperty(item, propertyName);
            if (value == null || value.Value.ValueKind == JsonValueKind.Null)
                return fallback;

            if (value.Value.ValueKind == JsonValueKind.Number && value.Value.TryGetInt32(out var number))
                return number;

            if (value.Value.ValueKind == JsonValueKind.String &&
                int.TryParse(value.Value.GetString(), out var parsed))
                return parsed;

            return fallback;
        }

        private static int? GetNullableInt(JsonElement item, string propertyName)
        {
            var value = GetProperty(item, propertyName);
            if (value == null || value.Value.ValueKind == JsonValueKind.Null)
                return null;

            if (value.Value.ValueKind == JsonValueKind.Number && value.Value.TryGetInt32(out var number))
                return number;

            if (value.Value.ValueKind == JsonValueKind.String &&
                int.TryParse(value.Value.GetString(), out var parsed))
                return parsed;

            return null;
        }

        private static decimal GetDecimal(JsonElement item, string propertyName, decimal fallback)
        {
            var value = GetProperty(item, propertyName);
            if (value == null || value.Value.ValueKind == JsonValueKind.Null)
                return fallback;

            if (value.Value.ValueKind == JsonValueKind.Number && value.Value.TryGetDecimal(out var number))
                return number;

            if (value.Value.ValueKind == JsonValueKind.String &&
                decimal.TryParse(value.Value.GetString(), out var parsed))
                return parsed;

            return fallback;
        }

        private static string GetString(JsonElement item, string propertyName, string fallback = "")
        {
            return GetNullableString(item, propertyName) ?? fallback;
        }

        private static string? GetNullableString(JsonElement item, string propertyName)
        {
            var value = GetProperty(item, propertyName);
            if (value == null || value.Value.ValueKind == JsonValueKind.Null)
                return null;

            return value.Value.ValueKind == JsonValueKind.String
                ? value.Value.GetString()
                : value.Value.ToString();
        }

        private static DateTime GetDateTime(JsonElement item, string propertyName, DateTime fallback)
        {
            return GetNullableDateTime(item, propertyName) ?? fallback;
        }

        private static DateTime? GetNullableDateTime(JsonElement item, string propertyName)
        {
            var value = GetProperty(item, propertyName);
            if (value == null || value.Value.ValueKind == JsonValueKind.Null)
                return null;

            if (value.Value.ValueKind == JsonValueKind.String &&
                DateTime.TryParse(value.Value.GetString(), out var parsed))
                return parsed;

            return null;
        }

        private static JsonElement? GetProperty(JsonElement item, string propertyName)
        {
            return item.TryGetProperty(propertyName, out var value) ? value : null;
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
            if (output.SyncStatus != 2)
                output.SyncStatus = 1;

            var outputId = await _outputRepo.AddAsync(output);

            foreach (var product in products)
            {
                product.OutputId = outputId;
                product.CreatedAt = DateTime.Now;
                await _outputProductRepo.AddAsync(product);
                await UpdateStockOnOutputAsync(product.ProductId, output.OriginBranchId, product.Quantity);
            }

            return outputId;
        }

        /// <summary>
        /// Resta la cantidad de un producto en el stock de la sucursal origen.
        /// El stock puede quedar negativo — es una regla de negocio del cliente:
        /// el sistema nunca bloquea ventas ni traspasos por stock insuficiente.
        /// </summary>
        private async Task UpdateStockOnOutputAsync(int productId, int branchId, int quantity)
        {
            Console.WriteLine($"[UpdateStockOnOutputAsync] Iniciando — ProductId={productId}, BranchId={branchId}, Quantity={quantity}");

            try
            {
                var existing = await _productStockRepository.FindAsync(
                    x => x.ProductId == productId && x.BranchId == branchId);

                Console.WriteLine($"[UpdateStockOnOutputAsync] Registros existentes encontrados: {existing.Count}");

                if (existing.Count > 0)
                {
                    existing[0].Quantity -= quantity; // Sin Math.Max — el stock puede ser negativo
                    existing[0].UpdatedAt = DateTime.Now;
                    existing[0].SyncStatus = 1;
                    await _productStockRepository.UpdateAsync(existing[0]);
                    Console.WriteLine($"[UpdateStockOnOutputAsync] Stock actualizado — nuevo total: {existing[0].Quantity}");
                }
                else
                {
                    // No había registro previo: la salida genera stock negativo
                    var newStock = new ProductStock
                    {
                        ProductId = productId,
                        BranchId = branchId,
                        Quantity = -quantity, // Negativo por diseño
                        UpdatedAt = DateTime.Now,
                        SyncStatus = 1,
                    };
                    var newId = await _productStockRepository.AddAsync(newStock);
                    Console.WriteLine($"[UpdateStockOnOutputAsync] No había stock previo — creado registro en {-quantity} con Id={newId}");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[UpdateStockOnOutputAsync] ERROR — {ex.Message}");
                throw;
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
                x => x.BranchId == branchId
                  && x.EntryType == StockEntryType.Transfer
                  && x.ConfirmedAt == null);
        }

        /// <summary>
        /// Carga los nombres de todos los proveedores en un solo query.
        /// Úsalo antes de iterar una lista de entradas para evitar N+1.
        /// </summary>
        public async Task<Dictionary<int, string>> GetSupplierNameMapAsync()
        {
            var suppliers = await _supplierRepo.GetAllAsync();
            return suppliers.ToDictionary(s => s.Id, s => s.Name);
        }

        /// <summary>
        /// Carga los nombres de todas las sucursales en un solo query.
        /// Úsalo antes de iterar una lista de salidas para evitar N+1.
        /// </summary>
        public async Task<Dictionary<int, string>> GetBranchNameMapAsync()
        {
            var branches = await _branchRepo.GetAllAsync();
            return branches.ToDictionary(b => b.Id, b => b.Name);
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
