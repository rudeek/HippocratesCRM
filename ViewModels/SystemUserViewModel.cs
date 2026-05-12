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
    internal class SystemUserViewModel : BaseViewModel
    {
        private readonly AppDbContext _ctx;
        private readonly ObservableCollection<Employee> _allEmployees;
        private readonly ObservableCollection<SystemUser> _users = new();
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

        public SystemUserViewModel(AppDbContext ctx, ObservableCollection<Employee> allEmployees)
        {
            _ctx = ctx;
            _allEmployees = allEmployees;

            View = CollectionViewSource.GetDefaultView(_users);
            View.Filter = obj =>
            {
                if (string.IsNullOrWhiteSpace(_search)) return true;
                if (obj is not SystemUser u) return false;
                return u.Login.ToLower().Contains(_search.ToLower())
                    || u.SystemRole.ToLower().Contains(_search.ToLower())
                    || (u.Employee?.FullName?.ToLower().Contains(_search.ToLower()) ?? false);
            };

            AddCommand = new RelayCommand(_ => Add());
            EditCommand = new RelayCommand(u => Edit(u as SystemUser), u => u is SystemUser);
            DeleteCommand = new RelayCommand(u => Delete(u as SystemUser), u => u is SystemUser);

            Load();
        }

        public void Reload() => Load();

        private void Load()
        {
            _ctx.ChangeTracker.Clear();
            _users.Clear();
            foreach (var u in _ctx.SystemUsers
                .Include(x => x.Employee)
                .OrderBy(x => x.Id).ToList())
                _users.Add(u);
        }

        private void Add()
        {
            // Сотрудники, у которых ещё нет аккаунта
            var takenIds = _users.Select(u => u.EmployeeId).ToHashSet();
            var free = new ObservableCollection<Employee>(
                _allEmployees.Where(e => !takenIds.Contains(e.Id)));

            if (free.Count == 0)
            {
                MessageBox.Show("Все сотрудники уже имеют системный аккаунт.",
                    "Нет свободных сотрудников", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            var entity = new SystemUser { IsActive = true };
            var vm = new SystemUserEditorViewModel(entity, free);
            var dlg = new Views.EditDialog(vm, _ctx, isNew: true)
            {
                Owner = Application.Current.MainWindow,
                Title = "Добавить пользователя",
                Icon = new BitmapImage(new Uri("pack://application:,,,/add.ico"))
            };
            dlg.TxtTitle.Text = "Добавление записи";
            if (dlg.ShowDialog() == true)
            {
                entity.Employee = _allEmployees.FirstOrDefault(e => e.Id == entity.EmployeeId);
                // system_role проставит триггер — перезагружаем, чтобы увидеть актуальное значение
                Reload();
            }
        }

        private void Edit(SystemUser? u)
        {
            if (u == null) return;

            // При редактировании — все сотрудники доступны
            // (текущий сотрудник должен остаться в списке, даже если у него уже есть аккаунт)
            var takenIds = _users
                .Where(x => x.Id != u.Id)
                .Select(x => x.EmployeeId)
                .ToHashSet();

            var available = new ObservableCollection<Employee>(
                _allEmployees.Where(e => !takenIds.Contains(e.Id)));

            var copy = new SystemUser
            {
                Id = u.Id,
                EmployeeId = u.EmployeeId,
                Login = u.Login,
                PasswordHash = u.PasswordHash,
                SystemRole = u.SystemRole,
                IsActive = u.IsActive
            };

            var vm = new SystemUserEditorViewModel(copy, available);
            var dlg = new Views.EditDialog(vm, _ctx, isNew: false)
            {
                Owner = Application.Current.MainWindow,
                Title = "Редактировать пользователя",
                Icon = new BitmapImage(new Uri("pack://application:,,,/edit.ico"))
            };

            if (dlg.ShowDialog() == true)
            {
                Reload();
            }
        }

        private void Delete(SystemUser? u)
        {
            if (u == null) return;
            var name = u.Employee?.FullName ?? u.Login;
            var res = MessageBox.Show(
                $"Удалить аккаунт пользователя «{name}» (логин: {u.Login})?",
                "Подтверждение", MessageBoxButton.YesNo, MessageBoxImage.Warning);
            if (res != MessageBoxResult.Yes) return;
            try
            {
                DbProcedures.DeleteSystemUser(_ctx, u.Id);
                _users.Remove(u);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.InnerException?.Message ?? ex.Message,
                    "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}