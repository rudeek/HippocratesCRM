using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media.Imaging;
using MyHippocrates.Commands;
using MyHippocrates.Data;
using MyHippocrates.Models;
using MyHippocrates.Views;

namespace MyHippocrates.ViewModels
{
    internal class PharmaciesViewModel : BaseViewModel
    {
        private readonly AppDbContext _ctx;
        private readonly ObservableCollection<Pharmacy> _pharmacies;
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

        public PharmaciesViewModel(AppDbContext ctx, ObservableCollection<Pharmacy> pharmacies)
        {
            _ctx = ctx;
            _pharmacies = pharmacies;
            View = CollectionViewSource.GetDefaultView(_pharmacies);
            View.Filter = obj =>
            {
                if (string.IsNullOrWhiteSpace(_search)) return true;
                if (obj is not Pharmacy p) return false;
                return p.Address.ToLower().Contains(_search.ToLower())
                || p.Phone.ToLower().Contains(_search.ToLower())
                || p.WorkingHours.ToLower().Contains(_search.ToLower());
            };
            AddCommand = new RelayCommand(_ => Add());
            EditCommand = new RelayCommand(p => Edit(p as Pharmacy), p => p is Pharmacy);
            DeleteCommand = new RelayCommand(p => Delete(p as Pharmacy), p => p is Pharmacy);
        }

        private void Add()
        {
            var entity = new Pharmacy();
            var dlg = new Views.EditDialog(entity, _ctx, isNew: true)
            { Owner = Application.Current.MainWindow, Title = "Добавить аптеку", Icon = new BitmapImage(new Uri("pack://application:,,,/add.ico")) };
            dlg.TxtTitle.Text = "Добавление записи";
            if (dlg.ShowDialog() == true)
            {
                _pharmacies.Add(entity);
                View.Refresh();
            }
        }

        private void Edit(Pharmacy? p)
        {
            if (p == null) return;
            var copy = new Pharmacy
            { Id = p.Id, Address = p.Address, Phone = p.Phone, WorkingHours = p.WorkingHours };
            var dlg = new Views.EditDialog(copy, _ctx, isNew: false)
            { Owner = Application.Current.MainWindow, Title = "Редактировать аптеку", Icon = new BitmapImage(new Uri("pack://application:,,,/edit.ico")) };
            if (dlg.ShowDialog() == true)
            {
                p.Address = copy.Address;
                p.Phone = copy.Phone;
                p.WorkingHours = copy.WorkingHours;

                var idx = _pharmacies.IndexOf(p);
                if (idx >= 0)
                {
                    _pharmacies.RemoveAt(idx);
                    _pharmacies.Insert(idx, p);
                }
                View.Refresh();
            }
        }

        private void Delete(Pharmacy? p)
        {
            if (p == null) return;

            var receiptCount = _ctx.Receipts.Count(r => r.PharmacyId == p.Id);
            var stockCount = _ctx.StockBalances.Count(s => s.PharmacyId == p.Id);

            if (receiptCount > 0 || stockCount > 0)
            {
                var details = new List<string>();
                if (receiptCount > 0) details.Add($"чеков: {receiptCount}");
                if (stockCount > 0) details.Add($"записей на складе: {stockCount}");

                MessageBox.Show(
                    $"Невозможно удалить аптеку «{p.Address}».\n\n" +
                    $"С ней связано: {string.Join(", ", details)}.\n" +
                    "Сначала удалите все связанные чеки и остатки склада.",
                    "Удаление невозможно", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var res = MessageBox.Show(
                $"Удалить аптеку «{p.Address}»?",
                "Подтверждение", MessageBoxButton.YesNo, MessageBoxImage.Question);
            if (res != MessageBoxResult.Yes) return;
            try
            {
                DbProcedures.DeletePharmacy(_ctx, p.Id);
                _pharmacies.Remove(p);
                MyHippocrates.Views.ToastService.ShowSuccess("Запись успешно удалена.");
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.InnerException?.Message ?? ex.Message,
                    "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        public void Reload()
        {
            _ctx.ChangeTracker.Clear();
            _pharmacies.Clear();
            foreach (var p in _ctx.Pharmacies.OrderBy(x => x.Id).ToList())
                _pharmacies.Add(p);
            View.Refresh();
        }
    }
}
