using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Threading;
using CasaCejaRemake.ViewModels.Inventory;
using System;
using System.Linq;

namespace CasaCejaRemake.Views.Inventory
{
    public partial class ConfirmEntryDetailView : Window
    {
        public bool Confirmed { get; private set; }

        private DispatcherTimer? _quantityTimer;
        private Key _currentArrowKey;

        public ConfirmEntryDetailView()
        {
            InitializeComponent();
            this.AddHandler(InputElement.KeyDownEvent, OnPreviewKeyDown, RoutingStrategies.Tunnel, handledEventsToo: true);
            this.AddHandler(InputElement.KeyUpEvent, OnPreviewKeyUp, RoutingStrategies.Tunnel, handledEventsToo: true);
        }

        protected override void OnOpened(EventArgs e)
        {
            base.OnOpened(e);

            if (CancelBtn != null)
                CancelBtn.Click += (_, __) => Close();

            if (ConfirmBtn != null)
                ConfirmBtn.Click += (_, __) =>
                {
                    ApplyNotesToEntry();
                    Confirmed = true;
                    Close();
                };
        }

        private void ApplyNotesToEntry()
        {
            if (DataContext is ConfirmEntryDetailViewModel vm)
                vm.Entry.Notes = string.IsNullOrWhiteSpace(vm.Notes) ? null : vm.Notes.Trim();
        }

        private void OnPreviewKeyDown(object? sender, KeyEventArgs e)
        {
            var vm = DataContext as ConfirmEntryDetailViewModel;

            // Flechas: no interferir cuando el foco está en un TextBox
            if ((e.Key == Key.Left || e.Key == Key.Right) && e.Source is TextBox)
                return;

            if ((e.Key == Key.Left || e.Key == Key.Right) && vm?.SelectedLine != null)
            {
                HandleQuantityArrowKey(e.Key, vm);
                e.Handled = true;
                return;
            }

            if (e.Key == Key.Escape)
            {
                Close();
                e.Handled = true;
                return;
            }

            if (e.Key == Key.Enter && e.Source is not TextBox)
            {
                ApplyNotesToEntry();
                Confirmed = true;
                Close();
                e.Handled = true;
            }
        }

        private void OnPreviewKeyUp(object? sender, KeyEventArgs e)
        {
            if ((e.Key == Key.Left || e.Key == Key.Right) && _quantityTimer?.IsEnabled == true)
            {
                _quantityTimer.Stop();
                e.Handled = true;
            }
        }

        private void HandleQuantityArrowKey(Key key, ConfirmEntryDetailViewModel vm)
        {
            _currentArrowKey = key;
            ChangeQuantityByArrow(key, vm);

            _quantityTimer ??= new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(140) };
            _quantityTimer.Tick -= OnQuantityTimerTick;
            _quantityTimer.Tick += OnQuantityTimerTick;

            if (!_quantityTimer.IsEnabled)
                _quantityTimer.Start();
        }

        private void OnQuantityTimerTick(object? sender, EventArgs e)
        {
            if (DataContext is ConfirmEntryDetailViewModel vm)
                ChangeQuantityByArrow(_currentArrowKey, vm);
        }

        private static void ChangeQuantityByArrow(Key key, ConfirmEntryDetailViewModel vm)
        {
            var line = vm.SelectedLine;
            if (line == null) return;

            if (key == Key.Left)
                line.ReceivedQuantity = Math.Max(0, line.ReceivedQuantity - 1);
            else if (key == Key.Right)
                line.ReceivedQuantity++;
        }

        private void OnQtyTextChanged(object? sender, TextChangedEventArgs e)
        {
            if (sender is not TextBox tb) return;
            var text = tb.Text ?? string.Empty;
            var clean = new string(text.Where(char.IsDigit).ToArray());
            if (clean != text)
                tb.Text = clean;
        }

        private void OnQtyLostFocus(object? sender, RoutedEventArgs e)
        {
            if (sender is not TextBox tb) return;
            if (string.IsNullOrWhiteSpace(tb.Text) || !int.TryParse(tb.Text, out var val) || val < 0)
            {
                tb.Text = "0";
                if (tb.DataContext is ConfirmLineItem line)
                    line.ReceivedQuantity = 0;
            }
        }
    }
}
