using System;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using CasaCejaRemake.ViewModels.POS;
using casa_ceja_remake.Helpers;

namespace CasaCejaRemake.Views.POS
{
    public partial class AddPaymentView : Window
    {
        private AddPaymentViewModel? _viewModel;

        public AddPaymentView()
        {
            InitializeComponent();
            Loaded += OnLoaded;
            Activated += OnActivated;
        }

        private void OnActivated(object? sender, EventArgs e)
        {
            // Asegurar que la ventana tenga focus
            Focus();
        }

        private void OnLoaded(object? sender, RoutedEventArgs e)
        {
            _viewModel = DataContext as AddPaymentViewModel;
            
            if (_viewModel != null)
            {
                _viewModel.PaymentCompleted += OnPaymentCompleted;
                _viewModel.Cancelled += OnCancelled;
            }

            // Focus y seleccionar todo el texto en el input
            var txtAmount = this.FindControl<TextBox>("TxtCurrentAmount");
            if (txtAmount != null)
            {
                txtAmount.Focus();
                txtAmount.SelectAll();
                
                txtAmount.GotFocus += (s, args) => 
                {
                    if (txtAmount.Text == "0.00")
                    {
                        Avalonia.Threading.Dispatcher.UIThread.Post(() => txtAmount.Text = "");
                    }
                    else
                    {
                        Avalonia.Threading.Dispatcher.UIThread.Post(() => txtAmount.SelectAll());
                    }
                };
            }
        }

        private async void OnPaymentCompleted(object? sender, PaymentResult e)
        {
            // Generar y mostrar ticket de abono
            try
            {
                if (_viewModel != null && e.Success)
                {
                    var app = Avalonia.Application.Current as CasaCejaRemake.App;
                    var configService = app?.GetConfigService();
                    var rfc = configService?.PosTerminalConfig.Rfc ?? string.Empty;

                    // Load branch from DB to get RazonSocial and address
                    var branch = app != null ? await app.GetCurrentBranchAsync() : null;
                    var branchName = branch?.Name ?? configService?.AppConfig.BranchName ?? string.Empty;
                    var branchAddress = branch?.Address ?? string.Empty;
                    var branchRazonSocial = branch?.RazonSocial ?? string.Empty;

                    // Construir JSON de pagos desde la lista de la sesión
                    var paymentDict = new System.Collections.Generic.Dictionary<string, decimal>();
                    foreach (var p in _viewModel.PaymentsList)
                    {
                        string key = p.Method.ToLower()
                            .Replace("á", "a").Replace("é", "e").Replace("í", "i")
                            .Replace("ó", "o").Replace("ú", "u")
                            .Replace(" ", "_");
                        if (paymentDict.ContainsKey(key))
                            paymentDict[key] += p.Amount;
                        else
                            paymentDict[key] = p.Amount;
                    }
                    string paymentJson = System.Text.Json.JsonSerializer.Serialize(paymentDict);

                    var paymentTicketData = new CasaCejaRemake.Services.PaymentTicketData
                    {
                        Folio = e.Folio,
                        OperationFolio = _viewModel.Folio,
                        OperationType = e.IsCredit ? 0 : 1,
                        BranchName = branchName,
                        BranchAddress = branchAddress,
                        BranchRazonSocial = branchRazonSocial,
                        CustomerName = _viewModel.CustomerName,
                        PaymentDate = DateTime.Now,
                        PaymentDetails = paymentJson,
                        TotalPaid = e.AmountPaid,
                        RemainingBalance = _viewModel.CurrentRemaining < 0 ? 0 : _viewModel.CurrentRemaining,
                        UserName = app?.GetAuthService()?.CurrentUser?.Name ?? string.Empty
                    };

                    var ticketService = new CasaCejaRemake.Services.TicketService();
                    var ticketText = ticketService.GeneratePaymentTicketText(paymentTicketData, rfc);
                    await DialogHelper.ShowTicketDialog(this, e.Folio, ticketText);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[AddPaymentView] Error mostrando ticket de abono: {ex.Message}");
            }

            Tag = e;
            Close();
        }

        private void OnCancelled(object? sender, EventArgs e)
        {
            Close();
        }

        protected override void OnKeyDown(KeyEventArgs e)
        {
            if (_viewModel == null)
            {
                base.OnKeyDown(e);
                return;
            }

            // ESC para cancelar
            if (e.Key == Key.Escape)
            {
                _viewModel.CancelCommand.Execute(null);
                e.Handled = true;
                return;
            }

            // F8 para confirmar
            if (e.Key == Key.F8)
            {
                _viewModel.ConfirmCommand.Execute(null);
                e.Handled = true;
                return;
            }

            // F1 para pagar restante (consistente con PaymentView)
            if (e.Key == Key.F1)
            {
                _viewModel.PayRemainingCommand.Execute(null);
                e.Handled = true;
                return;
            }

            // F2-F6 montos rápidos
            if (e.Key == Key.F2) { _viewModel.AddToCurrentCommand.Execute("20"); e.Handled = true; return; }
            if (e.Key == Key.F3) { _viewModel.AddToCurrentCommand.Execute("50"); e.Handled = true; return; }
            if (e.Key == Key.F4) { _viewModel.AddToCurrentCommand.Execute("100"); e.Handled = true; return; }
            if (e.Key == Key.F5) { _viewModel.AddToCurrentCommand.Execute("200"); e.Handled = true; return; }
            if (e.Key == Key.F6) { _viewModel.AddToCurrentCommand.Execute("500"); e.Handled = true; return; }
            
            // F7 para limpiar
            if (e.Key == Key.F7) { _viewModel.ClearCurrentCommand.Execute(null); e.Handled = true; return; }

            // Enter para agregar pago si el textbox está enfocado
            // Eliminar lógica que confirmaba con enter para que sólo sea posible con f8
            if (e.Key == Key.Enter)
            {
                var txtAmount = this.FindControl<TextBox>("TxtCurrentAmount");
                if (txtAmount != null && txtAmount.IsFocused)
                {
                    _viewModel.AddPaymentCommand.Execute(null);
                    e.Handled = true;
                    return;
                }
                return;
            }

            // Teclas 1-4 para seleccionar método de pago
            if (e.Key >= Key.D1 && e.Key <= Key.D4)
            {
                string method = e.Key switch
                {
                    Key.D1 => "Efectivo",
                    Key.D2 => "Debito",
                    Key.D3 => "Credito",
                    Key.D4 => "Transferencia",
                    _ => "Efectivo"
                };
                _viewModel.SelectMethodCommand.Execute(method);
                e.Handled = true;
                return;
            }

            // Flechas para ajustar monto
            if (e.Key == Key.Left)
            {
                _viewModel.AdjustAmount(-50);
                e.Handled = true;
                return;
            }
            if (e.Key == Key.Right)
            {
                _viewModel.AdjustAmount(50);
                e.Handled = true;
                return;
            }
            if (e.Key == Key.Up)
            {
                _viewModel.AdjustAmount(100);
                e.Handled = true;
                return;
            }
            if (e.Key == Key.Down)
            {
                _viewModel.AdjustAmount(-100);
                e.Handled = true;
                return;
            }

            base.OnKeyDown(e);
        }

        protected override void OnClosed(EventArgs e)
        {
            if (_viewModel != null)
            {
                _viewModel.PaymentCompleted -= OnPaymentCompleted;
                _viewModel.Cancelled -= OnCancelled;
            }
            base.OnClosed(e);
        }
    }
}
