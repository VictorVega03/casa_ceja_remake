using System;
using System.Collections.Generic;
using System.Linq;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using CasaCejaRemake.Models;
using CasaCejaRemake.ViewModels.POS;
using CasaCejaRemake.Services;
using casa_ceja_remake.Helpers;

namespace CasaCejaRemake.Views.POS
{
    public partial class CashCloseView : Window
    {
        private CashCloseViewModel? _viewModel;
        private string? _ticketText;

        public CashCloseView()
        {
            InitializeComponent();
            Loaded += OnLoaded;
        }

        private async void OnLoaded(object? sender, RoutedEventArgs e)
        {
            _viewModel = DataContext as CashCloseViewModel;
            
            if (_viewModel != null)
            {
                _viewModel.CloseCompleted += OnCloseCompleted;
                _viewModel.Cancelled += OnCancelled;
                
                // Cargar datos automáticamente
                await _viewModel.LoadDataCommand.ExecuteAsync(null);
            }

            // Validación numérica para el campo de monto declarado
            TxtDeclaredAmount.AddHandler(TextInputEvent, OnAmountTextInput, RoutingStrategies.Tunnel);

            // Enfocar el campo de monto declarado
            TxtDeclaredAmount.Focus();
            TxtDeclaredAmount.SelectAll();
        }

        private void OnAmountTextInput(object? sender, TextInputEventArgs e)
        {
            // Permitir solo números y un punto decimal
            if (string.IsNullOrEmpty(e.Text))
                return;

            foreach (char c in e.Text)
            {
                // Permitir números
                if (char.IsDigit(c))
                    continue;

                // Permitir un solo punto decimal
                if (c == '.' && TxtDeclaredAmount.Text?.Contains('.') == false)
                    continue;

                // Rechazar cualquier otro carácter
                e.Handled = true;
                return;
            }
        }

        private void OnCloseCompleted(object? sender, CashClose cashClose)
        {
            // Generar ticket de corte
            GenerateCashCloseTicket(cashClose);
            
            // Guardar el ticket y el resultado
            Tag = new CashCloseResult 
            { 
                CashClose = cashClose, 
                TicketText = _ticketText 
            };
            Close();
        }

        private void GenerateCashCloseTicket(CashClose cashClose)
        {
            try
            {
                var ticketService = new TicketService();
                
                // Parsear gastos e ingresos del JSON
                var expenses = new List<(string Concept, decimal Amount)>();
                var incomes = new List<(string Concept, decimal Amount)>();
                
                if (!string.IsNullOrEmpty(cashClose.Expenses) && cashClose.Expenses != "[]")
                {
                    try
                    {
                        var expensesList = System.Text.Json.JsonSerializer.Deserialize<List<ExpenseIncomeItem>>(cashClose.Expenses);
                        if (expensesList != null)
                        {
                            expenses = expensesList.Select(e => (e.description ?? e.Concept ?? "", e.amount)).ToList();
                        }
                    }
                    catch { }
                }
                
                if (!string.IsNullOrEmpty(cashClose.Income) && cashClose.Income != "[]")
                {
                    try
                    {
                        var incomesList = System.Text.Json.JsonSerializer.Deserialize<List<ExpenseIncomeItem>>(cashClose.Income);
                        if (incomesList != null)
                        {
                            incomes = incomesList.Select(i => (i.description ?? i.Concept ?? "", i.amount)).ToList();
                        }
                    }
                    catch { }
                }
                
                decimal totalExpenses = expenses.Sum(e => e.Amount);
                decimal totalIncome = incomes.Sum(i => i.Amount);
                
                _ticketText = ticketService.GenerateCashCloseTicketText(
                    branchName: "Casa Ceja", // TODO: Obtener de configuración
                    branchAddress: "",
                    branchPhone: "",
                    folio: cashClose.Folio,
                    userName: _viewModel?.UserName ?? "Usuario",
                    openingDate: cashClose.OpeningDate,
                    closeDate: cashClose.CloseDate,
                    openingCash: cashClose.OpeningCash,
                    totalCash: cashClose.TotalCash,
                    totalDebit: cashClose.TotalDebitCard,
                    totalCredit: cashClose.TotalCreditCard,
                    totalTransfer: cashClose.TotalTransfers,
                    totalChecks: cashClose.TotalChecks,
                    layawayCash: cashClose.LayawayCash,
                    creditCash: cashClose.CreditCash,
                    creditTotalCreated: cashClose.CreditTotalCreated,
                    layawayTotalCreated: cashClose.LayawayTotalCreated,
                    totalExpenses: totalExpenses,
                    totalIncome: totalIncome,
                    expectedCash: cashClose.ExpectedCash,
                    declaredAmount: cashClose.ExpectedCash + cashClose.Surplus,
                    difference: cashClose.Surplus,
                    salesCount: _viewModel?.SalesCount ?? 0,
                    expenses: expenses,
                    incomes: incomes
                );
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[CashCloseView] Error generando ticket: {ex.Message}");
                _ticketText = null;
            }
        }

        private void OnCancelled(object? sender, EventArgs e)
        {
            Tag = null;
            Close();
        }

        protected override void OnKeyDown(KeyEventArgs e)
        {
            if (_viewModel != null)
            {
                var shortcuts = new Dictionary<Key, Action>
                {
                    {Key.F5, () => _viewModel.ConfirmCommand.Execute(null) },
                    { Key.Escape, () => _viewModel.CancelCommand.Execute(null) }
                };

                if (KeyboardShortcutHelper.HandleShortcut(e, shortcuts))
                {
                    return;
                }
            }

            base.OnKeyDown(e);
        }

        protected override void OnClosed(EventArgs e)
        {
            if (_viewModel != null)
            {
                _viewModel.CloseCompleted -= OnCloseCompleted;
                _viewModel.Cancelled -= OnCancelled;
            }
            base.OnClosed(e);
        }
    }

    // Clase auxiliar para deserializar gastos/ingresos
    public class ExpenseIncomeItem
    {
        public string? description { get; set; }
        public string? Concept { get; set; }
        public decimal amount { get; set; }
    }

    // Resultado del corte con ticket
    public class CashCloseResult
    {
        public CashClose? CashClose { get; set; }
        public string? TicketText { get; set; }
    }
}
