using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Windows;
using System.Windows.Data;
using MyHippocrates.Commands;
using MyHippocrates.Data;
using MyHippocrates.Models;
using MyHippocrates.Views;

namespace MyHippocrates.ViewModels
{
    internal class ManufacturersViewModel : BaseViewModel
    {
        private readonly AppDbContext _ctx;
        private readonly ObservableCollection<Manufacturer> _manufacturers;
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

        public ManufacturersViewModel(AppDbContext ctx,
            ObservableCollection<Manufacturer> manufacturers)
        {
            _ctx = ctx;
            _manufacturers = manufacturers;
            View = CollectionViewSource.GetDefaultView(_manufacturers);
            View.Filter = obj =>
            {
                if (string.IsNullOrWhiteSpace(_search)) return true;
                if (obj is not Manufacturer m) return false;
                return m.Name.ToLower().Contains(_search.ToLower())
                    || m.Country.ToLower().Contains(_search.ToLower())
                    || m.Address.ToLower().Contains(_search.ToLower())
                    || m.Phone.ToLower().Contains(_search.ToLower());
            };
            AddCommand = new RelayCommand(_ => Add());
            EditCommand = new RelayCommand(m => Edit(m as Manufacturer), m => m is Manufacturer);
            DeleteCommand = new RelayCommand(m => Delete(m as Manufacturer), m => m is Manufacturer);
        }

        private void Add()
        {
            var entity = new Manufacturer();
            var dlg = new EditDialog(entity, _ctx, isNew: true)
            { Owner = Application.Current.MainWindow, Title = "Добавить производителя" };
            if (dlg.ShowDialog() == true) { _manufacturers.Add(entity); View.Refresh(); }
        }

        private void Edit(Manufacturer? m)
        {
            if (m == null) return;
            var copy = new Manufacturer
            {
                Id = m.Id,
                Name = m.Name,
                Country = m.Country,
                Address = m.Address,
                Phone = m.Phone,
                Email = m.Email
            };
            var dlg = new EditDialog(copy, _ctx, isNew: false)
            { Owner = Application.Current.MainWindow, Title = "Редактировать производителя" };
            if (dlg.ShowDialog() == true)
            {
                var idx = _manufacturers.IndexOf(m);
                if (idx >= 0) _manufacturers[idx] = copy;
                View.Refresh();
            }
        }

        private void Delete(Manufacturer? m)
        {
            if (m == null) return;
            var res = MessageBox.Show(
                $"Удалить производителя «{m.Name}»?\n\nВнимание: все связанные товары, позиции заказов и остатки склада будут удалены.",
                "Подтверждение", MessageBoxButton.YesNo, MessageBoxImage.Warning);
            if (res != MessageBoxResult.Yes) return;
            try
            {
                DbProcedures.DeleteManufacturer(_ctx, m.Id);
                _manufacturers.Remove(m);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.InnerException?.Message ?? ex.Message,
                    "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}