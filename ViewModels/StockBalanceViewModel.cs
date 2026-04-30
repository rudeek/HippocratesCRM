using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media.Imaging;
using Microsoft.EntityFrameworkCore;
using MyHippocrates.Commands;
using MyHippocrates.Data;
using MyHippocrates.Models;

namespace MyHippocrates.ViewModels
{
    internal class StockBalanceViewModel : BaseViewModel
    {
        private readonly AppDbContext _ctx;
        private readonly ObservableCollection<Pharmacy> _pharmacies;
        private readonly ObservableCollection<Product> _products;
        private readonly ObservableCollection<StockBalance> _items = new();

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

        public StockBalanceViewModel(AppDbContext ctx,
            ObservableCollection<Pharmacy> pharmacies,
            ObservableCollection<Product> products)
        {
            _ctx = ctx;
            _pharmacies = pharmacies;
            _products = products;

            View = CollectionViewSource.GetDefaultView(_items);
            View.Filter = obj =>
            {
                if (string.IsNullOrWhiteSpace(_search)) return true;
                if (obj is not StockBalance s) return false;
                return (s.Pharmacy?.Address?.ToLower().Contains(_search.ToLower()) ?? false)
                    || (s.Product?.Name?.ToLower().Contains(_search.ToLower()) ?? false)
                    || s.RemainingQty.ToString().Contains(_search);
            };

            AddCommand = new RelayCommand(_ => Add());
            EditCommand = new RelayCommand(s => Edit(s as StockBalance), s => s is StockBalance);
            DeleteCommand = new RelayCommand(s => Delete(s as StockBalance), s => s is StockBalance);

            Load();
        }

        public void Reload() => Load();

        private void Load()
        {
            _ctx.ChangeTracker.Clear();
            _items.Clear();
            foreach (var s in _ctx.StockBalances
                .Include(x => x.Pharmacy)
                .Include(x => x.Product)
                .OrderBy(x => x.PharmacyId).ThenBy(x => x.ProductId).ToList())
                _items.Add(s);
        }

        private void Add()
        {
            var entity = new StockBalance();
            var vm = new StockBalanceEditorViewModel(entity, _pharmacies, _products);
            var dlg = new Views.EditDialog(vm, _ctx, isNew: true)
            { Owner = Application.Current.MainWindow, Title = "Добавить остаток", Icon = new BitmapImage(new Uri("pack://application:,,,/add.ico")) };
            dlg.TxtTitle.Text = "Добавление записи";
            if (dlg.ShowDialog() == true)
            {
                entity.Pharmacy = _pharmacies.FirstOrDefault(p => p.Id == entity.PharmacyId);
                entity.Product = _products.FirstOrDefault(p => p.Id == entity.ProductId);
                _items.Add(entity);
                View.Refresh();
            }
        }

        private void Edit(StockBalance? s)
        {
            if (s == null) return;

            var oldPharmacyId = s.PharmacyId;
            var oldProductId = s.ProductId;

            var copy = new StockBalance
            {
                PharmacyId = s.PharmacyId,
                ProductId = s.ProductId,
                RemainingQty = s.RemainingQty
            };

            var vm = new StockBalanceEditorViewModel(copy, _pharmacies, _products);
            var dlg = new Views.EditDialog(vm, _ctx, isNew: false)
            { Owner = Application.Current.MainWindow, Title = "Редактировать остаток", Icon = new BitmapImage(new Uri("pack://application:,,,/edit.ico")) };

            if (dlg.ShowDialog() == true)
            {
                s.PharmacyId = copy.PharmacyId;
                s.ProductId = copy.ProductId;
                s.RemainingQty = copy.RemainingQty;
                s.Pharmacy = _pharmacies.FirstOrDefault(p => p.Id == copy.PharmacyId);
                s.Product = _products.FirstOrDefault(p => p.Id == copy.ProductId);

                var idx = _items.IndexOf(s);
                if (idx >= 0)
                {
                    _items.RemoveAt(idx);
                    _items.Insert(idx, s);
                }
                View.Refresh();
            }
        }

        private void Delete(StockBalance? s)
        {
            if (s == null) return;
            var name = s.Product?.Name ?? $"ProductId={s.ProductId}";
            var res = MessageBox.Show(
                $"Удалить остаток «{name}»?",
                "Подтверждение", MessageBoxButton.YesNo, MessageBoxImage.Question);
            if (res != MessageBoxResult.Yes) return;
            try
            {
                DbProcedures.DeleteStockBalance(_ctx, s.PharmacyId, s.ProductId);
                _items.Remove(s);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.InnerException?.Message ?? ex.Message,
                    "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}