using System;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Xps;
using System.Windows.Xps.Packaging;
using Microsoft.Win32;
using MyHippocrates.Data;
using MyHippocrates.Reports;

namespace MyHippocrates.Views
{
    public partial class ReportsWindow : Window
    {
        private readonly AppDbContext _ctx;
        private FlowDocument? _currentDoc;

        public ReportsWindow(AppDbContext ctx)
        {
            InitializeComponent();
            _ctx = ctx;
            InitFilters();
        }

        // ── Инициализация фильтров ────────────────────────────────

        private void InitFilters()
        {
            // Период по умолчанию — последние 6 месяцев
            DpFrom.SelectedDate = DateTime.Today.AddMonths(-6);
            DpTo.SelectedDate = DateTime.Today;
            DpFromProd.SelectedDate = DateTime.Today.AddMonths(-6);
            DpToProd.SelectedDate = DateTime.Today;

            // Аптеки
            var pharmacies = _ctx.Pharmacies.OrderBy(p => p.Address).ToList();
            var allPharmacies = new[] { "Все аптеки" }
                .Concat(pharmacies.Select(p => p.Address))
                .ToList();

            CbPharmacySales.ItemsSource = allPharmacies;
            CbPharmacySales.SelectedIndex = 0;

            CbPharmacyStock.ItemsSource = allPharmacies;
            CbPharmacyStock.SelectedIndex = 0;

            // Категории
            var categories = new[] { "Все товары", "По рецепту", "Без рецепта" };
            CbCategorySales.ItemsSource = categories;
            CbCategorySales.SelectedIndex = 0;
            CbCategoryProd.ItemsSource = categories;
            CbCategoryProd.SelectedIndex = 0;
        }

        // ── Смена типа отчёта ────────────────────────────────────

        private void ReportType_Changed(object sender, RoutedEventArgs e)
        {
            // Панели могут быть null при первом вызове (до InitializeComponent)
            if (PanelSalesFilters == null) return;

            PanelSalesFilters.Visibility = RbSales.IsChecked == true ? Visibility.Visible : Visibility.Collapsed;
            PanelProductFilters.Visibility = RbProducts.IsChecked == true ? Visibility.Visible : Visibility.Collapsed;
            PanelStockFilters.Visibility = RbStock.IsChecked == true ? Visibility.Visible : Visibility.Collapsed;

            // Сбрасываем просмотр
            DocReader.Visibility = Visibility.Collapsed;
            PlaceholderPanel.Visibility = Visibility.Visible;
            _currentDoc = null;
        }

        // ── Генерация отчёта ─────────────────────────────────────

        private void BtnGenerate_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                FlowDocument doc;

                if (RbSales.IsChecked == true)
                {
                    doc = BuildSalesReport();
                }
                else if (RbProducts.IsChecked == true)
                {
                    doc = BuildProductsReport();
                }
                else
                {
                    doc = BuildStockReport();
                }

                _currentDoc = doc;
                DocReader.Document = doc;
                DocReader.Visibility = Visibility.Visible;
                PlaceholderPanel.Visibility = Visibility.Collapsed;
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.InnerException?.Message ?? ex.Message,
                    "Ошибка формирования отчёта",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private FlowDocument BuildSalesReport()
        {
            var dateFrom = DpFrom.SelectedDate
                ?? throw new InvalidOperationException("Укажите дату начала периода.");
            var dateTo = DpTo.SelectedDate
                ?? throw new InvalidOperationException("Укажите дату конца периода.");
            if (dateFrom > dateTo)
                throw new InvalidOperationException("Дата начала должна быть раньше даты окончания.");

            return ReportBuilder.BuildSalesReport(
                _ctx,
                dateFrom, dateTo,
                CbPharmacySales.SelectedItem?.ToString() ?? "Все аптеки",
                CbCategorySales.SelectedItem?.ToString() ?? "Все товары");
        }

        private FlowDocument BuildProductsReport()
        {
            var dateFrom = DpFromProd.SelectedDate
                ?? throw new InvalidOperationException("Укажите дату начала периода.");
            var dateTo = DpToProd.SelectedDate
                ?? throw new InvalidOperationException("Укажите дату конца периода.");
            if (dateFrom > dateTo)
                throw new InvalidOperationException("Дата начала должна быть раньше даты окончания.");

            if (!int.TryParse(TbTopN.Text, out int topN) || topN <= 0)
                topN = 10;

            return ReportBuilder.BuildProductsReport(
                _ctx,
                dateFrom, dateTo,
                CbCategoryProd.SelectedItem?.ToString() ?? "Все товары",
                topN);
        }

        private FlowDocument BuildStockReport()
        {
            if (!int.TryParse(TbLowThreshold.Text, out int threshold) || threshold < 0)
                threshold = 20;

            return ReportBuilder.BuildStockReport(
                _ctx,
                CbPharmacyStock.SelectedItem?.ToString() ?? "Все аптеки",
                threshold);
        }

        // ── Печать ───────────────────────────────────────────────

        private void BtnPrint_Click(object sender, RoutedEventArgs e)
        {
            if (_currentDoc == null)
            {
                MessageBox.Show("Сначала сформируйте отчёт.",
                    "Нет отчёта", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            var dlg = new PrintDialog();
            if (dlg.ShowDialog() != true) return;

            // Создаём копию документа для печати (чтобы не сбить настройки просмотра)
            var paginator = ((IDocumentPaginatorSource)_currentDoc).DocumentPaginator;
            paginator.PageSize = new System.Windows.Size(
                dlg.PrintableAreaWidth, dlg.PrintableAreaHeight);

            dlg.PrintDocument(paginator, "Hippocrates — Отчёт");
        }

        // ── Сохранить как XPS ────────────────────────────────────

        private void BtnSavePdf_Click(object sender, RoutedEventArgs e)
        {
            if (_currentDoc == null)
            {
                MessageBox.Show("Сначала сформируйте отчёт.",
                    "Нет отчёта", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            var dlg = new SaveFileDialog
            {
                Title = "Сохранить отчёт",
                Filter = "XPS документ (*.xps)|*.xps",
                DefaultExt = ".xps",
                FileName = $"Hippocrates_Report_{DateTime.Now:yyyyMMdd_HHmm}.xps"
            };

            if (dlg.ShowDialog() != true) return;

            try
            {
                // Удаляем старый файл если есть
                if (File.Exists(dlg.FileName))
                    File.Delete(dlg.FileName);

                using var xpsDoc = new XpsDocument(dlg.FileName, FileAccess.ReadWrite);
                var writer = XpsDocument.CreateXpsDocumentWriter(xpsDoc);
                var paginator = ((IDocumentPaginatorSource)_currentDoc).DocumentPaginator;
                paginator.PageSize = new System.Windows.Size(1100, 850);
                writer.Write(paginator);

                MessageBox.Show($"Отчёт сохранён:\n{dlg.FileName}",
                    "Готово", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Ошибка сохранения",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // ── Валидация ввода ──────────────────────────────────────

        private void OnlyDigits_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            e.Handled = !e.Text.All(char.IsDigit);
        }
    }
}