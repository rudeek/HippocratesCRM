using Microsoft.EntityFrameworkCore;
using MyHippocrates.Commands;
using MyHippocrates.Data;
using MyHippocrates.Models;
using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Windows;
using System.Windows.Data;

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
            { Owner = Application.Current.MainWindow, Title = "Добавить сотрудника" };
            if (dlg.ShowDialog() == true) { _employees.Add(entity); View.Refresh(); }
        }

        private void Edit(Employee? e)
        {
            if (e == null) return;
            _ctx.Entry(e).State = EntityState.Detached;
            var copy = new Employee { Id = e.Id, FullName = e.FullName, Idnp = e.Idnp, Phone = e.Phone, Address = e.Address, Salary = e.Salary, Position = e.Position };
            var dlg = new Views.EditDialog(copy, _ctx, isNew: false)
            { Owner = Application.Current.MainWindow, Title = "Редактировать сотрудника" };
            if (dlg.ShowDialog() == true)
            {
                var idx = _employees.IndexOf(e);
                if (idx >= 0) _employees[idx] = copy;
                View.Refresh();
            }
            else _ctx.Entry(e).State = EntityState.Unchanged;
        }

        private void Delete(Employee? e)
        {
            if (e == null) return;
            if (MessageBox.Show($"Удалить сотрудника «{e.FullName}»?", "Подтверждение",
                MessageBoxButton.YesNo, MessageBoxImage.Question) != MessageBoxResult.Yes) return;
            try { _ctx.Employees.Remove(e); _ctx.SaveChanges(); _employees.Remove(e); }
            catch (Exception ex) { MessageBox.Show(ex.InnerException?.Message ?? ex.Message, "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error); }
        }
    }
}