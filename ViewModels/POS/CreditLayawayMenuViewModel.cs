using System;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace CasaCejaRemake.ViewModels.POS
{
    public enum CreditsLayawaysOption
    {
        List = 1,           // F1 - Lista de Creditos/Apartados
        NewOrPayment = 2,   // F2 - Nuevo o Abono
        CustomerList = 3    // F3 - Lista de Clientes
    }

    public partial class CreditsLayawaysMenuViewModel : ViewModelBase
    {
        public event EventHandler<CreditsLayawaysOption>? OptionSelected;
        public event EventHandler? Cancelled;

        [RelayCommand]
        private void SelectList()
        {
            OptionSelected?.Invoke(this, CreditsLayawaysOption.List);
        }

        [RelayCommand]
        private void SelectNewOrPayment()
        {
            OptionSelected?.Invoke(this, CreditsLayawaysOption.NewOrPayment);
        }

        [RelayCommand]
        private void SelectCustomerList()
        {
            OptionSelected?.Invoke(this, CreditsLayawaysOption.CustomerList);
        }

        [RelayCommand]
        private void Cancel()
        {
            Cancelled?.Invoke(this, EventArgs.Empty);
        }

        public void HandleKeyPress(string key)
        {
            switch (key.ToUpper())
            {
                case "F1":
                    SelectList();
                    break;
                case "F2":
                    SelectNewOrPayment();
                    break;
                case "F3":
                    SelectCustomerList();
                    break;
                case "ESCAPE":
                    Cancel();
                    break;
            }
        }
    }
}
