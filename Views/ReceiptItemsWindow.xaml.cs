using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Imaging;
using Microsoft.EntityFrameworkCore;
using MyHippocrates.Data;
using MyHippocrates.Models;
using MyHippocrates.ViewModels;

namespace MyHippocrates.Views
{
    public partial class ReceiptItemsWindow : Window
    {
        private readonly AppDbContext _ctx;
        private readonly Receipt _receipt;

        // Эти коллекции приходят извне (из MainViewModel) — чтобы ComboBox'ы в редакторе работали
        private readonly ObservableCollection<Receipt> _allReceipts;
        private readonly ObservableCollection<Product> _allProducts;
        private readonly ObservableCollection<Pharmacy> _allPharmacies;

        // Флаг: был ли чек удалён, потому что стал пустым
        public bool ReceiptWasDeleted { get; private set; } = false;

        // Флаг: изменялись ли данные (нужна перезагрузка снаружи)
        public bool DataChanged { get; private set; } = false;

        public ReceiptItemsWindow(
            AppDbContext ctx,
            Receipt receipt,
            ObservableCollection<Receipt> allReceipts,
            ObservableCollection<Product> allProducts,
            ObservableCollection<Pharmacy> allPharmacies)
        {
            InitializeComponent();
            _ctx = ctx;
            _receipt = receipt;
            _allReceipts = allReceipts;
            _allProducts = allProducts;
            _allPharmacies = allPharmacies;

            // Шапка
            TxtReceiptTitle.Text = $"Позиции чека № {receipt.ReceiptNumber}";
            TxtReceiptSubtitle.Text =
                $"Аптека: {receipt.Pharmacy?.Address ?? "—"}   |   " +
                $"Сотрудник: {receipt.Employee?.FullName ?? "—"}   |   " +
                $"Дата: {receipt.Date:dd.MM.yyyy}";

            LoadItems();
        }

        // ── Загрузка позиций ──────────────────────────────────────────────

        private void LoadItems()
        {
            _ctx.ChangeTracker.Clear();

            var items = _ctx.OrderItems
                .Include(o => o.Product)
                .Where(o => o.ReceiptId == _receipt.Id)
                .OrderBy(o => o.Product!.Name)
                .ToList();

            ItemsGrid.ItemsSource = null;
            ItemsGrid.ItemsSource = items;

            RefreshTotal();
        }

        private void RefreshTotal()
        {
            _ctx.ChangeTracker.Clear();
            var total = _ctx.OrderItems
                .Where(o => o.ReceiptId == _receipt.Id)
                .Sum(o => (decimal?)o.TotalPrice) ?? 0m;

            TxtTotal.Text = $"{total:F2} MDL";
        }

        // ── Нумерация строк ───────────────────────────────────────────────

        private void DataGrid_LoadingRow(object sender, DataGridRowEventArgs e)
        {
            e.Row.Header = (e.Row.GetIndex() + 1).ToString();
        }

        private void DataGrid_UnloadingRow(object sender, DataGridRowEventArgs e)
        {
            if (sender is not DataGrid dg) return;
            foreach (var item in dg.Items)
            {
                var row = dg.ItemContainerGenerator.ContainerFromItem(item) as DataGridRow;
                if (row != null)
                    row.Header = (row.GetIndex() + 1).ToString();
            }
        }

        // ── Выбор строки → активируем кнопки ─────────────────────────────

        private void ItemsGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            bool hasSelection = ItemsGrid.SelectedItem is OrderItem;
            BtnEdit.IsEnabled = hasSelection;
            BtnDelete.IsEnabled = hasSelection;
        }

        // ── Добавить позицию ──────────────────────────────────────────────

        private void Add_Click(object sender, RoutedEventArgs e)
        {
            // Кэш остатков для данной аптеки
            var stockBalances = _ctx.StockBalances
                .Where(s => s.PharmacyId == _receipt.PharmacyId)
                .ToList();

            var entity = new OrderItem { ReceiptId = _receipt.Id };

            // Для фильтра OrderItemEditorViewModel нужен список чеков
            // У нас один чек — но передаём коллекцию как есть
            var vm = new OrderItemEditorViewModel(
                entity,
                _allReceipts,
                _allProducts,
                stockBalances,
                _allPharmacies);

            var dlg = new EditDialog(vm, _ctx, isNew: true)
            {
                Owner = this,
                Title = "Добавить позицию",
                Icon = new BitmapImage(new Uri("pack://application:,,,/add.ico"))
            };
            dlg.TxtTitle.Text = "Добавление позиции";

            if (dlg.ShowDialog() == true)
            {
                DataChanged = true;
                LoadItems();
                UpdateReceiptTotalInCollection();
            }
        }

        // ── Изменить позицию ──────────────────────────────────────────────

        private void Edit_Click(object sender, RoutedEventArgs e)
        {
            if (ItemsGrid.SelectedItem is not OrderItem selected) return;

            var oldReceiptId = selected.ReceiptId;
            var oldProductId = selected.ProductId;

            var stockBalances = _ctx.StockBalances
                .Where(s => s.PharmacyId == _receipt.PharmacyId)
                .ToList();

            var copy = new OrderItem
            {
                ReceiptId = selected.ReceiptId,
                ProductId = selected.ProductId,
                Quantity = selected.Quantity,
                UnitPrice = selected.UnitPrice,
                Discount = selected.Discount,
                TotalPrice = selected.TotalPrice
            };

            var vm = new OrderItemEditorViewModel(
                copy,
                _allReceipts,
                _allProducts,
                stockBalances,
                _allPharmacies);

            var dlg = new EditDialog(vm, _ctx, isNew: false)
            {
                Owner = this,
                Title = "Изменить позицию",
                Icon = new BitmapImage(new Uri("pack://application:,,,/edit.ico"))
            };

            if (dlg.ShowDialog() == true)
            {
                DataChanged = true;
                LoadItems();
                UpdateReceiptTotalInCollection();
            }
        }

        // ── Удалить позицию ───────────────────────────────────────────────

        private void Delete_Click(object sender, RoutedEventArgs e)
        {
            if (ItemsGrid.SelectedItem is not OrderItem selected) return;

            var name = selected.Product?.Name ?? $"ProductId={selected.ProductId}";
            var res = MessageBox.Show(
                $"Удалить позицию «{name}»?\n\nТовар будет возвращён на склад.",
                "Подтверждение", MessageBoxButton.YesNo, MessageBoxImage.Question);

            if (res != MessageBoxResult.Yes) return;

            try
            {
                DbProcedures.DeleteOrderItem(_ctx, selected.ReceiptId, selected.ProductId);
                DataChanged = true;

                // Проверяем, остались ли ещё позиции
                _ctx.ChangeTracker.Clear();
                var remaining = _ctx.OrderItems.Count(o => o.ReceiptId == _receipt.Id);

                if (remaining == 0)
                {
                    // Чек стал пустым — удаляем его
                    var confirmDelete = MessageBox.Show(
                        "Все позиции удалены. Пустой чек будет автоматически удалён.",
                        "Удаление чека", MessageBoxButton.OK, MessageBoxImage.Information);

                    DbProcedures.DeleteReceipt(_ctx, _receipt.Id);
                    ReceiptWasDeleted = true;
                    ToastService.ShowSuccess($"Чек № {_receipt.ReceiptNumber} удалён (нет позиций).");
                    Close();
                    return;
                }

                UpdateReceiptTotalInCollection();
                LoadItems();
                ToastService.ShowSuccess("Позиция удалена.");
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.InnerException?.Message ?? ex.Message,
                    "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // ── Синхронизируем сумму в ObservableCollection чеков ────────────

        private void UpdateReceiptTotalInCollection()
        {
            _ctx.ChangeTracker.Clear();
            var fresh = _ctx.Receipts.FirstOrDefault(r => r.Id == _receipt.Id);
            if (fresh == null) return;

            var inCollection = _allReceipts.FirstOrDefault(r => r.Id == _receipt.Id);
            if (inCollection != null)
                inCollection.TotalAmount = fresh.TotalAmount;

            // Обновляем значение TotalAmount у самого объекта чека
            _receipt.TotalAmount = fresh.TotalAmount;
        }

        // ── Закрыть ───────────────────────────────────────────────────────

        private void Close_Click(object sender, RoutedEventArgs e) => Close();
    }
}