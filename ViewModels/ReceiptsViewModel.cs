// ══════════════════════════════════════════════════════════════════
// ReceiptsViewModel.cs
// ══════════════════════════════════════════════════════════════════
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

namespace MyHippocrates.ViewModels
{
    internal class ReceiptsViewModel : BaseViewModel
    {
        private readonly AppDbContext _ctx;
        private readonly ObservableCollection<Pharmacy> _pharmacies;
        private readonly ObservableCollection<Employee> _employees;
        private readonly ObservableCollection<Receipt> _receipts;
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

        public ReceiptsViewModel(AppDbContext ctx,
            ObservableCollection<Pharmacy> pharmacies,
            ObservableCollection<Employee> employees,
            ObservableCollection<Receipt> receipts)
        {
            _ctx = ctx; _pharmacies = pharmacies;
            _employees = employees; _receipts = receipts;

            View = CollectionViewSource.GetDefaultView(_receipts);
            View.Filter = obj =>
            {
                if (string.IsNullOrWhiteSpace(_search)) return true;
                if (obj is not Receipt r) return false;
                return r.ReceiptNumber.ToString().Contains(_search)
                    || (r.Pharmacy?.Address?.ToLower().Contains(_search.ToLower()) ?? false)
                    || (r.Employee?.FullName?.ToLower().Contains(_search.ToLower()) ?? false);
            };

            AddCommand = new RelayCommand(_ => Add());
            EditCommand = new RelayCommand(r => Edit(r as Receipt), r => r is Receipt);
            DeleteCommand = new RelayCommand(r => Delete(r as Receipt), r => r is Receipt);

            Load();
        }

        public void Reload() => Load();
        private void Load()
        {
            _receipts.Clear();
            foreach (var r in _ctx.Receipts
                .Include(x => x.Pharmacy).Include(x => x.Employee)
                .OrderBy(x => x.Id).ToList())
                _receipts.Add(r);
        }

        private void Add()
        {
            var entity = new Receipt { Date = DateTime.Today, Time = DateTime.Now.TimeOfDay };
            var vm = new ReceiptEditorViewModel(entity, _pharmacies, _employees);
            var dlg = new Views.EditDialog(vm, _ctx, isNew: true)
            { Owner = Application.Current.MainWindow, Title = "Добавить чек" };
            dlg.TxtTitle.Text = "Добавление записи";
            if (dlg.ShowDialog() == true)
            {
                entity.Pharmacy = _pharmacies.FirstOrDefault(p => p.Id == entity.PharmacyId);
                entity.Employee = _employees.FirstOrDefault(e => e.Id == entity.EmployeeId);
                _receipts.Add(entity); View.Refresh();
            }
        }

        private void Edit(Receipt? r)
        {
            if (r == null) return;
            var copy = new Receipt
            {
                Id = r.Id,
                ReceiptNumber = r.ReceiptNumber,
                PharmacyId = r.PharmacyId,
                EmployeeId = r.EmployeeId,
                TotalAmount = r.TotalAmount,
                Date = r.Date,
                Time = r.Time
            };
            var vm = new ReceiptEditorViewModel(copy, _pharmacies, _employees);
            var dlg = new Views.EditDialog(vm, _ctx, isNew: false)
            { Owner = Application.Current.MainWindow, Title = "Редактировать чек" };
            if (dlg.ShowDialog() == true)
            {
                copy.Pharmacy = _pharmacies.FirstOrDefault(p => p.Id == copy.PharmacyId);
                copy.Employee = _employees.FirstOrDefault(e => e.Id == copy.EmployeeId);
                var idx = _receipts.IndexOf(r);
                if (idx >= 0) _receipts[idx] = copy;
                View.Refresh();
            }
        }

        private void Delete(Receipt? r)
        {
            if (r == null) return;
            var res = MessageBox.Show(
                $"Удалить чек №{r.ReceiptNumber}?\n\nВнимание: все позиции этого чека будут удалены.",
                "Подтверждение", MessageBoxButton.YesNo, MessageBoxImage.Warning);
            if (res != MessageBoxResult.Yes) return;
            try { DbProcedures.DeleteReceipt(_ctx, r.Id); _receipts.Remove(r); }
            catch (Exception ex)
            { MessageBox.Show(ex.InnerException?.Message ?? ex.Message, "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error); }
        }
    }
}