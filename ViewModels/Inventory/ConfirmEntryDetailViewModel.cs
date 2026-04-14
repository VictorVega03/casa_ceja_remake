using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CasaCejaRemake.Models;

namespace CasaCejaRemake.ViewModels.Inventory
{
    public partial class ConfirmEntryDetailViewModel : ViewModelBase
    {
        public PendingEntryItem Entry { get; }

        [ObservableProperty]
        private string _notes = string.Empty;

        [ObservableProperty]
        private ConfirmLineItem? _selectedLine;

        public ConfirmEntryDetailViewModel(PendingEntryItem entry)
        {
            Entry = entry;
            _notes = entry.Notes ?? string.Empty;
        }

        [RelayCommand]
        private void DecrementLine(ConfirmLineItem? line)
        {
            if (line != null && line.ReceivedQuantity > 0)
                line.ReceivedQuantity--;
        }

        [RelayCommand]
        private void IncrementLine(ConfirmLineItem? line)
        {
            if (line != null)
                line.ReceivedQuantity++;
        }
    }
}
