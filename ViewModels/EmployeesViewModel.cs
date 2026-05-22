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
    internal class EmployeesViewModel : BaseViewModel
    {
        private readonly AppDbContext _ctx;
        private readonly ObservableCollection<Employee> _employees;
        private readonly ObservableCollection<Role> _roles;
        private readonly ObservableCollection<Pharmacy> _pharmacies;  // ← добавить
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

        public EmployeesViewModel(AppDbContext ctx,
            ObservableCollection<Employee> employees,
            ObservableCollection<Role> roles,
            ObservableCollection<Pharmacy> pharmacies)
        {
            _ctx = ctx;
            _employees = employees;
            _roles = roles;
            _pharmacies = pharmacies; 

            View = CollectionViewSource.GetDefaultView(_employees);
            View.Filter = obj =>
            {
                if (string.IsNullOrWhiteSpace(_search)) return true;
                if (obj is not Employee e) return false;
                return e.FullName.ToLower().Contains(_search.ToLower())
                    || e.Phone.ToLower().Contains(_search.ToLower())
                    || e.Idnp.ToLower().Contains(_search.ToLower())
                    || e.Address.ToLower().Contains(_search.ToLower())
                    || (e.Role?.Name?.ToLower().Contains(_search.ToLower()) ?? false);
            };

            AddCommand = new RelayCommand(_ => Add());
            EditCommand = new RelayCommand(e => Edit(e as Employee), e => e is Employee);
            DeleteCommand = new RelayCommand(e => Delete(e as Employee), e => e is Employee);
        }

        private void Add()
        {
            var entity = new Employee();
            var vm = new EmployeeEditorViewModel(entity, _roles, _pharmacies);  
            var dlg = new Views.EditDialog(vm, _ctx, isNew: true)
            {
                Owner = Application.Current.MainWindow,
                Title = "Добавить сотрудника",
                Icon = new BitmapImage(new Uri("pack://application:,,,/add.ico"))
            };
            dlg.TxtTitle.Text = "Добавление записи";
            if (dlg.ShowDialog() == true)
            {
                entity.Role = _roles.FirstOrDefault(r => r.Id == entity.RoleId);
                entity.Pharmacy = _pharmacies.FirstOrDefault(p => p.Id == entity.PharmacyId);  
                _employees.Add(entity);
                View.Refresh();
            }
        }

        private void Edit(Employee? e)
        {
            if (e == null) return;
            var copy = new Employee
            {
                Id = e.Id,
                FullName = e.FullName,
                Idnp = e.Idnp,
                Phone = e.Phone,
                Address = e.Address,
                RoleId = e.RoleId,
                PharmacyId = e.PharmacyId  
            };
            var vm = new EmployeeEditorViewModel(copy, _roles, _pharmacies);  
            var dlg = new Views.EditDialog(vm, _ctx, isNew: false)
            {
                Owner = Application.Current.MainWindow,
                Title = "Редактировать сотрудника",
                Icon = new BitmapImage(new Uri("pack://application:,,,/edit.ico"))
            };
            if (dlg.ShowDialog() == true)
            {
                e.FullName = copy.FullName;
                e.Idnp = copy.Idnp;
                e.Phone = copy.Phone;
                e.Address = copy.Address;
                e.RoleId = copy.RoleId;
                e.PharmacyId = copy.PharmacyId; 
                e.Role = _roles.FirstOrDefault(r => r.Id == copy.RoleId);
                e.Pharmacy = _pharmacies.FirstOrDefault(p => p.Id == copy.PharmacyId);  

                var idx = _employees.IndexOf(e);
                if (idx >= 0)
                {
                    _employees.RemoveAt(idx);
                    _employees.Insert(idx, e);
                }
                View.Refresh();
            }
        }

        private void Delete(Employee? e)
        {
            if (e == null) return;

            var receiptCount = _ctx.Receipts.Count(r => r.EmployeeId == e.Id);
            var hasUser = _ctx.SystemUsers.Any(u => u.EmployeeId == e.Id);

            if (receiptCount > 0 || hasUser)
            {
                var details = new List<string>();
                if (receiptCount > 0) details.Add($"чеков: {receiptCount}");
                if (hasUser) details.Add("системный аккаунт");

                MessageBox.Show(
                    $"Невозможно удалить сотрудника «{e.FullName}».\n\n" +
                    $"С ним связано: {string.Join(", ", details)}.\n" +
                    "Сначала удалите все связанные записи.",
                    "Удаление невозможно", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var res = MessageBox.Show(
                $"Удалить сотрудника «{e.FullName}»?",
                "Подтверждение", MessageBoxButton.YesNo, MessageBoxImage.Question);
            if (res != MessageBoxResult.Yes) return;
            try
            {
                DbProcedures.DeleteEmployee(_ctx, e.Id);
                _employees.Remove(e);
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
            _employees.Clear();
            foreach (var e in _ctx.Employees
                .Include(x => x.Role)
                .Include(x => x.Pharmacy) 
                .OrderBy(x => x.Id).ToList())
                _employees.Add(e);
            View.Refresh();
        }
    }
}
