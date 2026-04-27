using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows;
using Microsoft.EntityFrameworkCore;
using MyHippocrates.Data;
using MyHippocrates.Models;

namespace MyHippocrates.ViewModels
{
    // ── Chart data helpers ────────────────────────────────────────
    public class ChartPoint
    {
        public string Label { get; set; } = "";
        public double Value { get; set; }
        public string Color { get; set; } = "#2E7D32";
        public string Tooltip => $"{Label}: {Value:F2}";
    }

    public class PieSlice
    {
        public string Label { get; set; } = "";
        public double Value { get; set; }
        public double Percentage { get; set; }
        public string Color { get; set; } = "#2E7D32";
        public string Tooltip => $"{Label}: {Value:F2} ({Percentage:F1}%)";
    }

    // ── KPI card ──────────────────────────────────────────────────
    public class KpiCard
    {
        public string Title { get; set; } = "";
        public string Value { get; set; } = "";
        public string Icon { get; set; } = "";
        public string Color { get; set; } = "#2E7D32";
        public string SubValue { get; set; } = "";
    }

    // ── Grid row for top products ─────────────────────────────────
    public class ProductSalesRow
    {
        public string ProductName { get; set; } = "";
        public string ManufacturerName { get; set; } = "";
        public int TotalQty { get; set; }
        public decimal TotalRevenue { get; set; }
        public decimal AvgDiscount { get; set; }
        public decimal UnitPrice { get; set; }
        public string PrescriptionRequired { get; set; } = "";
    }

    public class PharmacyRevenueRow
    {
        public string PharmacyAddress { get; set; } = "";
        public decimal TotalRevenue { get; set; }
        public int ReceiptCount { get; set; }
        public decimal AvgReceipt { get; set; }
        public decimal MinReceipt { get; set; }
        public decimal MaxReceipt { get; set; }
    }

    public class LowStockRow
    {
        public string PharmacyAddress { get; set; } = "";
        public string ProductName { get; set; } = "";
        public int RemainingQty { get; set; }
    }

    // ── Main Dashboard ViewModel ──────────────────────────────────
    public class DashboardViewModel : INotifyPropertyChanged
    {
        private readonly AppDbContext _ctx;

        public event PropertyChangedEventHandler? PropertyChanged;
        private void OnPropertyChanged([CallerMemberName] string name = "") =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));

        // ── Filters ───────────────────────────────────────────────
        private DateTime _dateFrom = DateTime.UtcNow.AddMonths(-6);
        public DateTime DateFrom
        {
            get => _dateFrom;
            set { _dateFrom = value; OnPropertyChanged(); }
        }

        private DateTime _dateTo = DateTime.UtcNow;
        public DateTime DateTo
        {
            get => _dateTo;
            set { _dateTo = value; OnPropertyChanged(); }
        }

        private string _selectedPharmacy = "Все аптеки";
        public string SelectedPharmacy
        {
            get => _selectedPharmacy;
            set { _selectedPharmacy = value; OnPropertyChanged(); }
        }

        private string _selectedCategory = "Все товары";
        public string SelectedCategory
        {
            get => _selectedCategory;
            set { _selectedCategory = value; OnPropertyChanged(); }
        }

        private decimal _minPrice = 0;
        public decimal MinPrice
        {
            get => _minPrice;
            set { _minPrice = value; OnPropertyChanged(); }
        }

        private decimal _maxPrice = 999999;
        public decimal MaxPrice
        {
            get => _maxPrice;
            set { _maxPrice = value; OnPropertyChanged(); }
        }

        private int _topN = 10;
        public int TopN
        {
            get => _topN;
            set { _topN = value; OnPropertyChanged(); }
        }

        // Filter source collections
        public ObservableCollection<string> PharmacyOptions { get; } = new();
        public ObservableCollection<string> CategoryOptions { get; } = new() { "Все товары", "По рецепту", "Без рецепта" };
        public List<int> TopNOptions { get; } = new() { 5, 10, 15, 20 };

        // ── KPI cards ─────────────────────────────────────────────
        private ObservableCollection<KpiCard> _kpiCards = new();
        public ObservableCollection<KpiCard> KpiCards
        {
            get => _kpiCards;
            set { _kpiCards = value; OnPropertyChanged(); }
        }

        // ── Chart data ────────────────────────────────────────────
        private ObservableCollection<ChartPoint> _revenueByMonth = new();
        public ObservableCollection<ChartPoint> RevenueByMonth
        {
            get => _revenueByMonth;
            set { _revenueByMonth = value; OnPropertyChanged(); }
        }

        private ObservableCollection<ChartPoint> _revenueByPharmacy = new();
        public ObservableCollection<ChartPoint> RevenueByPharmacy
        {
            get => _revenueByPharmacy;
            set { _revenueByPharmacy = value; OnPropertyChanged(); }
        }

        private ObservableCollection<PieSlice> _salesByCategory = new();
        public ObservableCollection<PieSlice> SalesByCategory
        {
            get => _salesByCategory;
            set { _salesByCategory = value; OnPropertyChanged(); }
        }

        private ObservableCollection<ChartPoint> _topProductsChart = new();
        public ObservableCollection<ChartPoint> TopProductsChart
        {
            get => _topProductsChart;
            set { _topProductsChart = value; OnPropertyChanged(); }
        }

        // ── Grid data ─────────────────────────────────────────────
        private ObservableCollection<ProductSalesRow> _topProducts = new();
        public ObservableCollection<ProductSalesRow> TopProducts
        {
            get => _topProducts;
            set { _topProducts = value; OnPropertyChanged(); }
        }

        private ObservableCollection<PharmacyRevenueRow> _pharmacyRevenue = new();
        public ObservableCollection<PharmacyRevenueRow> PharmacyRevenue
        {
            get => _pharmacyRevenue;
            set { _pharmacyRevenue = value; OnPropertyChanged(); }
        }

        private ObservableCollection<LowStockRow> _lowStock = new();
        public ObservableCollection<LowStockRow> LowStock
        {
            get => _lowStock;
            set { _lowStock = value; OnPropertyChanged(); }
        }

        // ── Status ────────────────────────────────────────────────
        private string _statusText = "Готово";
        public string StatusText
        {
            get => _statusText;
            set { _statusText = value; OnPropertyChanged(); }
        }

        private string _lastUpdated = "";
        public string LastUpdated
        {
            get => _lastUpdated;
            set { _lastUpdated = value; OnPropertyChanged(); }
        }

        // ── Commands ──────────────────────────────────────────────
        public Commands.RelayCommand ApplyFiltersCommand { get; }
        public Commands.RelayCommand ResetFiltersCommand { get; }
        public Commands.RelayCommand ExportCommand { get; }

        public DashboardViewModel(AppDbContext ctx)
        {
            _ctx = ctx;
            ApplyFiltersCommand = new Commands.RelayCommand(_ => Refresh());
            ResetFiltersCommand = new Commands.RelayCommand(_ => ResetFilters());
            ExportCommand = new Commands.RelayCommand(_ => Export());

            LoadPharmacyOptions();
            Refresh();
        }

        private void LoadPharmacyOptions()
        {
            PharmacyOptions.Clear();
            PharmacyOptions.Add("Все аптеки");
            foreach (var ph in _ctx.Pharmacies.OrderBy(p => p.Address).ToList())
                PharmacyOptions.Add(ph.Address);
        }

        private void ResetFilters()
        {
            DateFrom = DateTime.Today.AddMonths(-6);
            DateTo = DateTime.Today;
            SelectedPharmacy = "Все аптеки";
            SelectedCategory = "Все товары";
            MinPrice = 0;
            MaxPrice = 999999;
            TopN = 10;
            Refresh();
        }

        public void Refresh()
        {
            try
            {
                StatusText = "Загрузка данных...";
                _ctx.ChangeTracker.Clear();

                LoadKpiCards();
                LoadRevenueByMonth();
                LoadRevenueByPharmacy();
                LoadSalesByCategory();
                LoadTopProducts();
                LoadPharmacyRevenue();
                LoadLowStock();

                LastUpdated = $"Обновлено: {DateTime.UtcNow:dd.MM.yyyy HH:mm:ss}";
                StatusText = "Данные загружены успешно";
            }
            catch (Exception ex)
            {
                StatusText = "Ошибка загрузки: " + ex.Message;
                MessageBox.Show(ex.Message, "Ошибка Dashboard", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // ── Filtered base queries ─────────────────────────────────
        private IQueryable<Receipt> FilteredReceipts()
        {
            // Конвертируем DateTime в UTC
            var dateFromUtc = DateTime.SpecifyKind(DateFrom.Date, DateTimeKind.Utc);
            var dateToUtc = DateTime.SpecifyKind(DateTo.Date.AddDays(1), DateTimeKind.Utc); // +1 день чтобы включить последний день

            var q = _ctx.Receipts
                .Include(r => r.Pharmacy)
                .Include(r => r.Employee)
                .Where(r => r.Date >= dateFromUtc && r.Date < dateToUtc
                         && r.TotalAmount >= MinPrice && r.TotalAmount <= MaxPrice);

            if (SelectedPharmacy != "Все аптеки")
                q = q.Where(r => r.Pharmacy!.Address == SelectedPharmacy);

            return q;
        }

        private IQueryable<OrderItem> FilteredOrderItems()
        {
            var receiptIds = FilteredReceipts().Select(r => r.Id);
            var q = _ctx.OrderItems
                .Include(o => o.Product).ThenInclude(p => p!.Manufacturer)
                .Include(o => o.Receipt).ThenInclude(r => r!.Pharmacy)
                .Where(o => receiptIds.Contains(o.ReceiptId));

            if (SelectedCategory == "По рецепту")
                q = q.Where(o => o.Product!.PrescriptionRequired);
            else if (SelectedCategory == "Без рецепта")
                q = q.Where(o => !o.Product!.PrescriptionRequired);

            return q;
        }

        // ── KPI Cards ─────────────────────────────────────────────
        private void LoadKpiCards()
        {
            var receipts = FilteredReceipts().ToList();
            var items = FilteredOrderItems().ToList();

            var totalRevenue = receipts.Sum(r => r.TotalAmount);
            var receiptCount = receipts.Count;
            var avgReceipt = receiptCount > 0 ? receipts.Average(r => r.TotalAmount) : 0;
            var maxReceipt = receiptCount > 0 ? receipts.Max(r => r.TotalAmount) : 0;
            var minReceipt = receiptCount > 0 ? receipts.Min(r => r.TotalAmount) : 0;
            var totalQty = items.Sum(o => o.Quantity);
            var avgDiscount = items.Any() ? items.Average(o => o.Discount) : 0;
            var uniqueProducts = items.Select(o => o.ProductId).Distinct().Count();

            var cards = new ObservableCollection<KpiCard>
            {
                new() { Title="Общая выручка",     Value=$"{totalRevenue:F2} MDL",   Icon="💰", Color="#2E7D32", SubValue=$"Чеков: {receiptCount}" },
                new() { Title="Средний чек",        Value=$"{avgReceipt:F2} MDL",    Icon="📊", Color="#1565C0", SubValue=$"MAX: {maxReceipt:F2}" },
                new() { Title="Мин. / Макс. чек",  Value=$"{minReceipt:F2} / {maxReceipt:F2}", Icon="📉", Color="#6A1B9A", SubValue="MDL" },
                new() { Title="Продано единиц",     Value=$"{totalQty:N0} шт.",       Icon="📦", Color="#E65100", SubValue=$"Товаров: {uniqueProducts}" },
                new() { Title="Средняя скидка",     Value=$"{avgDiscount:F2}%",        Icon="🏷️", Color="#00838F", SubValue="по позициям" },
                new() { Title="Кол-во чеков",       Value=$"{receiptCount:N0}",        Icon="🧾", Color="#558B2F", SubValue=$"за период" },
            };

            KpiCards = cards;
        }

        // ── Revenue by month ──────────────────────────────────────
        private void LoadRevenueByMonth()
        {
            var colors = new[] { "#2E7D32", "#43A047", "#66BB6A", "#81C784", "#A5D6A7", "#1B5E20", "#388E3C", "#4CAF50" };
            var data = FilteredReceipts()
                .GroupBy(r => new { r.Date.Year, r.Date.Month })
                .Select(g => new { Year = g.Key.Year, Month = g.Key.Month, Total = g.Sum(r => r.TotalAmount) })
                .OrderBy(x => x.Year).ThenBy(x => x.Month)
                .ToList();

            var points = data.Select((x, i) => new ChartPoint
            {
                Label = $"{x.Month:D2}/{x.Year}",
                Value = (double)x.Total,
                Color = colors[i % colors.Length]
            }).ToList();

            RevenueByMonth = new ObservableCollection<ChartPoint>(points);
        }

        // ── Revenue by pharmacy ───────────────────────────────────
        private void LoadRevenueByPharmacy()
        {
            var colors = new[] { "#1B5E20","#2E7D32","#388E3C","#43A047","#4CAF50","#66BB6A","#81C784",
                                 "#1565C0","#1976D2","#1E88E5","#42A5F5","#90CAF9",
                                 "#6A1B9A","#7B1FA2","#8E24AA","#AB47BC","#CE93D8" };
            var receipts = FilteredReceipts().ToList();
            var data = receipts
                .GroupBy(r => r.Pharmacy?.Address ?? "Неизвестно")
                .Select(g => new { Address = g.Key, Total = g.Sum(r => r.TotalAmount) })
                .OrderByDescending(x => x.Total)
                .Take(TopN)
                .ToList();

            var points = data.Select((x, i) => new ChartPoint
            {
                Label = x.Address.Length > 25 ? x.Address[..25] + "…" : x.Address,
                Value = (double)x.Total,
                Color = colors[i % colors.Length]
            }).ToList();

            RevenueByPharmacy = new ObservableCollection<ChartPoint>(points);
        }

        // ── Sales by prescription category ────────────────────────
        private void LoadSalesByCategory()
        {
            var items = FilteredOrderItems().ToList();
            var groups = items.GroupBy(o => o.Product?.PrescriptionRequired ?? false)
                              .Select(g => new { IsPrescription = g.Key, Revenue = g.Sum(o => o.TotalPrice) })
                              .ToList();

            var total = groups.Sum(g => g.Revenue);
            var slices = groups.Select(g => new PieSlice
            {
                Label = g.IsPrescription ? "По рецепту" : "Без рецепта",
                Value = (double)g.Revenue,
                Percentage = total > 0 ? (double)(g.Revenue / total * 100) : 0,
                Color = g.IsPrescription ? "#C62828" : "#2E7D32"
            }).ToList();

            SalesByCategory = new ObservableCollection<PieSlice>(slices);
        }

        // ── Top products chart ────────────────────────────────────
        private void LoadTopProducts()
        {
            var colors = new[] { "#2E7D32","#1565C0","#E65100","#6A1B9A","#00838F",
                                 "#558B2F","#AD1457","#00695C","#F57F17","#37474F" };
            var items = FilteredOrderItems().ToList();
            var data = items
                .GroupBy(o => new { o.ProductId, Name = o.Product?.Name ?? "?" })
                .Select(g => new
                {
                    g.Key.Name,
                    g.Key.ProductId,
                    TotalQty = g.Sum(o => o.Quantity),
                    TotalRevenue = g.Sum(o => o.TotalPrice),
                    AvgDiscount = g.Any() ? g.Average(o => o.Discount) : 0,
                    UnitPrice = g.FirstOrDefault()?.UnitPrice ?? 0,
                    Prescription = g.FirstOrDefault()?.Product?.PrescriptionRequired ?? false,
                    Manufacturer = g.FirstOrDefault()?.Product?.Manufacturer?.Name ?? "—"
                })
                .OrderByDescending(x => x.TotalRevenue)
                .Take(TopN)
                .ToList();

            TopProductsChart = new ObservableCollection<ChartPoint>(
                data.Select((x, i) => new ChartPoint
                {
                    Label = x.Name.Length > 18 ? x.Name[..18] + "…" : x.Name,
                    Value = (double)x.TotalRevenue,
                    Color = colors[i % colors.Length]
                }));

            TopProducts = new ObservableCollection<ProductSalesRow>(
                data.Select(x => new ProductSalesRow
                {
                    ProductName = x.Name,
                    ManufacturerName = x.Manufacturer,
                    TotalQty = x.TotalQty,
                    TotalRevenue = x.TotalRevenue,
                    AvgDiscount = x.AvgDiscount,
                    UnitPrice = x.UnitPrice,
                    PrescriptionRequired = x.Prescription ? "Да" : "Нет"
                }));
        }

        // ── Pharmacy revenue grid ─────────────────────────────────
        private void LoadPharmacyRevenue()
        {
            var receipts = FilteredReceipts().ToList();
            var data = receipts
                .GroupBy(r => r.Pharmacy?.Address ?? "?")
                .Select(g => new PharmacyRevenueRow
                {
                    PharmacyAddress = g.Key,
                    TotalRevenue = g.Sum(r => r.TotalAmount),
                    ReceiptCount = g.Count(),
                    AvgReceipt = g.Count() > 0 ? g.Average(r => r.TotalAmount) : 0,
                    MinReceipt = g.Count() > 0 ? g.Min(r => r.TotalAmount) : 0,
                    MaxReceipt = g.Count() > 0 ? g.Max(r => r.TotalAmount) : 0,
                })
                .OrderByDescending(x => x.TotalRevenue)
                .ToList();

            PharmacyRevenue = new ObservableCollection<PharmacyRevenueRow>(data);
        }

        // ── Low stock alert ───────────────────────────────────────
        private void LoadLowStock()
        {
            var data = _ctx.StockBalances
                .Include(s => s.Pharmacy)
                .Include(s => s.Product)
                .Where(s => s.RemainingQty < 20)
                .OrderBy(s => s.RemainingQty)
                .ToList()
                .Select(s => new LowStockRow
                {
                    PharmacyAddress = s.Pharmacy?.Address ?? "?",
                    ProductName = s.Product?.Name ?? "?",
                    RemainingQty = s.RemainingQty
                }).ToList();

            LowStock = new ObservableCollection<LowStockRow>(data);
        }

        // ── Export ────────────────────────────────────────────────
        private void Export()
        {
            var lines = new List<string>();
            lines.Add("=== DASHBOARD EXPORT ===");
            lines.Add($"Период: {DateFrom:dd.MM.yyyy} — {DateTo:dd.MM.yyyy}");
            lines.Add($"Аптека: {SelectedPharmacy} | Категория: {SelectedCategory}");
            lines.Add("");
            lines.Add("--- KPI ---");
            foreach (var k in KpiCards)
                lines.Add($"{k.Title}: {k.Value} ({k.SubValue})");
            lines.Add("");
            lines.Add("--- ТОП ТОВАРЫ ---");
            lines.Add("Товар;Производитель;Кол-во;Выручка;Скидка%;Цена;Рецепт");
            foreach (var r in TopProducts)
                lines.Add($"{r.ProductName};{r.ManufacturerName};{r.TotalQty};{r.TotalRevenue:F2};{r.AvgDiscount:F2};{r.UnitPrice:F2};{r.PrescriptionRequired}");
            lines.Add("");
            lines.Add("--- ВЫРУЧКА ПО АПТЕКАМ ---");
            lines.Add("Аптека;Выручка;Чеков;Средний;Мин;Макс");
            foreach (var r in PharmacyRevenue)
                lines.Add($"{r.PharmacyAddress};{r.TotalRevenue:F2};{r.ReceiptCount};{r.AvgReceipt:F2};{r.MinReceipt:F2};{r.MaxReceipt:F2}");

            var path = System.IO.Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.Desktop),
                $"Hippocrates_Dashboard_{DateTime.UtcNow:yyyyMMdd_HHmm}.csv");
            System.IO.File.WriteAllLines(path, lines, System.Text.Encoding.UTF8);
            MessageBox.Show($"Экспорт сохранён:\n{path}", "Экспорт", MessageBoxButton.OK, MessageBoxImage.Information);
        }
    }
}