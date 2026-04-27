using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Windows;
using System.Windows.Data;
using Microsoft.EntityFrameworkCore;
using MyHippocrates.Commands;
using MyHippocrates.Data;
using MyHippocrates.Models;

namespace MyHippocrates.ViewModels
{
    internal class OrderItemsViewModel : BaseViewModel
    {
        private readonly AppDbContext _ctx;
        private readonly ObservableCollection<Receipt> _receipts;
        private readonly ObservableCollection<Product> _products;
        private readonly ObservableCollection<OrderItem> _items = new();

        // Кэш остатков — обновляется при каждом Reload()
        private List<StockBalance> _stockBalances = new();

        public ICollectionView View { get; }

        private string _search = "";
        public string SearchText
        {
            get => _search;
            set { SetProperty(ref _search, value); View.Refresh(); }
        }

        public RelayCommand AddCommand { get; }
        public RelayCommand EditCommand { get; }
        public RelayCommand DeleteCommand { get; }

        public OrderItemsViewModel(AppDbContext ctx,
            ObservableCollection<Receipt> receipts,
            ObservableCollection<Product> products)
        {
            _ctx = ctx;
            _receipts = receipts;
            _products = products;

            View = CollectionViewSource.GetDefaultView(_items);
            View.Filter = obj =>
            {
                if (string.IsNullOrWhiteSpace(_search)) return true;
                if (obj is not OrderItem o) return false;
                return (o.Product?.Name?.ToLower().Contains(_search.ToLower()) ?? false)
                    || o.Quantity.ToString().Contains(_search)
                    || o.TotalPrice.ToString().Contains(_search);
            };

            AddCommand = new RelayCommand(_ => Add());
            EditCommand = new RelayCommand(o => Edit(o as OrderItem), o => o is OrderItem);
            DeleteCommand = new RelayCommand(o => Delete(o as OrderItem), o => o is OrderItem);

            Load();
        }

        public void Reload() => Load();

        private void Load()
        {
            _ctx.ChangeTracker.Clear();

            _items.Clear();
            foreach (var o in _ctx.OrderItems
                .Include(x => x.Receipt)
                .Include(x => x.Product)
                .OrderBy(x => x.ReceiptId).ToList())
                _items.Add(o);

            //Обновляем кэш остатков
            _stockBalances = _ctx.StockBalances.ToList();
        }

        private void Add()
        {
            var entity = new OrderItem();
            var vm = new OrderItemEditorViewModel(entity, _receipts, _products, _stockBalances);
            var dlg = new Views.EditDialog(vm, _ctx, isNew: true)
            { Owner = Application.Current.MainWindow, Title = "Добавить позицию" };
            dlg.TxtTitle.Text = "Добавление записи";
            if (dlg.ShowDialog() == true)
            {
                entity.Receipt = _receipts.FirstOrDefault(r => r.Id == entity.ReceiptId);
                entity.Product = _products.FirstOrDefault(p => p.Id == entity.ProductId);

                Reload();

                //Обновляем сумму чека в коллекции
                RefreshReceiptTotal(entity.ReceiptId);

                //Обновляем кэш остатков
                _stockBalances = _ctx.StockBalances.ToList();

                View.Refresh();
            }
        }

        private void Edit(OrderItem? o)
        {
            if (o == null) return;

            var oldReceiptId = o.ReceiptId;
            var oldProductId = o.ProductId;

            var copy = new OrderItem
            {
                ReceiptId = o.ReceiptId,
                ProductId = o.ProductId,
                Quantity = o.Quantity,
                UnitPrice = o.UnitPrice,
                Discount = o.Discount,
                TotalPrice = o.TotalPrice
            };

            var vm = new OrderItemEditorViewModel(copy, _receipts, _products, _stockBalances);
            var dlg = new Views.EditDialog(vm, _ctx, isNew: false)
            { Owner = Application.Current.MainWindow, Title = "Редактировать позицию" };

            if (dlg.ShowDialog() == true)
            {
                copy.Receipt = _receipts.FirstOrDefault(r => r.Id == copy.ReceiptId);
                copy.Product = _products.FirstOrDefault(p => p.Id == copy.ProductId);

                Reload();

                //Обновляем суммы затронутых чеков
                RefreshReceiptTotal(oldReceiptId);
                if (copy.ReceiptId != oldReceiptId)
                    RefreshReceiptTotal(copy.ReceiptId);

                //Обновляем кэш остатков
                _stockBalances = _ctx.StockBalances.ToList();

                View.Refresh();
            }
        }

        private void Delete(OrderItem? o)
        {
            if (o == null) return;
            var name = o.Product?.Name ?? $"ProductId={o.ProductId}";
            var res = MessageBox.Show(
                $"Удалить позицию «{name}»?\n\nТовар будет возвращён на склад.",
                "Подтверждение", MessageBoxButton.YesNo, MessageBoxImage.Question);
            if (res != MessageBoxResult.Yes) return;
            try
            {
                var receiptId = o.ReceiptId;
                DbProcedures.DeleteOrderItem(_ctx, o.ReceiptId, o.ProductId);
                Reload();
                //Обновляем сумму чека
                RefreshReceiptTotal(receiptId);

                //Обновляем кэш остатков
                _stockBalances = _ctx.StockBalances.ToList();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.InnerException?.Message ?? ex.Message,
                    "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        private void RefreshReceiptTotal(int receiptId)
        {
            var fresh = _ctx.Receipts.FirstOrDefault(r => r.Id == receiptId);
            if (fresh == null) return;

            var existing = _receipts.FirstOrDefault(r => r.Id == receiptId);
            if (existing != null)
                existing.TotalAmount = fresh.TotalAmount;
        }
    }
}