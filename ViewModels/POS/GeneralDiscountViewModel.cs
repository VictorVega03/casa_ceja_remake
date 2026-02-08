using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace CasaCejaRemake.ViewModels.POS
{
    /// <summary>
    /// ViewModel para el diálogo de descuento general sobre la venta.
    /// Permite aplicar un descuento en porcentaje o cantidad fija.
    /// </summary>
    public partial class GeneralDiscountViewModel : ViewModelBase
    {
        [ObservableProperty]
        private decimal _subtotal;

        [ObservableProperty]
        private bool _isPercentage = true;

        [ObservableProperty]
        private decimal _discountValue;

        [ObservableProperty]
        private decimal _calculatedDiscount;

        [ObservableProperty]
        private decimal _finalTotal;

        public GeneralDiscountViewModel(decimal subtotal)
        {
            _subtotal = subtotal;
            _finalTotal = subtotal;
        }

        partial void OnDiscountValueChanged(decimal value)
        {
            CalculateDiscount();
        }

        partial void OnIsPercentageChanged(bool value)
        {
            CalculateDiscount();
        }

        private void CalculateDiscount()
        {
            if (IsPercentage)
            {
                // Limitar porcentaje entre 0 y 100
                var percent = System.Math.Max(0, System.Math.Min(100, DiscountValue));
                CalculatedDiscount = Subtotal * (percent / 100m);
            }
            else
            {
                // Limitar cantidad a no exceder subtotal
                CalculatedDiscount = System.Math.Max(0, System.Math.Min(DiscountValue, Subtotal));
            }
            
            FinalTotal = Subtotal - CalculatedDiscount;
        }

        [RelayCommand]
        private void SetPercentage(decimal value)
        {
            IsPercentage = true;
            DiscountValue = value;
        }

        [RelayCommand]
        private void SetFixed()
        {
            IsPercentage = false;
        }

        [RelayCommand]
        private void Clear()
        {
            DiscountValue = 0;
            IsPercentage = true;
        }
    }
}
