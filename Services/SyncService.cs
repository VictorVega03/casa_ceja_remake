using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using CasaCejaRemake.Data;
using CasaCejaRemake.Data.Repositories;
using CasaCejaRemake.Models;
using CasaCejaRemake.Models.DTOs;

namespace CasaCejaRemake.Services
{
    public class SyncService
    {
        private readonly ApiClient _apiClient;
        private readonly ConfigService _configService;

        // Repositorios — uno por entidad sincronizable
        private readonly BaseRepository<Category> _categoryRepo;
        private readonly BaseRepository<Unit> _unitRepo;
        private readonly BaseRepository<Branch> _branchRepo;
        private readonly BaseRepository<Supplier> _supplierRepo;
        private readonly BaseRepository<Customer> _customerRepo;
        private readonly BaseRepository<User> _userRepo;
        private readonly BaseRepository<Product> _productRepo;
        private readonly BaseRepository<Credit> _creditRepo;
        private readonly BaseRepository<CreditPayment> _creditPaymentRepo;
        private readonly BaseRepository<Layaway> _layawayRepo;
        private readonly BaseRepository<LayawayPayment> _layawayPaymentRepo;
        private readonly BaseRepository<Sale> _saleRepo;
        private readonly BaseRepository<CashClose> _cashCloseRepo;
        private readonly BaseRepository<StockEntry> _stockEntryRepo;
        private readonly BaseRepository<StockOutput> _stockOutputRepo;

        public SyncService(
            ApiClient apiClient,
            ConfigService configService,
            DatabaseService databaseService)
        {
            _apiClient          = apiClient;
            _configService      = configService;
            _categoryRepo       = new BaseRepository<Category>(databaseService);
            _unitRepo           = new BaseRepository<Unit>(databaseService);
            _branchRepo         = new BaseRepository<Branch>(databaseService);
            _supplierRepo       = new BaseRepository<Supplier>(databaseService);
            _customerRepo       = new BaseRepository<Customer>(databaseService);
            _userRepo           = new BaseRepository<User>(databaseService);
            _productRepo        = new BaseRepository<Product>(databaseService);
            _creditRepo         = new BaseRepository<Credit>(databaseService);
            _creditPaymentRepo  = new BaseRepository<CreditPayment>(databaseService);
            _layawayRepo        = new BaseRepository<Layaway>(databaseService);
            _layawayPaymentRepo = new BaseRepository<LayawayPayment>(databaseService);
            _saleRepo           = new BaseRepository<Sale>(databaseService);
            _cashCloseRepo      = new BaseRepository<CashClose>(databaseService);
            _stockEntryRepo     = new BaseRepository<StockEntry>(databaseService);
            _stockOutputRepo    = new BaseRepository<StockOutput>(databaseService);
        }

        // ──────────────────────────────────────────────────────
        // SYNC COMPLETO
        // ──────────────────────────────────────────────────────

        public async Task<List<SyncResult>> SyncAllAsync(CancellationToken ct = default)
        {
            var results = new List<SyncResult>();

            if (!await _apiClient.IsServerAvailableAsync())
            {
                results.Add(SyncResult.Fail("server", "Servidor no disponible"));
                return results;
            }

            results.AddRange(await PushAllAsync(ct));
            results.AddRange(await PullAllAsync(ct));

            var serverTime = await _apiClient.GetServerTimeAsync();
            if (serverTime > 0)
            {
                await _configService.UpdateAppConfigAsync(config =>
                {
                    config.LastSyncTimestamp = serverTime;
                });
            }

            return results;
        }

        // ──────────────────────────────────────────────────────
        // PULL
        // ──────────────────────────────────────────────────────

        public async Task<List<SyncResult>> PullAllAsync(CancellationToken ct = default)
        {
            var since    = _configService.AppConfig.LastSyncTimestamp;
            var branchId = _configService.AppConfig.CurrentBranchId ?? 0;
            var results  = new List<SyncResult>();

            results.Add(await PullAsync("categories",      since, _categoryRepo,      ct));
            results.Add(await PullAsync("units",           since, _unitRepo,           ct));
            results.Add(await PullAsync("branches",        since, _branchRepo,         ct));
            results.Add(await PullAsync("suppliers",       since, _supplierRepo,       ct));
            results.Add(await PullAsync("customers",       since, _customerRepo,       ct));
            results.Add(await PullAsync("users",           since, _userRepo,           ct));
            results.Add(await PullAsync("products",        since, _productRepo,        ct));
            results.Add(await PullAsync("credits",         since, _creditRepo,         ct, $"&branch_id={branchId}"));
            results.Add(await PullAsync("credit-payments", since, _creditPaymentRepo,  ct, $"&branch_id={branchId}"));
            results.Add(await PullAsync("layaways",        since, _layawayRepo,        ct, $"&branch_id={branchId}"));
            results.Add(await PullAsync("layaway-payments",since, _layawayPaymentRepo, ct, $"&branch_id={branchId}"));

            return results;
        }

        // ──────────────────────────────────────────────────────
        // PUSH
        // ──────────────────────────────────────────────────────

        public async Task<List<SyncResult>> PushAllAsync(CancellationToken ct = default)
        {
            var results = new List<SyncResult>();

            results.Add(await PushAsync("sales",            _saleRepo,           ct));
            results.Add(await PushAsync("cash-closes",      _cashCloseRepo,      ct));
            results.Add(await PushAsync("credits",          _creditRepo,         ct));
            results.Add(await PushAsync("credit-payments",  _creditPaymentRepo,  ct));
            results.Add(await PushAsync("layaways",         _layawayRepo,        ct));
            results.Add(await PushAsync("layaway-payments", _layawayPaymentRepo, ct));
            results.Add(await PushAsync("stock-entries",    _stockEntryRepo,     ct));
            results.Add(await PushAsync("stock-outputs",    _stockOutputRepo,    ct));

            return results;
        }

        // ──────────────────────────────────────────────────────
        // ON-DEMAND
        // ──────────────────────────────────────────────────────

        public async Task<ApiResponse<JsonElement>?> GetProductStockAsync(string barcode)
        {
            return await _apiClient.GetAsync<JsonElement>($"/api/v1/stock/product/{barcode}");
        }

        public async Task<ApiResponse<JsonElement>?> GetBranchStockAsync(int branchId, int page = 1)
        {
            return await _apiClient.GetAsync<JsonElement>($"/api/v1/stock/branch/{branchId}?page={page}");
        }

        // ──────────────────────────────────────────────────────
        // HELPERS PRIVADOS
        // ──────────────────────────────────────────────────────

        private async Task<SyncResult> PullAsync<T>(
            string entity,
            long since,
            BaseRepository<T> repo,
            CancellationToken ct,
            string extraParams = "") where T : new()
        {
            int totalPulled = 0;
            int page        = 1;

            try
            {
                while (true)
                {
                    var endpoint = $"/api/v1/sync/pull/{entity}?since={since}&page={page}{extraParams}";
                    var response = await _apiClient.GetAsync<PullResponse<T>>(endpoint, ct);

                    if (response?.IsSuccess != true || response.Data == null)
                        break;

                    var pullData = response.Data;

                    if (pullData.Data.Count == 0)
                        break;

                    foreach (var item in pullData.Data)
                    {
                        SetSyncStatus(item, 2);
                        await repo.UpsertAsync(item);
                    }

                    totalPulled += pullData.Count;

                    if (!pullData.HasMorePages)
                        break;

                    page++;
                }

                return SyncResult.Ok(entity, pulled: totalPulled);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[SyncService] Error Pull {entity}: {ex.Message}");
                return SyncResult.Fail(entity, ex.Message);
            }
        }

        private async Task<SyncResult> PushAsync<T>(
            string entity,
            BaseRepository<T> repo,
            CancellationToken ct) where T : class, new()
        {
            try
            {
                var pending = await repo.FindAsync(x => GetSyncStatus(x) == 1);

                if (pending.Count == 0)
                    return SyncResult.Ok(entity);

                int accepted = 0;
                int rejected = 0;

                const int batchSize = 100;
                for (int i = 0; i < pending.Count; i += batchSize)
                {
                    var batch    = pending.GetRange(i, Math.Min(batchSize, pending.Count - i));
                    var body     = new { records = batch };
                    var response = await _apiClient.PostAsync<PushResponse>(
                        $"/api/v1/sync/push/{entity}", body, ct);

                    if (response?.Data == null) continue;

                    foreach (var record in batch)
                    {
                        var folio = GetFolio(record);
                        if (folio != null && response.Data.Accepted.Contains(folio))
                        {
                            SetSyncStatus(record, 2);
                            SetLastSync(record, DateTime.Now);
                            await repo.UpdateAsync(record);
                        }
                    }

                    accepted += response.Data.Accepted.Count;
                    rejected += response.Data.Rejected.Count;

                    foreach (var r in response.Data.Rejected)
                        Console.WriteLine($"[SyncService] Rechazado {entity} folio={r.Folio}: {r.Reason}");
                }

                return new SyncResult
                {
                    Success         = true,
                    Entity          = entity,
                    RecordsPushed   = accepted,
                    RecordsRejected = rejected,
                };
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[SyncService] Error Push {entity}: {ex.Message}");
                return SyncResult.Fail(entity, ex.Message);
            }
        }

        // ──────────────────────────────────────────────────────
        // REFLEXIÓN — acceso a propiedades comunes
        // ──────────────────────────────────────────────────────

        private void SetSyncStatus<T>(T entity, int status)
        {
            typeof(T).GetProperty("SyncStatus")?.SetValue(entity, status);
        }

        private int GetSyncStatus<T>(T entity)
        {
            return (int)(typeof(T).GetProperty("SyncStatus")?.GetValue(entity) ?? 0);
        }

        private void SetLastSync<T>(T entity, DateTime dateTime)
        {
            typeof(T).GetProperty("LastSync")?.SetValue(entity, dateTime);
        }

        private string? GetFolio<T>(T entity)
        {
            return typeof(T).GetProperty("Folio")?.GetValue(entity)?.ToString();
        }
    }
}