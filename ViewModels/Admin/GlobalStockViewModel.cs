using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CasaCejaRemake.Models;
using CasaCejaRemake.Models.DTOs;
using CasaCejaRemake.Services;

namespace CasaCejaRemake.ViewModels.Admin
{
    public partial class GlobalStockViewModel : ViewModelBase
    {
        private readonly ApiClient _apiClient;
        private CancellationTokenSource? _searchCts;
        private List<ProductStockDto> _rawPageItems = new(); // items sin filtro de la página actual

        private const int PerPage = 100;

        [ObservableProperty] private ObservableCollection<ProductStockDto> _stockItems = new();
        [ObservableProperty] private ObservableCollection<Branch> _availableBranches = new();
        [ObservableProperty] private Branch? _selectedBranch;
        [ObservableProperty] private string _searchText = string.Empty;
        [ObservableProperty] private bool _isLoading;
        [ObservableProperty] private string _statusMessage = string.Empty;
        [ObservableProperty] private int _currentPage = 1;
        [ObservableProperty] private int _totalPages = 1;
        [ObservableProperty] private int _totalItems;
        [ObservableProperty] private bool _canGoPrevious;
        [ObservableProperty] private bool _canGoNext;
        [ObservableProperty] private bool _showCriticalOnly;

        public event EventHandler? GoBackRequested;
        public event EventHandler? ExportRequested;
        public event EventHandler<string>? NetworkErrorOccurred;

        public GlobalStockViewModel(ApiClient apiClient, List<Branch> branches)
        {
            _apiClient = apiClient ?? throw new ArgumentNullException(nameof(apiClient));

            // Entrada "Todas las sucursales" con Id=0
            var allBranches = new List<Branch>
            {
                new Branch { Id = 0, Name = "Todas las sucursales" }
            };
            allBranches.AddRange(branches.Where(b => b.Active));

            AvailableBranches = new ObservableCollection<Branch>(allBranches);
            _selectedBranch   = AvailableBranches.First();
        }

        partial void OnSelectedBranchChanged(Branch? value)
        {
            _ = LoadAsync(1);
        }

        partial void OnSearchTextChanged(string value)
        {
            _searchCts?.Cancel();
            _searchCts = new CancellationTokenSource();
            var token  = _searchCts.Token;

            _ = Task.Delay(400, token).ContinueWith(t =>
            {
                if (!t.IsCanceled)
                    _ = LoadAsync(1);
            }, TaskScheduler.FromCurrentSynchronizationContext());
        }

        partial void OnShowCriticalOnlyChanged(bool value)
        {
            // Filtro client-side: aplica sobre los ítems ya descargados de la página actual
            ApplyCriticalFilter();
            UpdateStatusMessage();
        }

        [RelayCommand]
        public async Task LoadAsync(int page = 1)
        {
            IsLoading = true;
            StatusMessage = "Cargando existencias...";

            try
            {
                var url = BuildUrl(page);
                var response = await _apiClient.GetAsync<PagedProductStockResponse>(url);

                if (response == null || !response.IsSuccess || response.Data == null)
                {
                    var errorMsg = response == null
                        ? "Sin conexión al servidor. Verifica tu red e intenta de nuevo."
                        : "No se pudieron cargar las existencias.";
                    StatusMessage = string.Empty;
                    _rawPageItems = new List<ProductStockDto>();
                    StockItems = new ObservableCollection<ProductStockDto>();
                    UpdatePagination(page, 1, 0);
                    NetworkErrorOccurred?.Invoke(this, errorMsg);
                    return;
                }

                var paged = response.Data;
                _rawPageItems = paged.Data;
                ApplyCriticalFilter();
                UpdatePagination(paged.CurrentPage, paged.LastPage, paged.Total);
                UpdateStatusMessage();
            }
            catch (Exception ex)
            {
                StatusMessage = $"Error al cargar existencias: {ex.Message}";
                Console.WriteLine($"[GlobalStockVM] Error: {ex.Message}");
            }
            finally
            {
                IsLoading = false;
            }
        }

        private void ApplyCriticalFilter()
        {
            var items = ShowCriticalOnly
                ? _rawPageItems.Where(x => x.Quantity <= 0).ToList()
                : _rawPageItems;

            StockItems = new ObservableCollection<ProductStockDto>(items);
        }

        private void UpdateStatusMessage()
        {
            if (ShowCriticalOnly)
                StatusMessage = $"{StockItems.Count} producto(s) crítico(s) en esta página · {TotalItems} total con existencias";
            else
                StatusMessage = $"{TotalItems} producto(s) con existencias";
        }

        private string BuildUrl(int page)
        {
            var qs = new System.Text.StringBuilder("/api/v1/admin/reports/product-stock?");
            qs.Append($"page={page}&per_page={PerPage}");

            if (SelectedBranch != null && SelectedBranch.Id != 0)
                qs.Append($"&branch_id={SelectedBranch.Id}");

            if (!string.IsNullOrWhiteSpace(SearchText))
                qs.Append($"&search={Uri.EscapeDataString(SearchText.Trim())}");

            return qs.ToString();
        }

        private void UpdatePagination(int current, int last, int total)
        {
            CurrentPage   = current;
            TotalPages    = last < 1 ? 1 : last;
            TotalItems    = total;
            CanGoPrevious = CurrentPage > 1;
            CanGoNext     = CurrentPage < TotalPages;
            NextPageCommand.NotifyCanExecuteChanged();
            PreviousPageCommand.NotifyCanExecuteChanged();
        }

        [RelayCommand(CanExecute = nameof(CanGoNext))]
        private async Task NextPageAsync() => await LoadAsync(CurrentPage + 1);

        [RelayCommand(CanExecute = nameof(CanGoPrevious))]
        private async Task PreviousPageAsync() => await LoadAsync(CurrentPage - 1);

        [RelayCommand]
        private void GoBack() => GoBackRequested?.Invoke(this, EventArgs.Empty);

        [RelayCommand]
        private void Export() => ExportRequested?.Invoke(this, EventArgs.Empty);
    }
}
