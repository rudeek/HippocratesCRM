using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Windows;
using System.Windows.Data;
using Microsoft.EntityFrameworkCore;
using MyHippocrates.Commands;
using MyHippocrates.Data;
using MyHippocrates.Models;
using MyHippocrates.Views;

namespace MyHippocrates.ViewModels
{
    internal class ProductsViewModel : BaseViewModel
    {
        private readonly AppDbContext _ctx;
        private readonly ObservableCollection<Manufacturer> _manufacturers;
        private readonly ObservableCollection<Product> _products;
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

        public ProductsViewModel(AppDbContext ctx,
            ObservableCollection<Manufacturer> manufacturers,
            ObservableCollection<Product> products)
        {
            _ctx = ctx;
            _manufacturers = manufacturers;
            _products = products;

            View = CollectionViewSource.GetDefaultView(_products);
            View.Filter = obj =>
            {
                if (string.IsNullOrWhiteSpace(_search)) return true;
                if (obj is not Product p) return false;
                return p.Name.ToLower().Contains(_search.ToLower())
                    || (p.Manufacturer?.Name?.ToLower().Contains(_search.ToLower()) ?? false);
            };

            AddCommand = new RelayCommand(_ => Add());
            EditCommand = new RelayCommand(p => Edit(p as Product), p => p is Product);
            DeleteCommand = new RelayCommand(p => Delete(p as Product), p => p is Product);

            Load();
        }

        private void Load()
        {
            _products.Clear();
            foreach (var p in _ctx.Products.Include(x => x.Manufacturer).OrderBy(x => x.Id).ToList())
                _products.Add(p);
        }

        private void Add()
        {
            var entity = new Product();
            var vm = new ProductEditorViewModel(entity, _manufacturers);
            var dlg = new EditDialog(vm, _ctx, isNew: true)
            { Owner = Application.Current.MainWindow, Title = "Добавить товар" };
            if (dlg.ShowDialog() == true)
            {
                entity.Manufacturer = _manufacturers.FirstOrDefault(m => m.Id == entity.ManufacturerId);
                _products.Add(entity);
                View.Refresh();
            }
        }

        private void Edit(Product? p)
        {
            if (p == null) return;
            var copy = new Product
            {
                Id = p.Id,
                Name = p.Name,
                ManufacturerId = p.ManufacturerId,
                ExpirationDate = p.ExpirationDate,
                ProductionDate = p.ProductionDate,
                Unit = p.Unit,
                Description = p.Description,
                PrescriptionRequired = p.PrescriptionRequired,
                PurchasePrice = p.PurchasePrice,
                SalePrice = p.SalePrice
            };
            var vm = new ProductEditorViewModel(copy, _manufacturers);
            var dlg = new EditDialog(vm, _ctx, isNew: false)
            { Owner = Application.Current.MainWindow, Title = "Редактировать товар" };
            if (dlg.ShowDialog() == true)
            {
                copy.Manufacturer = _manufacturers.FirstOrDefault(m => m.Id == copy.ManufacturerId);
                var idx = _products.IndexOf(p);
                if (idx >= 0) _products[idx] = copy;
                View.Refresh();
            }
        }

        private void Delete(Product? p)
        {
            if (p == null) return;
            var res = MessageBox.Show(
                $"Удалить товар «{p.Name}»?\n\nВнимание: все позиции заказов и остатки склада для этого товара будут удалены.",
                "Подтверждение", MessageBoxButton.YesNo, MessageBoxImage.Warning);
            if (res != MessageBoxResult.Yes) return;
            try
            {
                DbProcedures.DeleteProduct(_ctx, p.Id);
                _products.Remove(p);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.InnerException?.Message ?? ex.Message,
                    "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        public void Reload()
        {
            _products.Clear();
            foreach (var p in _ctx.Products.Include(x => x.Manufacturer).OrderBy(x => x.Id).ToList())
                _products.Add(p);
            View.Refresh();
        }
    }
}