using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CasaCejaRemake.Models;
using CasaCejaRemake.Services;
using CasaCejaRemake.Data.Repositories;

namespace CasaCejaRemake.ViewModels.Shared
{
    /// <summary>
    /// Wrapper para mostrar datos de cortes de caja en la lista.
    /// </summary>
    public class CashCloseListItemWrapper
    {
        public CashClose CashClose { get; set; } = null!;
        public string UserName { get; set; } = string.Empty;
        public string BranchName { get; set; } = string.Empty;

        public int Id => CashClose.Id;
        public string Folio => CashClose.Folio;
        public DateTime CloseDate => CashClose.CloseDate;
        public DateTime OpeningDate => CashClose.OpeningDate;
        public decimal OpeningCash => CashClose.OpeningCash;
        public decimal TotalDelCorte => CashClose.TotalDelCorte;
        public decimal ExpectedCash => CashClose.ExpectedCash;
        public decimal Surplus => CashClose.Surplus;
        
        public string SurplusStatus => CashClose.Surplus switch
        {
            > 0 => "SOBRANTE",
            < 0 => "FALTANTE",
            _ => "CUADRADO"
        };

        public string SurplusColor => CashClose.Surplus switch
        {
            > 0 => "#4CAF50", // Verde - sobrante
            < 0 => "#F44336", // Rojo - faltante
            _ => "#AAAAAA"    // Gris - cuadrado
        };
    }

    /// <summary>
    /// ViewModel para la vista de historial de cortes de caja.
    /// </summary>
    public partial class CashCloseHistoryViewModel : ViewModelBase
    {
        private readonly CashCloseService _cashCloseService;
        private readonly AuthService _authService;
        private readonly BaseRepository<User> _userRepository;
        private readonly BaseRepository<Branch> _branchRepository;
        private readonly int _branchId;
        private readonly CasaCejaRemake.Services.ApiClient? _apiClient;
        private List<CashCloseListItemWrapper> _allItems = new();
        private List<CashCloseListItemWrapper> _filteredItems = new();
        private const int HistoryLimit = 200;
        private bool _isUpdatingFilters;

        [ObservableProperty]
        private CashCloseListItemWrapper? _selectedItem;

        [ObservableProperty]
        private bool _isLoading;

        [ObservableProperty]
        private string _statusMessage = string.Empty;

        [ObservableProperty]
        private int _totalCount;

        [ObservableProperty]
        private string _searchText = string.Empty;

        [ObservableProperty]
        private DateTimeOffset? _filterDateFrom;

        [ObservableProperty]
        private DateTimeOffset? _filterDateTo;

        [ObservableProperty]
        private int _currentPage = 1;

        [ObservableProperty]
        private int _totalPages = 1;

        [ObservableProperty]
        private string _paginationInfo = "Página 1 de 1";

        [ObservableProperty]
        private bool _canGoPrevious;

        [ObservableProperty]
        private bool _canGoNext;

        [ObservableProperty]
        private bool _isAdminMode;

        [ObservableProperty]
        private ObservableCollection<Branch> _availableBranches = new();

        [ObservableProperty]
        private Branch? _selectedBranch;

        [ObservableProperty]
        private bool _isOffline;

        [ObservableProperty]
        private string _offlineMessage = string.Empty;

        private const int PageSize = 50;

        public ObservableCollection<CashCloseListItemWrapper> Items { get; } = new();

        public bool HasSelectedItem => SelectedItem != null;

        public event EventHandler<CashCloseListItemWrapper>? ItemSelected;
        public event EventHandler? CloseRequested;
        public event EventHandler? ExportRequested;

        public CashCloseHistoryViewModel(
            CashCloseService cashCloseService,
            AuthService authService,
            BaseRepository<User> userRepository,
            BaseRepository<Branch> branchRepository,
            int branchId,
            bool isAdminMode = false,
            List<Branch>? allBranches = null,
            CasaCejaRemake.Services.ApiClient? apiClient = null)
        {
            _cashCloseService = cashCloseService;
            _authService = authService;
            _userRepository = userRepository;
            _branchRepository = branchRepository;
            _branchId = branchId;
            _isAdminMode = isAdminMode;
            _apiClient = apiClient;

            if (isAdminMode)
            {
                AvailableBranches.Add(new Branch { Id = 0, Name = "Todas las sucursales" });
                foreach (var branch in allBranches ?? new List<Branch>())
                {
                    AvailableBranches.Add(branch);
                }

                _selectedBranch = AvailableBranches.FirstOrDefault();
                OnPropertyChanged(nameof(SelectedBranch));
            }

            // Filtros por defecto: último mes
            _filterDateTo = DateTimeOffset.Now;
            _filterDateFrom = DateTimeOffset.Now.AddDays(-30);
        }

        public async Task InitializeAsync()
        {
            await LoadDataAsync();
        }

        [RelayCommand]
        private async Task LoadDataAsync()
        {
            // Check de conectividad una sola vez al cargar/refrescar
            if (_apiClient != null)
            {
                var online = await _apiClient.IsServerAvailableAsync();
                IsOffline = !online;
                OfflineMessage = online
                    ? string.Empty
                    : "Sin conexión al servidor";
            }

            try
            {
                IsLoading = true;
                _allItems.Clear();

                // Obtener historial de cortes
                var cashCloses = await GetHistoryForCurrentSelectionAsync();

                // Aplicar filtro de fechas si está configurado
                if (FilterDateFrom.HasValue)
                {
                    cashCloses = cashCloses.Where(c => c.CloseDate.Date >= FilterDateFrom.Value.DateTime.Date).ToList();
                }
                if (FilterDateTo.HasValue)
                {
                    cashCloses = cashCloses.Where(c => c.CloseDate.Date <= FilterDateTo.Value.DateTime.Date).ToList();
                }

                // Cargar todos los usuarios y sucursales de una vez para eficiencia
                var allUsers = await _userRepository.GetAllAsync();
                var allBranches = await _branchRepository.GetAllAsync();

                foreach (var cashClose in cashCloses)
                {
                    // Obtener nombre del usuario desde la BD
                    var user = allUsers.FirstOrDefault(u => u.Id == cashClose.UserId);
                    var userName = user?.Name ?? $"Usuario #{cashClose.UserId}";

                    // Obtener nombre de la sucursal desde la BD
                    var branch = allBranches.FirstOrDefault(b => b.Id == cashClose.BranchId);
                    var branchName = branch?.Name ?? $"Sucursal #{cashClose.BranchId}";

                    _allItems.Add(new CashCloseListItemWrapper
                    {
                        CashClose = cashClose,
                        UserName = userName,
                        BranchName = branchName
                    });
                }

                ApplySearch();
            }
            catch (Exception ex)
            {
                StatusMessage = $"Error: {ex.Message}";
            }
            finally
            {
                IsLoading = false;
            }
        }

        private Task<List<CashClose>> GetHistoryForCurrentSelectionAsync()
        {
            if (!IsAdminMode)
                return _cashCloseService.GetHistoryAsync(_branchId, HistoryLimit);

            var branchId = SelectedBranch?.Id ?? 0;
            return branchId <= 0
                ? GetAllBranchesHistoryFromServerAsync()
                : _cashCloseService.GetHistoryFromServerAsync(branchId, HistoryLimit);
        }

        private async Task<List<CashClose>> GetAllBranchesHistoryFromServerAsync()
        {
            var branchIds = AvailableBranches.Where(b => b.Id > 0).Select(b => b.Id).Distinct().ToList();
            if (branchIds.Count == 0)
                return await _cashCloseService.GetAllBranchesHistoryAsync(HistoryLimit);

            var result = new Dictionary<string, CashClose>();
            foreach (var branchId in branchIds)
            {
                var history = await _cashCloseService.GetHistoryFromServerAsync(branchId, HistoryLimit);
                foreach (var cashClose in history)
                {
                    if (!string.IsNullOrWhiteSpace(cashClose.Folio))
                        result[cashClose.Folio] = cashClose;
                }
            }

            return result.Values
                .OrderByDescending(c => c.CloseDate)
                .Take(HistoryLimit)
                .ToList();
        }

        private void ApplySearch()
        {
            var filtered = _allItems.AsEnumerable();

            if (!string.IsNullOrWhiteSpace(SearchText))
            {
                var search = SearchText.Trim().ToLower();
                filtered = filtered.Where(item =>
                    item.Folio.ToLower().Contains(search) ||
                    item.UserName.ToLower().Contains(search) ||
                    item.BranchName.ToLower().Contains(search));
            }

            _filteredItems = filtered.ToList();
            CurrentPage = 1;
            TotalPages = Math.Max(1, (int)Math.Ceiling((double)_filteredItems.Count / PageSize));
            LoadCurrentPage();

            TotalCount = _filteredItems.Count;
            StatusMessage = $"{TotalCount} corte(s) encontrado(s)";
        }

        private void LoadCurrentPage()
        {
            Items.Clear();

            var pageItems = _filteredItems.Skip((CurrentPage - 1) * PageSize).Take(PageSize);
            foreach (var item in pageItems)
            {
                Items.Add(item);
            }

            CanGoPrevious = CurrentPage > 1;
            CanGoNext = CurrentPage < TotalPages;
            PaginationInfo = $"Página {CurrentPage} de {TotalPages}";
        }

        [RelayCommand]
        private void ExecuteSearch()
        {
            ApplySearch();
        }

        [RelayCommand]
        private async Task ApplyDateFilter()
        {
            await LoadDataAsync();
        }

        [RelayCommand]
        private async Task Refresh()
        {
            await LoadDataAsync();
        }

        [RelayCommand]
        private async Task ClearFilters()
        {
            try
            {
                _isUpdatingFilters = true;
                FilterDateFrom = DateTimeOffset.Now.AddDays(-30);
                FilterDateTo = DateTimeOffset.Now;
                SearchText = string.Empty;
            }
            finally
            {
                _isUpdatingFilters = false;
            }

            await LoadDataAsync();
        }

        [RelayCommand]
        private void NextPage()
        {
            if (CurrentPage < TotalPages)
            {
                CurrentPage++;
                LoadCurrentPage();
            }
        }

        [RelayCommand]
        private void PreviousPage()
        {
            if (CurrentPage > 1)
            {
                CurrentPage--;
                LoadCurrentPage();
            }
        }

        partial void OnSelectedItemChanged(CashCloseListItemWrapper? value)
        {
            OnPropertyChanged(nameof(HasSelectedItem));
        }

        partial void OnSelectedBranchChanged(Branch? value)
        {
            _ = LoadDataAsync();
        }

        partial void OnFilterDateFromChanged(DateTimeOffset? value)
        {
            if (!_isUpdatingFilters && value != null && FilterDateTo != null)
                _ = LoadDataAsync();
        }

        partial void OnFilterDateToChanged(DateTimeOffset? value)
        {
            if (!_isUpdatingFilters && value != null && FilterDateFrom != null)
                _ = LoadDataAsync();
        }

        partial void OnSearchTextChanged(string value)
        {
            if (!_isUpdatingFilters)
                ApplySearch();
        }

        [RelayCommand]
        private void SelectItem()
        {
            if (SelectedItem != null)
            {
                ItemSelected?.Invoke(this, SelectedItem);
            }
        }

        [RelayCommand]
        private void ExportToExcel()
        {
            ExportRequested?.Invoke(this, EventArgs.Empty);
        }

        /// <summary>
        /// Prepara un reporte de 2 hojas:
        /// Hoja 1: Resumen de todos los cortes de caja
        /// Hoja 2: Detalles completos de todos los cortes en UNA sola hoja (con separadores)
        /// </summary>
        public async Task<List<ExportSheetData>> PrepareMultiSheetExportAsync(ExportService exportService)
        {
            var sheets = new List<ExportSheetData>();

            // ==== HOJA 1: RESUMEN DE TODOS LOS CORTES ====
            var summaryColumns = new List<ExportColumn<CashCloseListItemWrapper>>
            {
                new() { Header = "Folio", ValueSelector = i => i.Folio, Width = 20 },
                new() { Header = "Usuario", ValueSelector = i => i.UserName, Width = 20 },
                new() { Header = "Fecha Cierre", ValueSelector = i => i.CloseDate, Format = "dd/MM/yyyy HH:mm", Width = 20 },
                new() { Header = "Fondo", ValueSelector = i => i.OpeningCash, Format = "$#,##0.00", Width = 15 },
                new() { Header = "Total Corte", ValueSelector = i => i.TotalDelCorte, Format = "$#,##0.00", Width = 15 },
                new() { Header = "Esperado", ValueSelector = i => i.ExpectedCash, Format = "$#,##0.00", Width = 15 },
                new() { Header = "Diferencia", ValueSelector = i => i.Surplus, Format = "$#,##0.00", Width = 15 },
                new() { Header = "Resultado", ValueSelector = i => i.SurplusStatus, Width = 15 }
            };

            var exportItems = _filteredItems.Count > 0 ? _filteredItems : Items.ToList();

            sheets.Add(exportService.CreateSheetData(
                exportItems,
                summaryColumns,
                "Resumen",
                "Resumen de Cortes de Caja"));

            // ==== HOJA 2: DETALLES DE TODOS LOS CORTES EN UNA SOLA HOJA ====
            var allDetails = new List<(string Label, object Value)>();

            foreach (var item in exportItems)
            {
                var cashClose = item.CashClose;

                // Título del corte
                allDetails.Add(($"═══════════════════════════════════════════════════", ""));
                allDetails.Add(($"CORTE: {cashClose.Folio}", ""));
                allDetails.Add(($"═══════════════════════════════════════════════════", ""));
                allDetails.Add(("", ""));

                // Información general
                allDetails.Add(("Usuario", item.UserName));
                allDetails.Add(("Sucursal", item.BranchName));
                allDetails.Add(("Fecha Apertura", $"{cashClose.OpeningDate:dd/MM/yyyy HH:mm}"));
                allDetails.Add(("Fecha Cierre", $"{cashClose.CloseDate:dd/MM/yyyy HH:mm}"));
                allDetails.Add(("Duración Turno (hrs)", $"{cashClose.ShiftDurationHours:0.00}"));
                allDetails.Add(("", ""));

                // Fondo y Efectivo
                allDetails.Add(("--- FONDO Y EFECTIVO ---", ""));
                allDetails.Add(("Fondo de Apertura", $"{cashClose.OpeningCash:C2}"));
                allDetails.Add(("Efectivo Ventas", $"{cashClose.TotalCash:C2}"));
                allDetails.Add(("Efectivo Abonos Apartados", $"{cashClose.LayawayCash:C2}"));
                allDetails.Add(("Efectivo Abonos Créditos", $"{cashClose.CreditCash:C2}"));
                allDetails.Add(("Efectivo Total", $"{cashClose.EfectivoTotal:C2}"));
                allDetails.Add(("", ""));

                // Métodos de Pago
                allDetails.Add(("--- MÉTODOS DE PAGO ---", ""));
                allDetails.Add(("Tarjeta Débito", $"{cashClose.TotalDebitCard:C2}"));
                allDetails.Add(("Tarjeta Crédito", $"{cashClose.TotalCreditCard:C2}"));
                allDetails.Add(("Cheques", $"{cashClose.TotalChecks:C2}"));
                allDetails.Add(("Transferencias", $"{cashClose.TotalTransfers:C2}"));
                allDetails.Add(("", ""));

                // Créditos y Apartados
                allDetails.Add(("--- CRÉDITOS Y APARTADOS CREADOS ---", ""));
                allDetails.Add(("Total Créditos Creados", $"{cashClose.CreditTotalCreated:C2}"));
                allDetails.Add(("Total Apartados Creados", $"{cashClose.LayawayTotalCreated:C2}"));
                allDetails.Add(("", ""));

                // Totales
                allDetails.Add(("--- TOTALES ---", ""));
                allDetails.Add(("Total del Corte", $"{cashClose.TotalDelCorte:C2}"));
                allDetails.Add(("Total Ventas Directas", $"{cashClose.TotalSales:C2}"));
                allDetails.Add(("Total Pagos", $"{cashClose.TotalPayments:C2}"));
                allDetails.Add(("Total Electrónicos", $"{cashClose.TotalElectronicPayments:C2}"));
                allDetails.Add(("", ""));

                // Arqueo
                allDetails.Add(("--- ARQUEO DE CAJA ---", ""));
                allDetails.Add(("Efectivo Esperado", $"{cashClose.ExpectedCash:C2}"));
                allDetails.Add(("Diferencia", $"{cashClose.Surplus:C2}"));
                allDetails.Add(("Estado", item.SurplusStatus));
                allDetails.Add(("", ""));

                // Notas
                if (!string.IsNullOrWhiteSpace(cashClose.Notes))
                {
                    allDetails.Add(("Observaciones", cashClose.Notes));
                    allDetails.Add(("", ""));
                }

                // Separador entre cortes (espacio en blanco más amplio)
                allDetails.Add(("", ""));
                allDetails.Add(("", ""));
                allDetails.Add(("", ""));
                allDetails.Add(("", ""));
            }

            var detailColumns = new List<ExportColumn<(string Label, object Value)>>
            {
                new() { Header = "Campo", ValueSelector = d => d.Label, Width = 40 },
                new() { Header = "Valor", ValueSelector = d => d.Value, Width = 25 }
            };

            sheets.Add(exportService.CreateSheetData(
                allDetails,
                detailColumns,
                "Detalles",
                "Detalles de Todos los Cortes"));

            return await Task.FromResult(sheets);
        }

        [RelayCommand]
        private void Close()
        {
            CloseRequested?.Invoke(this, EventArgs.Empty);
        }
    }
}
