using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;
using MyHippocrates.Models;

namespace MyHippocrates.Views
{
    public partial class ReceiptPrintSelectionWindow : Window, INotifyPropertyChanged
    {
        private Receipt? _selectedReceipt;

        public event PropertyChangedEventHandler? PropertyChanged;
        public IReadOnlyList<Receipt> Receipts { get; }

        public Receipt? SelectedReceipt
        {
            get => _selectedReceipt;
            set
            {
                _selectedReceipt = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(SelectedReceipt)));
            }
        }

        public ReceiptPrintSelectionWindow(IReadOnlyList<Receipt> receipts)
        {
            InitializeComponent();
            Receipts = receipts;
            SelectedReceipt = receipts.Count > 0 ? receipts[0] : null;
            DataContext = this;
        }

        private void Print_Click(object sender, RoutedEventArgs e)
        {
            if (SelectedReceipt == null)
            {
                MessageBox.Show(
                    "Выберите чек для печати.",
                    "Печать чеков",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);
                return;
            }

            DialogResult = true;
        }

        private void Cancel_Click(object sender, RoutedEventArgs e) => DialogResult = false;

        private void OnPropertyChanged([CallerMemberName] string name = "") =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}
