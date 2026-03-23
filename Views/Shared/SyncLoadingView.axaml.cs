using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Avalonia.Threading;
using CasaCejaRemake.ViewModels.Shared;

namespace CasaCejaRemake.Views.Shared
{
    public partial class SyncLoadingView : Window
    {
        public SyncLoadingView()
        {
            InitializeComponent();
            Opened += (_, _) => ApplyResponsiveSize();
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }

        private void ApplyResponsiveSize()
        {
            var screen = Screens.ScreenFromWindow(this) ?? Screens.Primary;
            if (screen == null) return;

            var area = screen.WorkingArea;
            Width = Math.Max(380, Math.Min(680, area.Width * 0.30));
            Height = Math.Max(260, Math.Min(520, area.Height * 0.34));

            var posX = area.X + (int)((area.Width - Width) / 2);
            var posY = area.Y + (int)((area.Height - Height) / 2);
            Position = new PixelPoint(posX, posY);
        }

        public void StartSync()
        {
            if (DataContext is SyncLoadingViewModel vm)
            {
                vm.SyncCompleted += (s, e) =>
                {
                    Dispatcher.UIThread.Post(() =>
                    {
                        Tag = "completed";
                        Close();
                    });
                };

                vm.ServerUnavailable += (s, e) =>
                {
                    Console.WriteLine("[SyncLoading] ServerUnavailable disparado");
                    Dispatcher.UIThread.Post(async () =>
                    {
                        Console.WriteLine("[SyncLoading] Mostrando OfflineDialog");
                        var dialog = new OfflineDialog();
                        await dialog.ShowDialog(this);
                        Tag = "completed";
                        Close();
                    });
                };

                _ = vm.StartAsync();
            }
        }
    }
}