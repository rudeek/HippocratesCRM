using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media.Imaging;
using MyHippocrates.Commands;
using MyHippocrates.Data;
using MyHippocrates.Models;

namespace MyHippocrates.ViewModels
{
    internal class EmployeesViewModel : BaseViewModel
    {
        private readonly AppDbContext _ctx;
        private readonly ObservableCollection<Employee> _employees;
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

        public EmployeesViewModel(AppDbContext ctx, ObservableCollection<Employee> employees)
        {
            _ctx = ctx;
            _employees = employees;
            View = CollectionViewSource.GetDefaultView(_employees);
            View.Filter = obj =>
            {
                if (string.IsNullOrWhiteSpace(_search)) return true;
                if (obj is not Employee e) return false;
                return e.FullName.ToLower().Contains(_search.ToLower())
                    || (e.Position?.ToLower().Contains(_search.ToLower()) ?? false);
            };
            AddCommand = new RelayCommand(_ => Add());
            EditCommand = new RelayCommand(e => Edit(e as Employee), e => e is Employee);
            DeleteCommand = new RelayCommand(e => Delete(e as Employee), e => e is Employee);
        }

        private void Add()
        {
            var entity = new Employee();
            var dlg = new Views.EditDialog(entity, _ctx, isNew: true)
            { Owner = Application.Current.MainWindow, Title = "Добавить сотрудника", Icon = new BitmapImage(new Uri("pack://application:,,,/add.ico")) };
            dlg.TxtTitle.Text = "Добавление записи";
            if (dlg.ShowDialog() == true)
            {
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
                Salary = e.Salary,
                Position = e.Position
            };
            var dlg = new Views.EditDialog(copy, _ctx, isNew: false)
            { Owner = Application.Current.MainWindow, Title = "Редактировать сотрудника", Icon = new BitmapImage(new Uri("pack://application:,,,/edit.ico")) };
            if (dlg.ShowDialog() == true)
            {
                e.FullName = copy.FullName;
                e.Idnp = copy.Idnp;
                e.Phone = copy.Phone;
                e.Address = copy.Address;
                e.Salary = copy.Salary;
                e.Position = copy.Position;

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
            var res = MessageBox.Show(
                $"Удалить сотрудника «{e.FullName}»?\n\nВнимание: все связанные чеки и позиции заказов будут удалены.",
                "Подтверждение", MessageBoxButton.YesNo, MessageBoxImage.Warning);
            if (res != MessageBoxResult.Yes) return;
            try
            {
                DbProcedures.DeleteEmployee(_ctx, e.Id);
                _employees.Remove(e);
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
            foreach (var e in _ctx.Employees.OrderBy(x => x.Id).ToList())
                _employees.Add(e);
            View.Refresh();
        }
    }
}