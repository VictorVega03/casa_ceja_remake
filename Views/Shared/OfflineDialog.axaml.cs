using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;

namespace CasaCejaRemake.Views.Shared
{
    public partial class OfflineDialog : Window
    {
        public OfflineDialog()
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
            Width = Math.Max(400, Math.Min(640, area.Width * 0.28));
            Height = Math.Max(400, Math.Min(540, area.Height * 0.50));

            var posX = area.X + (int)((area.Width - Width) / 2);
            var posY = area.Y + (int)((area.Height - Height) / 2);
            Position = new PixelPoint(posX, posY);
        }

        private void OnContinueClick(object? sender, RoutedEventArgs e)
        {
            Tag = "continue";
            Close();
        }
    }
}