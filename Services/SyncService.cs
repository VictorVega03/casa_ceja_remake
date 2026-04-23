using System;
using System.Collections.Generic;
using System.Linq;
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
        private readonly DatabaseService _databaseService;

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
        private readonly BaseRepository<Role> _roleRepo;

        public SyncService(
            ApiClient apiClient,
            ConfigService configService,
            DatabaseService databaseService)
        {
            _apiClient          = apiClient;
            _configService      = configService;
            _databaseService    = databaseService;
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
            _roleRepo           = new BaseRepository<Role>(databaseService);
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

        /// <summary>
        /// Pull rápido de roles desde el servidor. Se llama justo después del login
        /// para garantizar que los roles están disponibles antes de mostrar el selector.
        /// </summary>
        public async Task PullRolesOnlyAsync(CancellationToken ct = default)
        {
            try
            {
                if (!await _apiClient.IsServerAvailableAsync())
                    return;

                await PullAsync("roles", 0, _roleRepo, ct);
                Console.WriteLine("[SyncService] Roles sincronizados desde servidor");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[SyncService] Error al sincronizar roles: {ex.Message}");
            }
        }

    public async Task<List<SyncResult>> PullAllAsync(CancellationToken ct = default)
    {
        var branchId = _configService.AppConfig.CurrentBranchId ?? 0;
        var results  = new List<SyncResult>();

        // Si la BD local está vacía (reinstalación o BD borrada),
        // forzar since=0 para Pull completo ignorando el timestamp guardado en config.json
        var productCount = await _productRepo.CountAsync();
        var since = productCount == 0 ? 0 : _configService.AppConfig.LastSyncTimestamp;

        // Roles PRIMERO y SECUENCIAL — selector de módulos y autorización de todo
        // el sistema dependen de ellos. Si falla aquí, no debe tumbar al resto
        // de pulls, por eso está aislado del Task.WhenAll de abajo.
        var rolesResult = await PullAsync("roles", 0, _roleRepo, ct);
        results.Add(rolesResult);

        // Catálogos globales restantes — corren en paralelo
        var catalogTasks = new[]
        {
            PullAsync("categories",   since, _categoryRepo,  ct),
            PullAsync("units",        since, _unitRepo,       ct),
            PullAsync("branches",     since, _branchRepo,     ct),
            PullAsync("suppliers",    since, _supplierRepo,   ct),
            PullAsync("customers",    since, _customerRepo,   ct),
            PullUsersAsync(since, ct),   // especializado — preserva password
            PullAsync("products",     since, _productRepo,    ct),
        };

        var catalogResults = await Task.WhenAll(catalogTasks);
        results.AddRange(catalogResults);

        // Operaciones por sucursal — también en paralelo
        var operationTasks = new[]
        {
            PullCreditsAsync(since, branchId, ct),
            PullCreditPaymentsAsync(since, branchId, ct),
            PullLayawaysAsync(since, branchId, ct),
            PullLayawayPaymentsAsync(since, branchId, ct),
        };

        var operationResults = await Task.WhenAll(operationTasks);
        results.AddRange(operationResults);

        return results;
    }

        // ──────────────────────────────────────────────────────
        // PUSH
        // ──────────────────────────────────────────────────────

        public async Task<List<SyncResult>> PushAllAsync(CancellationToken ct = default)
        {
            var results = new List<SyncResult>();

            results.Add(await PushCustomersAsync(ct));
            results.Add(await PushSalesAsync(ct));
            results.Add(await PushCashClosesAsync(ct));
            results.Add(await PushCreditsAsync(ct));
            results.Add(await PushCreditPaymentsAsync(ct));
            results.Add(await PushLayawaysAsync(ct));
            results.Add(await PushLayawayPaymentsAsync(ct));
            results.Add(await PushStockEntriesAsync(ct));
            results.Add(await PushStockOutputsAsync(ct));

            return results;
        }

        private async Task<SyncResult> PushCustomersAsync(CancellationToken ct)
        {
            try
            {
                var all = await _customerRepo.GetAllAsync();
                var pending = all.Where(c => c.SyncStatus == 1).ToList();

                if (pending.Count == 0)
                    return SyncResult.Ok("customers");

                int accepted = 0;
                int rejected = 0;

                foreach (var customer in pending)
                {
                    if (ct.IsCancellationRequested)
                        break;

                    var pushed = await PushCustomerAsync(customer, ct);
                    if (pushed)
                        accepted++;
                    else
                        rejected++;
                }

                return new SyncResult
                {
                    Success = true,
                    Entity = "customers",
                    RecordsPushed = accepted,
                    RecordsRejected = rejected,
                };
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[SyncService] Error Push customers: {ex.Message}");
                return SyncResult.Fail("customers", ex.Message);
            }
        }
        private async Task<SyncResult> PushSalesAsync(CancellationToken ct)
        {
            try
            {
                var all     = await _saleRepo.GetAllAsync();
                var pending = all.Where(x => GetSyncStatus(x) == 1).ToList();

                if (pending.Count == 0)
                    return SyncResult.Ok("sales");

                var saleProductRepo = new BaseRepository<SaleProduct>(_databaseService);
                var allProducts     = await saleProductRepo.GetAllAsync();

                var dtos = pending.Select(sale => new SalePushDto
                {
                    Folio          = sale.Folio,
                    BranchId       = sale.BranchId,
                    UserId         = sale.UserId,
                    Subtotal       = sale.Subtotal,
                    Discount       = sale.Discount,
                    Total          = sale.Total,
                    AmountPaid     = sale.AmountPaid,
                    ChangeGiven    = sale.ChangeGiven,
                    PaymentMethod  = sale.PaymentMethod,
                    PaymentSummary = sale.PaymentSummary,
                    CashCloseFolio = sale.CashCloseFolio,
                    SaleDate       = sale.SaleDate,
                    TicketData     = sale.TicketData,
                    Products       = allProducts
                        .Where(p => p.SaleId == sale.Id)
                        .Select(p => new SaleProductPushDto
                        {
                            ProductId           = p.ProductId,
                            Barcode             = p.Barcode,
                            ProductName         = p.ProductName,
                            Quantity            = p.Quantity,
                            ListPrice           = p.ListPrice,
                            FinalUnitPrice      = p.FinalUnitPrice,
                            LineTotal           = p.LineTotal,
                            TotalDiscountAmount = p.TotalDiscountAmount,
                            PriceType           = p.PriceType,
                            DiscountInfo        = p.DiscountInfo,
                            PricingData         = p.PricingData,
                        }).ToList()
                }).ToList();

                int accepted = 0;
                int rejected = 0;
                var branchId = _configService.AppConfig.CurrentBranchId ?? 0;

                const int batchSize = 100;
                for (int i = 0; i < dtos.Count; i += batchSize)
                {
                    var batch    = dtos.GetRange(i, Math.Min(batchSize, dtos.Count - i));
                    var body     = new { branch_id = branchId, records = batch };
                    var response = await _apiClient.PostAsync<PushResponse>(
                        "/api/v1/sync/push/sales", body, ct);

                    if (response?.Data == null) continue;

                    foreach (var sale in pending)
                    {
                        if (response.Data.Accepted.Contains(sale.Folio))
                        {
                            SetSyncStatus(sale, 2);
                            SetLastSync(sale, DateTime.Now);
                            await _saleRepo.UpdateAsync(sale);
                        }
                    }

                    accepted += response.Data.Accepted.Count;
                    rejected += response.Data.Rejected.Count;

                    foreach (var r in response.Data.Rejected)
                        Console.WriteLine($"[SyncService] Rechazado sales folio={r.Folio}: {r.Reason}");
                }

                return new SyncResult
                {
                    Success         = true,
                    Entity          = "sales",
                    RecordsPushed   = accepted,
                    RecordsRejected = rejected,
                };
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[SyncService] Error Push sales: {ex.Message}");
                return SyncResult.Fail("sales", ex.Message);
            }
        }

        private async Task<SyncResult> PushCashClosesAsync(CancellationToken ct)
        {
            try
            {
                var all     = await _cashCloseRepo.GetAllAsync();
                var pending = all.Where(x => 
                    GetSyncStatus(x) == 1 && 
                    x.CloseDate > x.OpeningDate.AddSeconds(1)  // Solo cortes cerrados
                ).ToList();

                if (pending.Count == 0)
                    return SyncResult.Ok("cash-closes");

                var movementRepo = new BaseRepository<CashMovement>(_databaseService);
                var allMovements = await movementRepo.GetAllAsync();
                var branchId     = _configService.AppConfig.CurrentBranchId ?? 0;

                var dtos = pending.Select(cc => new CashClosePushDto
                {
                    Folio                = cc.Folio,
                    BranchId             = cc.BranchId,
                    UserId               = cc.UserId,
                    OpeningCash          = cc.OpeningCash,
                    TotalCash            = cc.TotalCash,
                    TotalDebitCard       = cc.TotalDebitCard,
                    TotalCreditCard      = cc.TotalCreditCard,
                    TotalChecks          = cc.TotalChecks,
                    TotalTransfers       = cc.TotalTransfers,
                    LayawayCash          = cc.LayawayCash,
                    CreditCash           = cc.CreditCash,
                    CreditTotalCreated   = cc.CreditTotalCreated,
                    LayawayTotalCreated  = cc.LayawayTotalCreated,
                    Expenses             = cc.Expenses,
                    Income               = cc.Income,
                    Surplus              = cc.Surplus,
                    ExpectedCash         = cc.ExpectedCash,
                    TotalSales           = cc.TotalSales,
                    Notes                = cc.Notes,
                    OpeningDate          = cc.OpeningDate,
                    CloseDate            = cc.CloseDate,
                    Movements            = allMovements
                        .Where(m => m.CashCloseId == cc.Id)
                        .Select(m => new CashMovementPushDto
                        {
                            Type    = m.Type,
                            Concept = m.Concept,
                            Amount  = m.Amount,
                            UserId  = m.UserId,
                        }).ToList()
                }).ToList();

                return await PushWithDtoAsync("cash-closes", dtos, pending, branchId, ct);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[SyncService] Error Push cash-closes: {ex.Message}");
                return SyncResult.Fail("cash-closes", ex.Message);
            }
        }

        private async Task<SyncResult> PushCreditsAsync(CancellationToken ct)
        {
            try
            {
                var all     = await _creditRepo.GetAllAsync();
                var pending = all.Where(x => GetSyncStatus(x) == 1).ToList();

                if (pending.Count == 0)
                    return SyncResult.Ok("credits");

                var productRepo = new BaseRepository<CreditProduct>(_databaseService);
                var allProducts = await productRepo.GetAllAsync();
                var branchId    = _configService.AppConfig.CurrentBranchId ?? 0;

                var dtos = pending.Select(c => new CreditPushDto
                {
                    Folio        = c.Folio,
                    BranchId     = c.BranchId,
                    UserId       = c.UserId,
                    CustomerId   = c.CustomerId ?? 0,
                    Total        = c.Total,
                    TotalPaid    = c.TotalPaid,
                    MonthsToPay  = c.MonthsToPay,
                    CreditDate   = c.CreditDate,
                    DueDate      = c.DueDate,
                    Status       = c.Status,
                    Notes        = c.Notes,
                    CashCloseFolio = c.CashCloseFolio,
                    TicketData   = c.TicketData,
                    Products     = allProducts
                        .Where(p => p.CreditId == c.Id)
                        .Select(p => new CreditProductPushDto
                        {
                            ProductId   = p.ProductId,
                            Barcode     = p.Barcode,
                            ProductName = p.ProductName,
                            Quantity    = p.Quantity,
                            UnitPrice   = p.UnitPrice,
                                LineTotal   = p.LineTotal,
                        }).ToList()
                }).ToList();

                return await PushWithDtoAsync("credits", dtos, pending, branchId, ct);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[SyncService] Error Push credits: {ex.Message}");
                return SyncResult.Fail("credits", ex.Message);
            }
        }

        private async Task<SyncResult> PushCreditPaymentsAsync(CancellationToken ct)
        {
            try
            {
                var all     = await _creditPaymentRepo.GetAllAsync();
                var pending = all.Where(x => GetSyncStatus(x) == 1).ToList();

                if (pending.Count == 0)
                    return SyncResult.Ok("credit-payments");

                // Necesitamos el folio del crédito — cargar todos los créditos locales
                var allCredits = await _creditRepo.GetAllAsync();
                var branchId   = _configService.AppConfig.CurrentBranchId ?? 0;

                var dtos = pending.Select(p =>
                {
                    var credit = allCredits.FirstOrDefault(c => c.Id == p.CreditId);
                    return new CreditPaymentPushDto
                    {
                        Folio           = p.Folio,
                        CreditFolio     = credit?.Folio ?? string.Empty,
                        UserId          = p.UserId,
                        AmountPaid      = p.AmountPaid,
                        PaymentMethod   = p.PaymentMethod,
                        PaymentDate     = p.PaymentDate,
                        CashCloseFolio  = p.CashCloseFolio,
                        Notes           = p.Notes,
                    };
                }).ToList();

                // Filtrar los que no tienen folio de crédito (error de datos)
                var valid   = dtos.Where(d => !string.IsNullOrEmpty(d.CreditFolio)).ToList();
                var invalid = dtos.Where(d => string.IsNullOrEmpty(d.CreditFolio)).ToList();

                if (invalid.Count > 0)
                    Console.WriteLine($"[SyncService] {invalid.Count} abonos sin folio de crédito — omitidos");

                if (valid.Count == 0)
                    return SyncResult.Ok("credit-payments");

                int accepted = 0;
                int rejected = 0;

                const int batchSize = 100;
                for (int i = 0; i < valid.Count; i += batchSize)
                {
                    var batch    = valid.GetRange(i, Math.Min(batchSize, valid.Count - i));
                    var body     = new { branch_id = branchId, records = batch };
                    var response = await _apiClient.PostAsync<PushResponse>(
                        "/api/v1/sync/push/credit-payments", body, ct);

                    if (response?.Data == null) continue;

                    foreach (var payment in pending)
                    {
                        if (response.Data.Accepted.Contains(payment.Folio))
                        {
                            SetSyncStatus(payment, 2);
                            SetLastSync(payment, DateTime.Now);
                            await _creditPaymentRepo.UpdateAsync(payment);
                        }
                    }

                    accepted += response.Data.Accepted.Count;
                    rejected += response.Data.Rejected.Count;

                    foreach (var r in response.Data.Rejected)
                        Console.WriteLine($"[SyncService] Rechazado credit-payments folio={r.Folio}: {r.Reason}");
                }

                return new SyncResult
                {
                    Success         = true,
                    Entity          = "credit-payments",
                    RecordsPushed   = accepted,
                    RecordsRejected = rejected,
                };
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[SyncService] Error Push credit-payments: {ex.Message}");
                return SyncResult.Fail("credit-payments", ex.Message);
            }
        }

        private async Task<SyncResult> PushLayawayPaymentsAsync(CancellationToken ct)
        {
            try
            {
                var all     = await _layawayPaymentRepo.GetAllAsync();
                var pending = all.Where(x => GetSyncStatus(x) == 1).ToList();

                if (pending.Count == 0)
                    return SyncResult.Ok("layaway-payments");

                var allLayaways = await _layawayRepo.GetAllAsync();
                var branchId    = _configService.AppConfig.CurrentBranchId ?? 0;

                var dtos = pending.Select(p =>
                {
                    var layaway = allLayaways.FirstOrDefault(l => l.Id == p.LayawayId);
                    return new LayawayPaymentPushDto
                    {
                        Folio          = p.Folio,
                        LayawayFolio   = layaway?.Folio ?? string.Empty,
                        UserId         = p.UserId,
                        AmountPaid     = p.AmountPaid,
                        PaymentMethod  = p.PaymentMethod,
                        PaymentDate    = p.PaymentDate,
                        CashCloseFolio = p.CashCloseFolio,
                        Notes          = p.Notes,
                    };
                }).ToList();

                var valid   = dtos.Where(d => !string.IsNullOrEmpty(d.LayawayFolio)).ToList();
                var invalid = dtos.Where(d => string.IsNullOrEmpty(d.LayawayFolio)).ToList();

                if (invalid.Count > 0)
                    Console.WriteLine($"[SyncService] {invalid.Count} abonos de apartado sin folio — omitidos");

                if (valid.Count == 0)
                    return SyncResult.Ok("layaway-payments");

                int accepted = 0;
                int rejected = 0;

                const int batchSize = 100;
                for (int i = 0; i < valid.Count; i += batchSize)
                {
                    var batch    = valid.GetRange(i, Math.Min(batchSize, valid.Count - i));
                    var body     = new { branch_id = branchId, records = batch };
                    var response = await _apiClient.PostAsync<PushResponse>(
                        "/api/v1/sync/push/layaway-payments", body, ct);

                    if (response?.Data == null) continue;

                    foreach (var payment in pending)
                    {
                        if (response.Data.Accepted.Contains(payment.Folio))
                        {
                            SetSyncStatus(payment, 2);
                            SetLastSync(payment, DateTime.Now);
                            await _layawayPaymentRepo.UpdateAsync(payment);
                        }
                    }

                    accepted += response.Data.Accepted.Count;
                    rejected += response.Data.Rejected.Count;

                    foreach (var r in response.Data.Rejected)
                        Console.WriteLine($"[SyncService] Rechazado layaway-payments folio={r.Folio}: {r.Reason}");
                }

                return new SyncResult
                {
                    Success         = true,
                    Entity          = "layaway-payments",
                    RecordsPushed   = accepted,
                    RecordsRejected = rejected,
                };
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[SyncService] Error Push layaway-payments: {ex.Message}");
                return SyncResult.Fail("layaway-payments", ex.Message);
            }
        }

        private async Task<SyncResult> PushLayawaysAsync(CancellationToken ct)
        {
            try
            {
                var all     = await _layawayRepo.GetAllAsync();
                var pending = all.Where(x => GetSyncStatus(x) == 1).ToList();

                if (pending.Count == 0)
                    return SyncResult.Ok("layaways");

                var productRepo = new BaseRepository<LayawayProduct>(_databaseService);
                var allProducts = await productRepo.GetAllAsync();
                var branchId    = _configService.AppConfig.CurrentBranchId ?? 0;

                var dtos = pending.Select(l => new LayawayPushDto
                {
                    Folio           = l.Folio,
                    BranchId        = l.BranchId,
                    UserId          = l.UserId,
                    DeliveryUserId  = l.DeliveryUserId,
                    CustomerId      = l.CustomerId,
                    Total           = l.Total,
                    TotalPaid       = l.TotalPaid,
                    LayawayDate     = l.LayawayDate,
                    PickupDate      = l.PickupDate,
                    DeliveryDate    = l.DeliveryDate,
                    Status          = l.Status,
                    Notes           = l.Notes,
                    CashCloseFolio  = l.CashCloseFolio,
                    TicketData      = l.TicketData,
                    Products        = allProducts
                        .Where(p => p.LayawayId == l.Id)
                        .Select(p => new LayawayProductPushDto
                        {
                            ProductId   = p.ProductId,
                            Barcode     = p.Barcode,
                            ProductName = p.ProductName,
                            Quantity    = p.Quantity,
                            UnitPrice   = p.UnitPrice,
                                LineTotal   = p.LineTotal,
                        }).ToList()
                }).ToList();

                return await PushWithDtoAsync("layaways", dtos, pending, branchId, ct);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[SyncService] Error Push layaways: {ex.Message}");
                return SyncResult.Fail("layaways", ex.Message);
            }
        }

        private async Task<SyncResult> PushStockEntriesAsync(CancellationToken ct)
        {
            try
            {
                var all = await _stockEntryRepo.GetAllAsync();

                // Solo PURCHASE con SyncStatus=1 — las TRANSFER ya vienen del servidor (SyncStatus=2)
                var pending = all
                    .Where(x => GetSyncStatus(x) == 1 && x.EntryType == StockEntryType.Purchase)
                    .ToList();

                if (pending.Count == 0)
                    return SyncResult.Ok("stock-entries");

                var productRepo = new BaseRepository<EntryProduct>(_databaseService);
                var allProducts = await productRepo.GetAllAsync();
                var branchId    = _configService.AppConfig.CurrentBranchId ?? 0;

                var dtos = pending.Select(e => new StockEntryPushDto
                {
                    Folio        = e.Folio,
                    FolioOutput  = e.FolioOutput,
                    BranchId     = e.BranchId,
                    SupplierId   = e.SupplierId,
                    UserId       = e.UserId,
                    EntryType    = e.EntryType,
                    TotalAmount  = e.TotalAmount,
                    EntryDate    = e.EntryDate,
                    Notes        = e.Notes,
                    Products     = allProducts
                        .Where(p => p.EntryId == e.Id)
                        .Select(p => new EntryProductPushDto
                        {
                            ProductId   = p.ProductId,
                            Barcode     = p.Barcode,
                            ProductName = p.ProductName,
                            Quantity    = p.Quantity,
                            UnitCost    = p.UnitCost,
                            LineTotal   = p.LineTotal,
                        }).ToList()
                }).ToList();

                return await PushWithDtoAsync("stock-entries", dtos, pending, branchId, ct);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[SyncService] Error Push stock-entries: {ex.Message}");
                return SyncResult.Fail("stock-entries", ex.Message);
            }
        }

        private async Task<SyncResult> PushStockOutputsAsync(CancellationToken ct)
        {
            try
            {
                var all     = await _stockOutputRepo.GetAllAsync();
                var pending = all.Where(x => GetSyncStatus(x) == 1).ToList();

                if (pending.Count == 0)
                    return SyncResult.Ok("stock-outputs");

                var productRepo = new BaseRepository<OutputProduct>(_databaseService);
                var allProducts = await productRepo.GetAllAsync();
                var branchId    = _configService.AppConfig.CurrentBranchId ?? 0;

                var dtos = pending.Select(o => new StockOutputPushDto
                {
                    Folio                 = o.Folio,
                    OriginBranchId        = o.OriginBranchId,
                    DestinationBranchId   = o.DestinationBranchId,
                    UserId                = o.UserId,
                    Type                  = "OTHER",
                    TotalAmount           = o.TotalAmount,
                    OutputDate            = o.OutputDate,
                    Notes                 = o.Notes,
                    Products              = allProducts
                        .Where(p => p.OutputId == o.Id)
                        .Select(p => new OutputProductPushDto
                        {
                            ProductId   = p.ProductId,
                            Barcode     = p.Barcode,
                            ProductName = p.ProductName,
                            Quantity    = p.Quantity,
                            UnitCost    = p.UnitCost,
                            LineTotal   = p.LineTotal,
                        }).ToList()
                }).ToList();

                return await PushWithDtoAsync("stock-outputs", dtos, pending, branchId, ct);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[SyncService] Error Push stock-outputs: {ex.Message}");
                return SyncResult.Fail("stock-outputs", ex.Message);
            }
        }

        private async Task<SyncResult> PushWithDtoAsync<TDto, TModel>(
            string entity,
            List<TDto> dtos,
            List<TModel> originals,
            int branchId,
            CancellationToken ct) where TModel : class, new()
        {
            int accepted = 0;
            int rejected = 0;

            const int batchSize = 100;
            for (int i = 0; i < dtos.Count; i += batchSize)
            {
                var batchDtos = dtos.GetRange(i, Math.Min(batchSize, dtos.Count - i));
                var body      = new { branch_id = branchId, records = batchDtos };
                var response  = await _apiClient.PostAsync<PushResponse>(
                    $"/api/v1/sync/push/{entity}", body, ct);

                if (response?.Data == null) continue;

                foreach (var original in originals)
                {
                    var folio = GetFolio(original);
                    if (folio != null && response.Data.Accepted.Contains(folio))
                    {
                        SetSyncStatus(original, 2);
                        SetLastSync(original, DateTime.Now);

                        var repo = GetRepoForModel<TModel>();
                        if (repo != null)
                            await repo.UpdateAsync(original);
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

        private BaseRepository<T>? GetRepoForModel<T>() where T : class, new()
        {
            if (typeof(T) == typeof(CashClose))    return _cashCloseRepo as BaseRepository<T>;
            if (typeof(T) == typeof(Credit))       return _creditRepo as BaseRepository<T>;
            if (typeof(T) == typeof(Layaway))      return _layawayRepo as BaseRepository<T>;
            if (typeof(T) == typeof(StockEntry))   return _stockEntryRepo as BaseRepository<T>;
            if (typeof(T) == typeof(StockOutput))  return _stockOutputRepo as BaseRepository<T>;
            return null;
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

        private async Task<SyncResult> PullUsersAsync(long since, CancellationToken ct)
        {
            int totalPulled = 0;
            int page        = 1;
            Console.WriteLine($"[SyncService] Iniciando Pull users since={since}");

            try
            {
                while (true)
                {
                    var endpoint = $"/api/v1/sync/pull/users?since={since}&page={page}";
                    var response = await _apiClient.GetAsync<PullResponse<User>>(endpoint, ct);

                    if (response?.IsSuccess != true || response.Data == null)
                        break;

                    var pullData = response.Data;

                    Console.WriteLine($"[SyncService] Pull users page={page} count={pullData.Data.Count}");

                    if (pullData.Data.Count == 0)
                        break;

                    foreach (var serverUser in pullData.Data)
                    {
                        SetSyncStatus(serverUser, 2);

                        var existing = await _userRepo.FirstOrDefaultAsync(u => u.Id == serverUser.Id);

                        if (existing != null)
                        {
                            if (string.IsNullOrWhiteSpace(serverUser.Password))
                                serverUser.Password = existing.Password;

                            await _userRepo.UpdateAsync(serverUser);
                        }
                        else
                        {
                            await _userRepo.AddAsync(serverUser);
                        }
                    }

                    totalPulled += pullData.Count;

                    if (!pullData.HasMorePages)
                        break;

                    page++;
                }

                return SyncResult.Ok("users", pulled: totalPulled);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[SyncService] Error Pull users: {ex.Message}");
                return SyncResult.Fail("users", ex.Message);
            }
        }

        private async Task<SyncResult> PullCreditsAsync(long since, int branchId, CancellationToken ct)
        {
            int totalPulled = 0;
            int page        = 1;
            var creditProductRepo = new BaseRepository<CreditProduct>(_databaseService);
            Console.WriteLine($"[SyncService] Iniciando Pull credits since={since} branch={branchId}");

            try
            {
                while (true)
                {
                    var endpoint = $"/api/v1/sync/pull/credits?since={since}&page={page}&branch_id={branchId}";
                    var response = await _apiClient.GetAsync<PullResponse<CreditPullDto>>(endpoint, ct);

                    if (response?.IsSuccess != true || response.Data == null) break;

                    var pullData = response.Data;
                    Console.WriteLine($"[SyncService] Pull credits page={page} count={pullData.Data.Count}");

                    if (pullData.Data.Count == 0) break;

                    foreach (var dto in pullData.Data)
                    {
                        var existing = await _creditRepo.FirstOrDefaultAsync(c => c.Folio == dto.Folio);
                        if (existing != null)
                        {
                            // Crédito ya existe localmente — solo actualizar campos mutables
                            existing.Status    = dto.Status;
                            existing.TotalPaid = dto.TotalPaid;
                            existing.SyncStatus = 2;
                            await _creditRepo.UpdateAsync(existing);
                        }
                        else
                        {
                            // Crédito nuevo de otra caja — guardar cabecera
                            var credit = new Credit
                            {
                                Folio          = dto.Folio,
                                CustomerId     = dto.CustomerId,
                                BranchId       = dto.BranchId,
                                UserId         = dto.UserId,
                                Total          = dto.Total,
                                TotalPaid      = dto.TotalPaid,
                                MonthsToPay    = dto.MonthsToPay,
                                CreditDate     = dto.CreditDate,
                                DueDate        = dto.DueDate,
                                Status         = dto.Status,
                                Notes          = dto.Notes,
                                CashCloseFolio = dto.CashCloseFolio,
                                TicketData     = dto.TicketData,
                                SyncStatus     = 2,
                            };

                            // AddAsync hace INSERT simple — SQLite auto-asigna id local
                            // (NO usar AddWithExplicitIdAsync — causaría id=0 y destruiría registros previos)
                            await _creditRepo.AddAsync(credit);
                            // Ahora credit.Id tiene el id local recién asignado por SQLite

                            // Guardar productos con el ID LOCAL del crédito
                            foreach (var prod in dto.Products)
                            {
                                var cp = new CreditProduct
                                {
                                    CreditId    = credit.Id, // ID LOCAL, no el del servidor
                                    ProductId   = prod.ProductId,
                                    Barcode     = prod.Barcode,
                                    ProductName = prod.ProductName,
                                    Quantity    = prod.Quantity,
                                    UnitPrice   = prod.UnitPrice,
                                    LineTotal   = prod.LineTotal,
                                };
                                await creditProductRepo.AddAsync(cp);
                            }
                        }
                    }

                    totalPulled += pullData.Count;
                    if (!pullData.HasMorePages) break;
                    page++;
                }

                Console.WriteLine($"[SyncService] Pull credits completado: {totalPulled} registros");
                return SyncResult.Ok("credits", pulled: totalPulled);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[SyncService] Error Pull credits: {ex.Message}");
                return SyncResult.Fail("credits", ex.Message);
            }
        }

        private async Task<SyncResult> PullCreditPaymentsAsync(long since, int branchId, CancellationToken ct)
        {
            int totalPulled = 0;
            int page        = 1;
            Console.WriteLine($"[SyncService] Iniciando Pull credit-payments since={since} branch={branchId}");

            try
            {
                while (true)
                {
                    var endpoint = $"/api/v1/sync/pull/credit-payments?since={since}&page={page}&branch_id={branchId}";
                    var response = await _apiClient.GetAsync<PullResponse<CreditPaymentPullDto>>(endpoint, ct);

                    if (response?.IsSuccess != true || response.Data == null) break;

                    var pullData = response.Data;
                    Console.WriteLine($"[SyncService] Pull credit-payments page={page} count={pullData.Data.Count}");

                    if (pullData.Data.Count == 0) break;

                    foreach (var dto in pullData.Data)
                    {
                        var existing = await _creditPaymentRepo.FirstOrDefaultAsync(p => p.Folio == dto.Folio);
                        if (existing != null)
                        {
                            existing.SyncStatus = 2;
                            await _creditPaymentRepo.UpdateAsync(existing);
                        }
                        else
                        {
                            // Resolver credit_id LOCAL usando el folio del crédito
                            if (string.IsNullOrEmpty(dto.CreditFolio))
                            {
                                Console.WriteLine($"[SyncService] Abono {dto.Folio} sin credit_folio — omitido");
                                continue;
                            }

                            var localCredit = await _creditRepo.FirstOrDefaultAsync(c => c.Folio == dto.CreditFolio);
                            if (localCredit == null)
                            {
                                Console.WriteLine($"[SyncService] Abono {dto.Folio}: crédito '{dto.CreditFolio}' no encontrado localmente — omitido");
                                continue;
                            }

                            var payment = new CreditPayment
                            {
                                Folio          = dto.Folio,
                                CreditId       = localCredit.Id, // ID LOCAL correcto
                                UserId         = dto.UserId,
                                AmountPaid     = dto.AmountPaid,
                                PaymentMethod  = dto.PaymentMethod,
                                PaymentDate    = dto.PaymentDate,
                                CashCloseFolio = dto.CashCloseFolio,
                                Notes          = dto.Notes,
                                SyncStatus     = 2,
                            };
                            await _creditPaymentRepo.AddAsync(payment);
                        }
                    }

                    totalPulled += pullData.Count;
                    if (!pullData.HasMorePages) break;
                    page++;
                }

                Console.WriteLine($"[SyncService] Pull credit-payments completado: {totalPulled} registros");
                return SyncResult.Ok("credit-payments", pulled: totalPulled);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[SyncService] Error Pull credit-payments: {ex.Message}");
                return SyncResult.Fail("credit-payments", ex.Message);
            }
        }

        private async Task<SyncResult> PullLayawaysAsync(long since, int branchId, CancellationToken ct)
        {
            int totalPulled = 0;
            int page        = 1;
            var layawayProductRepo = new BaseRepository<LayawayProduct>(_databaseService);
            Console.WriteLine($"[SyncService] Iniciando Pull layaways since={since} branch={branchId}");

            try
            {
                while (true)
                {
                    var endpoint = $"/api/v1/sync/pull/layaways?since={since}&page={page}&branch_id={branchId}";
                    var response = await _apiClient.GetAsync<PullResponse<LayawayPullDto>>(endpoint, ct);

                    if (response?.IsSuccess != true || response.Data == null) break;

                    var pullData = response.Data;
                    Console.WriteLine($"[SyncService] Pull layaways page={page} count={pullData.Data.Count}");

                    if (pullData.Data.Count == 0) break;

                    foreach (var dto in pullData.Data)
                    {
                        var existing = await _layawayRepo.FirstOrDefaultAsync(l => l.Folio == dto.Folio);
                        if (existing != null)
                        {
                            existing.Status    = dto.Status;
                            existing.TotalPaid = dto.TotalPaid;
                            existing.SyncStatus = 2;
                            await _layawayRepo.UpdateAsync(existing);
                        }
                        else
                        {
                            var layaway = new Layaway
                            {
                                Folio          = dto.Folio,
                                CustomerId     = dto.CustomerId,
                                BranchId       = dto.BranchId,
                                UserId         = dto.UserId,
                                DeliveryUserId = dto.DeliveryUserId,
                                Total          = dto.Total,
                                TotalPaid      = dto.TotalPaid,
                                LayawayDate    = dto.LayawayDate,
                                PickupDate     = dto.PickupDate,
                                DeliveryDate   = dto.DeliveryDate,
                                Status         = dto.Status,
                                Notes          = dto.Notes,
                                CashCloseFolio = dto.CashCloseFolio,
                                TicketData     = dto.TicketData,
                                SyncStatus     = 2,
                            };

                            await _layawayRepo.AddAsync(layaway);

                            foreach (var prod in dto.Products)
                            {
                                var lp = new LayawayProduct
                                {
                                    LayawayId   = layaway.Id, // ID LOCAL
                                    ProductId   = prod.ProductId,
                                    Barcode     = prod.Barcode,
                                    ProductName = prod.ProductName,
                                    Quantity    = prod.Quantity,
                                    UnitPrice   = prod.UnitPrice,
                                    LineTotal   = prod.LineTotal,
                                };
                                await layawayProductRepo.AddAsync(lp);
                            }
                        }
                    }

                    totalPulled += pullData.Count;
                    if (!pullData.HasMorePages) break;
                    page++;
                }

                Console.WriteLine($"[SyncService] Pull layaways completado: {totalPulled} registros");
                return SyncResult.Ok("layaways", pulled: totalPulled);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[SyncService] Error Pull layaways: {ex.Message}");
                return SyncResult.Fail("layaways", ex.Message);
            }
        }

        private async Task<SyncResult> PullLayawayPaymentsAsync(long since, int branchId, CancellationToken ct)
        {
            int totalPulled = 0;
            int page        = 1;
            Console.WriteLine($"[SyncService] Iniciando Pull layaway-payments since={since} branch={branchId}");

            try
            {
                while (true)
                {
                    var endpoint = $"/api/v1/sync/pull/layaway-payments?since={since}&page={page}&branch_id={branchId}";
                    var response = await _apiClient.GetAsync<PullResponse<LayawayPaymentPullDto>>(endpoint, ct);

                    if (response?.IsSuccess != true || response.Data == null) break;

                    var pullData = response.Data;
                    Console.WriteLine($"[SyncService] Pull layaway-payments page={page} count={pullData.Data.Count}");

                    if (pullData.Data.Count == 0) break;

                    foreach (var dto in pullData.Data)
                    {
                        var existing = await _layawayPaymentRepo.FirstOrDefaultAsync(p => p.Folio == dto.Folio);
                        if (existing != null)
                        {
                            existing.SyncStatus = 2;
                            await _layawayPaymentRepo.UpdateAsync(existing);
                        }
                        else
                        {
                            if (string.IsNullOrEmpty(dto.LayawayFolio))
                            {
                                Console.WriteLine($"[SyncService] Abono {dto.Folio} sin layaway_folio — omitido");
                                continue;
                            }

                            var localLayaway = await _layawayRepo.FirstOrDefaultAsync(l => l.Folio == dto.LayawayFolio);
                            if (localLayaway == null)
                            {
                                Console.WriteLine($"[SyncService] Abono {dto.Folio}: apartado '{dto.LayawayFolio}' no encontrado localmente — omitido");
                                continue;
                            }

                            var payment = new LayawayPayment
                            {
                                Folio          = dto.Folio,
                                LayawayId      = localLayaway.Id, // ID LOCAL correcto
                                UserId         = dto.UserId,
                                AmountPaid     = dto.AmountPaid,
                                PaymentMethod  = dto.PaymentMethod,
                                PaymentDate    = dto.PaymentDate,
                                CashCloseFolio = dto.CashCloseFolio,
                                Notes          = dto.Notes,
                                SyncStatus     = 2,
                            };
                            await _layawayPaymentRepo.AddAsync(payment);
                        }
                    }

                    totalPulled += pullData.Count;
                    if (!pullData.HasMorePages) break;
                    page++;
                }

                Console.WriteLine($"[SyncService] Pull layaway-payments completado: {totalPulled} registros");
                return SyncResult.Ok("layaway-payments", pulled: totalPulled);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[SyncService] Error Pull layaway-payments: {ex.Message}");
                return SyncResult.Fail("layaway-payments", ex.Message);
            }
        }

        private async Task<SyncResult> PullAsync<T>(
            string entity,
            long since,
            BaseRepository<T> repo,
            CancellationToken ct,
            string extraParams = "") where T : new()
        {
            int totalPulled = 0;
            int page        = 1;
            Console.WriteLine($"[SyncService] Iniciando Pull {entity} since={since}");

            try
            {
                while (true)
                {
                    var endpoint = $"/api/v1/sync/pull/{entity}?since={since}&page={page}{extraParams}";
                    var response = await _apiClient.GetAsync<PullResponse<T>>(endpoint, ct);

                    if (response?.IsSuccess != true || response.Data == null)
                        break;

                    var pullData = response.Data;

                    Console.WriteLine($"[SyncService] Pull {entity} page={page} count={pullData.Data.Count}");

                    if (pullData.Data.Count == 0)
                        break;

                    foreach (var item in pullData.Data)
                    {
                        Console.WriteLine($"[SyncService] Item recibido tipo={typeof(T).Name}");
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
                var all     = await repo.GetAllAsync();
                var pending = all.Where(x => GetSyncStatus(x) == 1).ToList();

                if (pending.Count == 0)
                    return SyncResult.Ok(entity);

                int accepted = 0;
                int rejected = 0;

                var branchId = _configService.AppConfig.CurrentBranchId ?? 0;

                const int batchSize = 100;
                for (int i = 0; i < pending.Count; i += batchSize)
                {
                    var batch = pending.GetRange(i, Math.Min(batchSize, pending.Count - i));
                    var body  = new { branch_id = branchId, records = batch };

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
    
        /// Pull completo con since=0 — descarga y sobreescribe todo el catálogo.
        /// onProgress recibe (label, índice actual, total de entidades).
        public async Task<List<SyncResult>> PullCatalogFullAsync(
            Action<string, int, int>? onProgress = null,
            CancellationToken ct = default)
        {
            var results = new List<SyncResult>();

            var steps = new (string Endpoint, string Label, Func<Task<SyncResult>> Action)[]
            {
                ("categories", "Categorías",  () => PullAsync("categories", 0, _categoryRepo,  ct)),
                ("units",      "Unidades",    () => PullAsync("units",      0, _unitRepo,       ct)),
                ("branches",   "Sucursales",  () => PullAsync("branches",   0, _branchRepo,     ct)),
                ("suppliers",  "Proveedores", () => PullAsync("suppliers",  0, _supplierRepo,   ct)),
                ("customers",  "Clientes",    () => PullAsync("customers",  0, _customerRepo,   ct)),
                ("users",      "Usuarios",    () => PullUsersAsync(0, ct)),
                ("products",   "Productos",   () => PullAsync("products",   0, _productRepo,    ct)),
            };

            for (int i = 0; i < steps.Length; i++)
            {
                if (ct.IsCancellationRequested) break;

                var (_, label, action) = steps[i];
                onProgress?.Invoke(label, i + 1, steps.Length);

                results.Add(await action());
            }

            return results;
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

        /// Push inmediato de un cliente al servidor — se llama al crear o modificar un cliente.
        /// No es batch — es una operación individual en tiempo real.
        public async Task<bool> PushCustomerAsync(Customer customer, CancellationToken ct = default)
        {
            try
            {
                if (!await _apiClient.IsServerAvailableAsync())
                {
                    Console.WriteLine("[SyncService] PushCustomer omitido — sin conexión");
                    return false;
                }

                var folio = $"CUST-{customer.Id}-{DateTimeOffset.UtcNow.ToUnixTimeSeconds()}";

                var dto = new CustomerPushDto
                {
                    Folio           = folio,
                    Id              = customer.Id,
                    Name            = customer.Name,
                    Rfc             = customer.Rfc ?? string.Empty,
                    Street          = customer.Street ?? string.Empty,
                    ExteriorNumber  = customer.ExteriorNumber ?? string.Empty,
                    InteriorNumber  = customer.InteriorNumber,
                    Neighborhood    = customer.Neighborhood ?? string.Empty,
                    PostalCode      = customer.PostalCode ?? string.Empty,
                    City            = customer.City ?? string.Empty,
                    Email           = customer.Email ?? string.Empty,
                    Phone           = customer.Phone,
                    Active          = customer.Active,
                };

                var body     = new { records = new[] { dto } };
                var response = await _apiClient.PostAsync<CustomerPushResponse>(
                    "/api/v1/sync/push/customers", body, ct);

                if (response?.Data?.Accepted.Count > 0)
                {
                    var accepted = response.Data.Accepted[0];

                    // Si el servidor asignó un id diferente al local, actualizar
                    if (accepted.ServerId > 0 && accepted.ServerId != customer.Id)
                    {
                        Console.WriteLine($"[SyncService] Actualizando id local {customer.Id} → servidor {accepted.ServerId}");

                        // Borrar el registro local con id temporal
                        await _customerRepo.DeleteAsync(customer);

                        // Insertar respetando el id del servidor usando repositorio
                        customer.Id         = accepted.ServerId;
                        customer.SyncStatus = 2;
                        customer.LastSync   = DateTime.Now;
                        await _customerRepo.AddWithExplicitIdAsync(customer);

                        Console.WriteLine($"[SyncService] Cliente '{customer.Name}' guardado con id servidor: {accepted.ServerId}");
                    }
                    else
                    {
                        SetSyncStatus(customer, 2);
                        SetLastSync(customer, DateTime.Now);
                        await _customerRepo.UpdateAsync(customer);
                    }

                    Console.WriteLine($"[SyncService] Cliente '{customer.Name}' enviado ✅ (id servidor: {accepted.ServerId})");
                    return true;
                }

                Console.WriteLine("[SyncService] Cliente rechazado por el servidor");
                return false;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[SyncService] Error PushCustomer: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Push inmediato de un usuario al servidor.
        /// Se llama al crear un usuario desde el POS para que esté disponible
        /// en otras terminales sin esperar el sync del siguiente login.
        /// Usa username como identificador (no folio) porque el ID viene del servidor.
        /// </summary>
        public async Task<bool> PushUserAsync(User user, CancellationToken ct = default)
        {
            try
            {
                if (!await _apiClient.IsServerAvailableAsync())
                {
                    Console.WriteLine("[SyncService] PushUser omitido — sin conexión");
                    return false;
                }

                var branchId = _configService.AppConfig.CurrentBranchId ?? 0;
                var body = new { branch_id = branchId, records = new[] { user } };

                var response = await _apiClient.PostAsync<PushResponse>(
                    "/api/v1/sync/push/users", body, ct);

                if (response?.Data?.Accepted.Contains(user.Username) == true)
                {
                    SetSyncStatus(user, 2);
                    SetLastSync(user, DateTime.Now);
                    await _userRepo.UpdateAsync(user);
                    Console.WriteLine($"[SyncService] Usuario '{user.Username}' enviado al servidor ✅");
                    return true;
                }

                Console.WriteLine($"[SyncService] Usuario '{user.Username}' rechazado por el servidor");
                return false;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[SyncService] Error PushUser: {ex.Message}");
                return false;
            }
        }
    }
}