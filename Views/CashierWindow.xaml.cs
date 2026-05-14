using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media.Imaging;
using Microsoft.EntityFrameworkCore;
using MyHippocrates.Data;
using MyHippocrates.Models;

namespace MyHippocrates.Views
{
    public partial class CashierWindow : Window, INotifyPropertyChanged
    {
        private readonly AppDbContext _ctx;
        private readonly SystemUser _user;
        private Receipt? _currentReceipt;
        private Pharmacy? _selectedPharmacy;
        private string _searchText = "";

        public event PropertyChangedEventHandler? PropertyChanged;

        public ObservableCollection<Pharmacy> Pharmacies { get; } = new();
        public ObservableCollection<CashierProductCard> ProductCards { get; } = new();
        public ObservableCollection<CashierReceiptLine> CurrentLines { get; } = new();

        public string CashierName => _user.Employee?.FullName ?? _user.Login;

        public Pharmacy? SelectedPharmacy
        {
            get => _selectedPharmacy;
            set
            {
                if (_selectedPharmacy?.Id == value?.Id) return;
                _selectedPharmacy = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(CanOpenReceipt));
                ReloadProducts();
            }
        }

        public string SearchText
        {
            get => _searchText;
            set
            {
                if (_searchText == value) return;
                _searchText = value;
                OnPropertyChanged();
                ReloadProducts();
            }
        }

        public bool HasOpenReceipt => _currentReceipt != null;
        public bool CanOpenReceipt => _currentReceipt == null && SelectedPharmacy != null;
        public bool CanCloseReceipt => _currentReceipt != null && CurrentLines.Count > 0;
        public string ProductSummary => $"{ProductCards.Count} товаров";
        public string ReceiptInfo => _currentReceipt == null
            ? "Откройте чек перед продажей"
            : $"№ {_currentReceipt.ReceiptNumber} от {_currentReceipt.Date:dd.MM.yyyy} • {CashierName}";
        public decimal Total => CurrentLines.Sum(x => x.Total);
        public string TotalText => $"{Total:F2} MDL";

        public CashierWindow(AppDbContext ctx, SystemUser user)
        {
            InitializeComponent();
            _ctx = ctx;
            _user = user;
            DataContext = this;

            LoadPharmacies();
            ReloadProducts();
            RefreshReceiptState();
        }

        private void LoadPharmacies()
        {
            Pharmacies.Clear();
            foreach (var pharmacy in _ctx.Pharmacies.OrderBy(p => p.Id).ToList())
                Pharmacies.Add(pharmacy);

            SelectedPharmacy = Pharmacies.FirstOrDefault();
        }

        private void ReloadProducts()
        {
            ProductCards.Clear();
            if (SelectedPharmacy == null) return;

            IQueryable<StockBalance> query = _ctx.StockBalances
                .AsNoTracking()
                .Include(s => s.Product)
                .ThenInclude(p => p!.Category)
                .Where(s => s.PharmacyId == SelectedPharmacy.Id && s.RemainingQty > 0);

            if (!string.IsNullOrWhiteSpace(SearchText))
            {
                var search = SearchText.Trim().ToLower();
                query = query.Where(s =>
                    s.Product!.Name.ToLower().Contains(search) ||
                    (s.Product.Category != null && s.Product.Category.Name.ToLower().Contains(search)));
            }

            foreach (var stock in query.OrderBy(s => s.Product!.Name).ToList())
            {
                if (stock.Product != null)
                    ProductCards.Add(new CashierProductCard(stock.Product, stock.RemainingQty));
            }

            OnPropertyChanged(nameof(ProductSummary));
        }

        private void ReloadReceipt()
        {
            CurrentLines.Clear();
            if (_currentReceipt == null)
            {
                RefreshReceiptState();
                return;
            }

            var lines = _ctx.OrderItems
                .AsNoTracking()
                .Include(o => o.Product)
                .Where(o => o.ReceiptId == _currentReceipt.Id)
                .OrderBy(o => o.Product!.Name)
                .ToList();

            foreach (var line in lines)
                CurrentLines.Add(new CashierReceiptLine(line));

            _currentReceipt = _ctx.Receipts
                .AsNoTracking()
                .FirstOrDefault(r => r.Id == _currentReceipt.Id);

            RefreshReceiptState();
        }

        private void OpenReceipt_Click(object sender, RoutedEventArgs e)
        {
            if (SelectedPharmacy == null || _user.Employee == null) return;

            try
            {
                var receipt = new Receipt
                {
                    PharmacyId = SelectedPharmacy.Id,
                    EmployeeId = _user.Employee.Id,
                    Date = DateTime.Today
                };

                receipt.Id = DbProcedures.AddReceipt(_ctx, receipt);
                _ctx.ChangeTracker.Clear();
                _currentReceipt = _ctx.Receipts
                    .AsNoTracking()
                    .First(r => r.Id == receipt.Id);

                ReloadReceipt();
            }
            catch (Exception ex)
            {
                ShowError(ex);
            }
        }

        private void AddProduct_Click(object sender, RoutedEventArgs e)
        {
            if ((sender as FrameworkElement)?.Tag is not CashierProductCard card) return;
            AddReceiptProduct(card.Product.Id, 1);
        }

        private void IncreaseLine_Click(object sender, RoutedEventArgs e)
        {
            if ((sender as FrameworkElement)?.Tag is not CashierReceiptLine line) return;
            AddReceiptProduct(line.ProductId, 1, line.Discount);
        }

        private void DecreaseLine_Click(object sender, RoutedEventArgs e)
        {
            if (_currentReceipt == null) return;
            if ((sender as FrameworkElement)?.Tag is not CashierReceiptLine line) return;

            try
            {
                if (line.Quantity <= 1)
                {
                    DbProcedures.DeleteOrderItem(_ctx, _currentReceipt.Id, line.ProductId);
                }
                else
                {
                    DbProcedures.UpdateOrderItem(
                        _ctx,
                        _currentReceipt.Id,
                        line.ProductId,
                        new OrderItem
                        {
                            ReceiptId = _currentReceipt.Id,
                            ProductId = line.ProductId,
                            Quantity = line.Quantity - 1,
                            Discount = line.Discount
                        });
                }

                _ctx.ChangeTracker.Clear();
                ReloadReceipt();
                ReloadProducts();
            }
            catch (Exception ex)
            {
                ShowError(ex);
            }
        }

        private void DeleteLine_Click(object sender, RoutedEventArgs e)
        {
            if (_currentReceipt == null) return;
            if ((sender as FrameworkElement)?.Tag is not CashierReceiptLine line) return;

            try
            {
                DbProcedures.DeleteOrderItem(_ctx, _currentReceipt.Id, line.ProductId);
                _ctx.ChangeTracker.Clear();
                ReloadReceipt();
                ReloadProducts();
            }
            catch (Exception ex)
            {
                ShowError(ex);
            }
        }

        private void Discount_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            e.Handled = !e.Text.All(char.IsDigit);
        }

        private void Discount_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key != Key.Enter || sender is not TextBox textBox) return;

            ApplyDiscount(textBox);
            Keyboard.ClearFocus();
        }

        private void Discount_LostFocus(object sender, RoutedEventArgs e)
        {
            if (sender is TextBox textBox)
                ApplyDiscount(textBox);
        }

        private void CloseReceipt_Click(object sender, RoutedEventArgs e)
        {
            if (_currentReceipt == null || CurrentLines.Count == 0) return;

            MessageBox.Show(
                $"Чек закрыт.\n\nФармацевт: {CashierName}\nСумма: {TotalText}",
                "Продажа завершена",
                MessageBoxButton.OK,
                MessageBoxImage.Information);

            _currentReceipt = null;
            CurrentLines.Clear();
            RefreshReceiptState();
        }

        private void AddReceiptProduct(int productId, int quantity, decimal? discount = null)
        {
            if (_currentReceipt == null) return;

            try
            {
                var appliedDiscount = discount ??
                    CurrentLines.FirstOrDefault(x => x.ProductId == productId)?.Discount ??
                    0m;

                DbProcedures.AddOrderItem(_ctx, new OrderItem
                {
                    ReceiptId = _currentReceipt.Id,
                    ProductId = productId,
                    Quantity = quantity,
                    Discount = appliedDiscount
                });

                _ctx.ChangeTracker.Clear();
                ReloadReceipt();
                ReloadProducts();
            }
            catch (Exception ex)
            {
                ShowError(ex);
            }
        }

        private void ApplyDiscount(TextBox textBox)
        {
            if (_currentReceipt == null) return;
            if (textBox.Tag is not CashierReceiptLine line) return;

            var discount = 0m;
            if (!string.IsNullOrWhiteSpace(textBox.Text) &&
                (!decimal.TryParse(textBox.Text, out discount) || discount < 0))
            {
                discount = 0;
            }

            if (discount > 30)
                discount = 30;

            textBox.Text = discount.ToString("0");
            if (discount == line.Discount)
                return;

            try
            {
                DbProcedures.UpdateOrderItem(
                    _ctx,
                    _currentReceipt.Id,
                    line.ProductId,
                    new OrderItem
                    {
                        ReceiptId = _currentReceipt.Id,
                        ProductId = line.ProductId,
                        Quantity = line.Quantity,
                        Discount = discount
                    });

                _ctx.ChangeTracker.Clear();
                ReloadReceipt();
                ReloadProducts();
            }
            catch (Exception ex)
            {
                ShowError(ex);
            }
        }

        private void CancelReceipt_Click(object sender, RoutedEventArgs e)
        {
            if (_currentReceipt == null) return;

            var result = MessageBox.Show(
                "Отменить текущий чек? Все позиции будут удалены, остатки вернутся на склад.",
                "Отмена чека",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);
            if (result != MessageBoxResult.Yes) return;

            try
            {
                DbProcedures.CancelReceipt(_ctx, _currentReceipt.Id);
                _ctx.ChangeTracker.Clear();
                _currentReceipt = null;
                CurrentLines.Clear();
                ReloadProducts();
                RefreshReceiptState();
            }
            catch (Exception ex)
            {
                ShowError(ex);
            }
        }

        private void Logout_Click(object sender, RoutedEventArgs e)
        {
            var login = new ConnectionWindow();
            Application.Current.MainWindow = login;
            login.Show();
            Close();
        }

        private void RefreshReceiptState()
        {
            OnPropertyChanged(nameof(HasOpenReceipt));
            OnPropertyChanged(nameof(CanOpenReceipt));
            OnPropertyChanged(nameof(CanCloseReceipt));
            OnPropertyChanged(nameof(ReceiptInfo));
            OnPropertyChanged(nameof(TotalText));
        }

        private static void ShowError(Exception ex)
        {
            MessageBox.Show(
                ex.InnerException?.Message ?? ex.Message,
                "Ошибка",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
        }

        private void OnPropertyChanged([CallerMemberName] string name = "") =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }

    public sealed class CashierProductCard
    {
        public Product Product { get; }
        public int RemainingQty { get; }
        public string RemainingText => $"В наличии: {RemainingQty} {Product.Unit}";

        public CashierProductCard(Product product, int remainingQty)
        {
            Product = product;
            RemainingQty = remainingQty;
        }
    }

    public sealed class CashierReceiptLine
    {
        public int ProductId { get; }
        public string ProductName { get; }
        public int Quantity { get; }
        public decimal UnitPrice { get; }
        public decimal Discount { get; }
        public decimal Total { get; }
        public string Details => $"{Quantity} × {UnitPrice:F2} MDL" +
                                 (Discount > 0 ? $" • скидка {Discount:F0}%" : "");
        public string TotalText => $"{Total:F2}";

        public CashierReceiptLine(OrderItem item)
        {
            ProductId = item.ProductId;
            ProductName = item.Product?.Name ?? $"Товар #{item.ProductId}";
            Quantity = item.Quantity;
            UnitPrice = item.UnitPrice;
            Discount = item.Discount;
            Total = item.Quantity * item.UnitPrice * (1 - item.Discount / 100m);
        }
    }

    public sealed class ProductImageConverter : IValueConverter
    {
        public object? Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is not string path || string.IsNullOrWhiteSpace(path) || !File.Exists(path))
                return null;

            var image = new BitmapImage();
            image.BeginInit();
            image.CacheOption = BitmapCacheOption.OnLoad;
            image.UriSource = new Uri(path, UriKind.Absolute);
            image.EndInit();
            image.Freeze();
            return image;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) =>
            Binding.DoNothing;
    }

    public sealed class MissingImageVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is string path && !string.IsNullOrWhiteSpace(path) && File.Exists(path))
                return Visibility.Collapsed;

            return Visibility.Visible;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) =>
            Binding.DoNothing;
    }
}
