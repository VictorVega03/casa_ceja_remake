using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CasaCejaRemake.Models;
using CasaCejaRemake.Services;
using CasaCejaRemake.Data;
using CasaCejaRemake.Data.Repositories;

namespace CasaCejaRemake.ViewModels.POS
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
        private readonly DatabaseService _databaseService;
        private readonly int _branchId;
        private ObservableCollection<CashCloseListItemWrapper> _allItems = new();

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
        private DateTime? _filterDateFrom;

        [ObservableProperty]
        private DateTime? _filterDateTo;

        public ObservableCollection<CashCloseListItemWrapper> Items { get; } = new();

        public bool HasSelectedItem => SelectedItem != null;

        public event EventHandler<CashCloseListItemWrapper>? ItemSelected;
        public event EventHandler? CloseRequested;
        public event EventHandler? ExportRequested;

        public CashCloseHistoryViewModel(
            CashCloseService cashCloseService,
            AuthService authService,
            DatabaseService databaseService,
            int branchId)
        {
            _cashCloseService = cashCloseService;
            _authService = authService;
            _databaseService = databaseService;
            _branchId = branchId;

            // Filtros por defecto: último mes
            _filterDateTo = DateTime.Today;
            _filterDateFrom = DateTime.Today.AddDays(-30);
        }

        public async Task InitializeAsync()
        {
            await LoadDataAsync();
        }

        [RelayCommand]
        private async Task LoadDataAsync()
        {
            try
            {
                IsLoading = true;
                _allItems.Clear();

                // Obtener historial de cortes
                var cashCloses = await _cashCloseService.GetHistoryAsync(_branchId, 100);

                // Aplicar filtro de fechas si está configurado
                if (FilterDateFrom.HasValue)
                {
                    cashCloses = cashCloses.Where(c => c.CloseDate.Date >= FilterDateFrom.Value.Date).ToList();
                }
                if (FilterDateTo.HasValue)
                {
                    cashCloses = cashCloses.Where(c => c.CloseDate.Date <= FilterDateTo.Value.Date).ToList();
                }

                // Cargar todos los usuarios y sucursales de una vez para eficiencia
                var userRepository = new BaseRepository<User>(_databaseService);
                var branchRepository = new BaseRepository<Branch>(_databaseService);
                var allUsers = await userRepository.GetAllAsync();
                var allBranches = await branchRepository.GetAllAsync();

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

        private void ApplySearch()
        {
            Items.Clear();

            var filtered = _allItems.AsEnumerable();

            if (!string.IsNullOrWhiteSpace(SearchText))
            {
                var search = SearchText.Trim().ToLower();
                filtered = filtered.Where(item =>
                    item.Folio.ToLower().Contains(search) ||
                    item.UserName.ToLower().Contains(search));
            }

            foreach (var item in filtered)
            {
                Items.Add(item);
            }

            TotalCount = Items.Count;
            StatusMessage = $"{TotalCount} corte(s) encontrado(s)";
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
        private async Task ClearFilters()
        {
            FilterDateFrom = DateTime.Today.AddDays(-30);
            FilterDateTo = DateTime.Today;
            SearchText = string.Empty;
            await LoadDataAsync();
        }

        partial void OnSelectedItemChanged(CashCloseListItemWrapper? value)
        {
            OnPropertyChanged(nameof(HasSelectedItem));
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
        /// Obtiene las columnas de exportación para el reporte de cortes.
        /// </summary>
        public List<ExportColumn<CashCloseListItemWrapper>> GetExportColumns()
        {
            return new List<ExportColumn<CashCloseListItemWrapper>>
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
        }

        [RelayCommand]
        private void Close()
        {
            CloseRequested?.Invoke(this, EventArgs.Empty);
        }
    }
}


