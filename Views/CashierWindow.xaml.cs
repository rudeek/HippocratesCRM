using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media.Imaging;
using Microsoft.EntityFrameworkCore;
using MyHippocrates.Data;
using MyHippocrates.Models;
using QRCoder;

namespace MyHippocrates.Views
{
    public partial class CashierWindow : Window, INotifyPropertyChanged
    {
        private readonly AppDbContext _ctx;
        private readonly SystemUser _user;
        private Receipt? _currentReceipt;
        private Pharmacy? _selectedPharmacy;
        private string _searchText = "";
        private string _receiptDiscountText = "0";

        public event PropertyChangedEventHandler? PropertyChanged;
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
        public decimal Subtotal => CurrentLines.Sum(x => x.Total);
        public decimal ReceiptDiscount => ParseReceiptDiscount();
        public decimal Total => Subtotal * (1 - ReceiptDiscount / 100m);
        public string SubtotalText => $"{Subtotal:F2} MDL";
        public string TotalText => $"{Total:F2} MDL";

        public string ReceiptDiscountText
        {
            get => _receiptDiscountText;
            set
            {
                if (_receiptDiscountText == value) return;
                _receiptDiscountText = value;
                OnPropertyChanged();
                RefreshTotals();
            }
        }

        public CashierWindow(AppDbContext ctx, SystemUser user)
        {
            InitializeComponent();
            _ctx = ctx;
            _user = user;
            DataContext = this;

           
            if (user.Employee?.PharmacyId != null)
            {
                SelectedPharmacy = _ctx.Pharmacies
                    .FirstOrDefault(p => p.Id == user.Employee.PharmacyId);
            }

            ReloadProducts();
            RefreshReceiptState();
        }

        private void ReloadProducts()
        {
            ProductCards.Clear();
            if (SelectedPharmacy == null) return;

            IQueryable<StockBalance> query = _ctx.StockBalances
                .AsNoTracking()
                .Include(s => s.Product)
                .ThenInclude(p => p!.Category)
                .Include(s => s.Product)
                .ThenInclude(p => p!.Manufacturer)
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
                ReceiptDiscountText = "0";

                ReloadReceipt();
            }
            catch (Exception ex)
            {
                ShowError(ex);
            }
        }

        private void AddProduct_Click(object sender, RoutedEventArgs e)
        {
            e.Handled = true;
            if ((sender as FrameworkElement)?.Tag is not CashierProductCard card) return;
            AddReceiptProduct(card.Product.Id, 1);
        }

        private void ProductCard_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            if ((sender as FrameworkElement)?.Tag is not CashierProductCard card) return;

            var details = new ProductDetailsWindow(card.Product, card.RemainingQty)
            {
                Owner = this
            };
            details.ShowDialog();
        }

        private void IncreaseLine_Click(object sender, RoutedEventArgs e)
        {
            if ((sender as FrameworkElement)?.Tag is not CashierReceiptLine line) return;
            AddReceiptProduct(line.ProductId, 1);
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
                            Discount = 0
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

        private void PrintTodayReceipts_Click(object sender, RoutedEventArgs e)
        {
            if (SelectedPharmacy == null || _user.Employee == null) return;

            var today = DateTime.Today;
            _ctx.ChangeTracker.Clear();
            var receipts = _ctx.Receipts
                .AsNoTracking()
                .Include(r => r.Pharmacy)
                .Include(r => r.Employee)
                .Include(r => r.OrderItems)
                    .ThenInclude(oi => oi.Product)
                .Where(r => r.PharmacyId == SelectedPharmacy.Id &&
                            r.EmployeeId == _user.Employee.Id &&
                            r.OrderItems.Any())
                .AsEnumerable()
                .Where(r => r.Date.Date == today)
                .OrderBy(r => r.ReceiptNumber)
                .ToList();

            if (receipts.Count == 0)
            {
                MessageBox.Show(
                    "За сегодня по выбранной аптеке у вас пока нет оформленных чеков.",
                    "Печать чеков",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);
                return;
            }

            var selectionWindow = new ReceiptPrintSelectionWindow(receipts)
            {
                Owner = this
            };
            if (selectionWindow.ShowDialog() != true || selectionWindow.SelectedReceipt == null)
                return;

            var printDialog = new PrintDialog();
            if (printDialog.ShowDialog() != true) return;

            var document = BuildReceiptDocument(selectionWindow.SelectedReceipt);
            document.PageHeight = printDialog.PrintableAreaHeight;
            document.PageWidth = Math.Min(printDialog.PrintableAreaWidth, 320);
            document.PagePadding = new Thickness(14);
            document.ColumnWidth = document.PageWidth - 28;

            printDialog.PrintDocument(
                ((IDocumentPaginatorSource)document).DocumentPaginator,
                $"Hippocrates чек {selectionWindow.SelectedReceipt.ReceiptNumber}");
        }

        private void ReceiptDiscount_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            e.Handled = !e.Text.All(char.IsDigit);
        }

        private static FlowDocument BuildReceiptDocument(Receipt receipt)
        {
            var document = new FlowDocument
            {
                FontFamily = new System.Windows.Media.FontFamily("Consolas"),
                FontSize = 12,
                TextAlignment = TextAlignment.Left
            };

            var lines = new List<string>
            {
                CenterLine("HIPPOCRATES", 34),
                CenterLine("КАССОВЫЙ ЧЕК", 34),
                SeparatorLine(34),
                $"Чек N {receipt.ReceiptNumber}",
                $"Дата {receipt.Date:dd.MM.yy} {receipt.Time:hh\\:mm}",
                $"Кассир {TrimToWidth(receipt.Employee?.FullName ?? "-", 27)}",
                SeparatorLine(34)
            };

            foreach (var item in receipt.OrderItems.OrderBy(x => x.Product?.Name))
            {
                var name = TrimToWidth(item.Product?.Name?.ToUpper() ?? $"ТОВАР #{item.ProductId}", 34);
                var qtyPrice = $"{item.UnitPrice:F2}*{item.Quantity}шт.";
                var total = $"={item.TotalPrice:F2}";

                lines.Add(name);
                lines.Add(TwoColumnLine(qtyPrice, total, 34));
                if (item.Discount > 0)
                    lines.Add($"СКИДКА {item.Discount:0}%");
            }

            var vat = receipt.TotalAmount * 10m / 110m;
            lines.Add(SeparatorLine(34));
            lines.Add(TwoColumnLine("ИТОГ", $"={receipt.TotalAmount:F2}", 34));
            lines.Add(TwoColumnLine("СУММА НДС 10%", $"={vat:F2}", 34));
            lines.Add(TwoColumnLine("ОПЛАТА", $"={receipt.TotalAmount:F2}", 34));
            lines.Add(SeparatorLine(34));
            lines.Add($"АПТЕКА: {TrimToWidth(receipt.Pharmacy?.Address ?? "-", 25)}");
            lines.Add("Место расчетов: Аптека");
            lines.Add($"ФД {receipt.Id:D8}");
            lines.Add($"ПРИХОД");
            lines.Add(SeparatorLine(34));
            lines.Add(CenterLine("Спасибо за покупку!", 34));

            document.Blocks.Add(new Paragraph(new Run(string.Join(Environment.NewLine, lines)))
            {
                Margin = new Thickness(0),
                LineHeight = 15
            });

            var qrImage = new System.Windows.Controls.Image
            {
                Source = CreateQrImage(BuildQrPayload(receipt)),
                Width = 128,
                Height = 128,
                Stretch = System.Windows.Media.Stretch.Uniform,
                HorizontalAlignment = HorizontalAlignment.Center,
                Margin = new Thickness(0, 12, 0, 0)
            };
            document.Blocks.Add(new BlockUIContainer(qrImage));

            return document;
        }

        private static string BuildQrPayload(Receipt receipt)
        {
            var items = string.Join("; ", receipt.OrderItems
                .OrderBy(x => x.Product?.Name)
                .Select(x => $"{x.Product?.Name ?? $"#{x.ProductId}"} x{x.Quantity} = {x.TotalPrice:F2}"));

            return string.Join(Environment.NewLine, new[]
            {
                "Hippocrates",
                $"Чек №{receipt.ReceiptNumber}",
                $"Дата: {receipt.Date:dd.MM.yyyy} {receipt.Time:hh\\:mm}",
                $"Аптека: {receipt.Pharmacy?.Address ?? "-"}",
                $"Кассир: {receipt.Employee?.FullName ?? "-"}",
                $"Сумма: {receipt.TotalAmount:F2} MDL",
                $"Товары: {items}"
            });
        }

        private static BitmapImage CreateQrImage(string payload)
        {
            using var generator = new QRCodeGenerator();
            using var data = generator.CreateQrCode(payload, QRCodeGenerator.ECCLevel.Q);
            var qrCode = new PngByteQRCode(data);
            var bytes = qrCode.GetGraphic(8);

            var image = new BitmapImage();
            using var stream = new MemoryStream(bytes);
            image.BeginInit();
            image.CacheOption = BitmapCacheOption.OnLoad;
            image.StreamSource = stream;
            image.EndInit();
            image.Freeze();
            return image;
        }

        private static string SeparatorLine(int width) => new('-', width);

        private static string CenterLine(string value, int width)
        {
            value = TrimToWidth(value, width);
            var left = Math.Max(0, (width - value.Length) / 2);
            return new string(' ', left) + value;
        }

        private static string TwoColumnLine(string left, string right, int width)
        {
            left = TrimToWidth(left, width);
            right = TrimToWidth(right, width);
            var spaces = Math.Max(1, width - left.Length - right.Length);
            return left + new string(' ', spaces) + right;
        }

        private static string TrimToWidth(string value, int width) =>
            value.Length <= width ? value : value[..width];

        private void ReceiptDiscount_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key != Key.Enter || sender is not TextBox textBox) return;

            NormalizeReceiptDiscount(textBox);
            Keyboard.ClearFocus();
        }

        private void ReceiptDiscount_LostFocus(object sender, RoutedEventArgs e)
        {
            if (sender is TextBox textBox)
                NormalizeReceiptDiscount(textBox);
        }

        private void CloseReceipt_Click(object sender, RoutedEventArgs e)
        {
            if (_currentReceipt == null || CurrentLines.Count == 0) return;

            try
            {
                DbProcedures.ApplyReceiptDiscount(_ctx, _currentReceipt.Id, ReceiptDiscount);

                MessageBox.Show(
                    $"Чек закрыт.\n\nФармацевт: {CashierName}\nСкидка: {ReceiptDiscount:0}%\nСумма: {TotalText}",
                    "Продажа завершена",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);

                _currentReceipt = null;
                CurrentLines.Clear();
                ReceiptDiscountText = "0";
                RefreshReceiptState();
            }
            catch (Exception ex)
            {
                ShowError(ex);
            }
        }

        private void AddReceiptProduct(int productId, int quantity)
        {
            if (_currentReceipt == null) return;

            try
            {
                DbProcedures.AddOrderItem(_ctx, new OrderItem
                {
                    ReceiptId = _currentReceipt.Id,
                    ProductId = productId,
                    Quantity = quantity,
                    Discount = 0
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

        private void NormalizeReceiptDiscount(TextBox textBox)
        {
            ReceiptDiscountText = ParseReceiptDiscount(textBox.Text).ToString("0");
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
                ReceiptDiscountText = "0";
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
            RefreshTotals();
        }

        private void RefreshTotals()
        {
            OnPropertyChanged(nameof(Subtotal));
            OnPropertyChanged(nameof(ReceiptDiscount));
            OnPropertyChanged(nameof(Total));
            OnPropertyChanged(nameof(SubtotalText));
            OnPropertyChanged(nameof(TotalText));
        }

        private decimal ParseReceiptDiscount() => ParseReceiptDiscount(ReceiptDiscountText);

        private static decimal ParseReceiptDiscount(string value)
        {
            if (!decimal.TryParse(value, out var discount) || discount < 0)
                return 0;

            return discount > 30 ? 30 : discount;
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
        public decimal Total { get; }
        public string Details => $"{Quantity} × {UnitPrice:F2} MDL";
        public string TotalText => $"{Total:F2}";

        public CashierReceiptLine(OrderItem item)
        {
            ProductId = item.ProductId;
            ProductName = item.Product?.Name ?? $"Товар #{item.ProductId}";
            Quantity = item.Quantity;
            UnitPrice = item.UnitPrice;
            Total = item.Quantity * item.UnitPrice;
        }
    }

    public sealed class ProductImageConverter : IValueConverter
    {
        public object? Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is not string path || string.IsNullOrWhiteSpace(path))
                return null;

            if (!Path.IsPathRooted(path))
                path = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, path);

            if (!File.Exists(path))
                return null;

            try
            {
                var image = new BitmapImage();
                image.BeginInit();
                image.CacheOption = BitmapCacheOption.OnLoad;
                image.UriSource = new Uri(path, UriKind.Absolute);
                image.EndInit();
                image.Freeze();
                return image;
            }
            catch
            {
                return null;
            }
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
