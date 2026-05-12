using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media.Imaging;
using MyHippocrates.Commands;
using MyHippocrates.Data;
using MyHippocrates.Models;

namespace MyHippocrates.ViewModels
{
    internal class RoleViewModel : BaseViewModel
    {
        private readonly AppDbContext _ctx;
        private readonly ObservableCollection<Role> _roles;
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

        public RoleViewModel(AppDbContext ctx, ObservableCollection<Role> roles)
        {
            _ctx = ctx;
            _roles = roles;

            View = CollectionViewSource.GetDefaultView(_roles);
            View.Filter = obj =>
            {
                if (string.IsNullOrWhiteSpace(_search)) return true;
                if (obj is not Role r) return false;
                return r.Name.ToLower().Contains(_search.ToLower())
                    || (r.Description?.ToLower().Contains(_search.ToLower()) ?? false)
                    || r.FixedSalary.ToString().Contains(_search);
            };

            AddCommand = new RelayCommand(_ => Add());
            EditCommand = new RelayCommand(r => Edit(r as Role), r => r is Role);
            DeleteCommand = new RelayCommand(r => Delete(r as Role), r => r is Role);
        }

        private void Add()
        {
            var entity = new Role();
            var dlg = new Views.EditDialog(entity, _ctx, isNew: true)
            {
                Owner = Application.Current.MainWindow,
                Title = "Добавить должность",
                Icon = new BitmapImage(new Uri("pack://application:,,,/add.ico"))
            };
            dlg.TxtTitle.Text = "Добавление записи";
            if (dlg.ShowDialog() == true)
            {
                _roles.Add(entity);
                View.Refresh();
            }
        }

        private void Edit(Role? r)
        {
            if (r == null) return;
            var copy = new Role
            {
                Id = r.Id,
                Name = r.Name,
                FixedSalary = r.FixedSalary,
                Description = r.Description
            };
            var dlg = new Views.EditDialog(copy, _ctx, isNew: false)
            {
                Owner = Application.Current.MainWindow,
                Title = "Редактировать должность",
                Icon = new BitmapImage(new Uri("pack://application:,,,/edit.ico"))
            };
            if (dlg.ShowDialog() == true)
            {
                r.Name = copy.Name;
                r.FixedSalary = copy.FixedSalary;
                r.Description = copy.Description;

                var idx = _roles.IndexOf(r);
                if (idx >= 0)
                {
                    _roles.RemoveAt(idx);
                    _roles.Insert(idx, r);
                }
                View.Refresh();
            }
        }

        private void Delete(Role? r)
        {
            if (r == null) return;
            var res = MessageBox.Show(
                $"Удалить должность «{r.Name}»?\n\nВнимание: сотрудники с этой должностью потеряют привязку.",
                "Подтверждение", MessageBoxButton.YesNo, MessageBoxImage.Warning);
            if (res != MessageBoxResult.Yes) return;
            try
            {
                DbProcedures.DeleteRole(_ctx, r.Id);
                _roles.Remove(r);
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
            _roles.Clear();
            foreach (var r in _ctx.Roles.OrderBy(x => x.Id).ToList())
                _roles.Add(r);
            View.Refresh();
        }
    }
}