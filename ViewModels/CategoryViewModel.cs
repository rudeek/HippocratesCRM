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
    internal class CategoryViewModel : BaseViewModel
    {
        private readonly AppDbContext _ctx;
        private readonly ObservableCollection<Category> _categories;
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

        public CategoryViewModel(AppDbContext ctx, ObservableCollection<Category> categories)
        {
            _ctx = ctx;
            _categories = categories;

            View = CollectionViewSource.GetDefaultView(_categories);
            View.Filter = obj =>
            {
                if (string.IsNullOrWhiteSpace(_search)) return true;
                if (obj is not Category c) return false;
                return c.Name.ToLower().Contains(_search.ToLower())
                    || (c.Description?.ToLower().Contains(_search.ToLower()) ?? false);
            };

            AddCommand = new RelayCommand(_ => Add());
            EditCommand = new RelayCommand(c => Edit(c as Category), c => c is Category);
            DeleteCommand = new RelayCommand(c => Delete(c as Category), c => c is Category);
        }

        private void Add()
        {
            var entity = new Category();
            var dlg = new Views.EditDialog(entity, _ctx, isNew: true)
            {
                Owner = Application.Current.MainWindow,
                Title = "Добавить категорию",
                Icon = new BitmapImage(new Uri("pack://application:,,,/add.ico"))
            };
            dlg.TxtTitle.Text = "Добавление записи";
            if (dlg.ShowDialog() == true)
            {
                _categories.Add(entity);
                View.Refresh();
            }
        }

        private void Edit(Category? c)
        {
            if (c == null) return;
            var copy = new Category
            {
                Id = c.Id,
                Name = c.Name,
                Description = c.Description
            };
            var dlg = new Views.EditDialog(copy, _ctx, isNew: false)
            {
                Owner = Application.Current.MainWindow,
                Title = "Редактировать категорию",
                Icon = new BitmapImage(new Uri("pack://application:,,,/edit.ico"))
            };
            if (dlg.ShowDialog() == true)
            {
                c.Name = copy.Name;
                c.Description = copy.Description;

                var idx = _categories.IndexOf(c);
                if (idx >= 0)
                {
                    _categories.RemoveAt(idx);
                    _categories.Insert(idx, c);
                }
                View.Refresh();
            }
        }

        private void Delete(Category? c)
        {
            if (c == null) return;

            var productCount = _ctx.Products.Count(p => p.CategoryId == c.Id);
            if (productCount > 0)
            {
                MessageBox.Show(
                    $"Невозможно удалить категорию «{c.Name}».\n\n" +
                    $"К ней привязано товаров: {productCount}.\n" +
                    "Сначала переназначьте или удалите все товары этой категории.",
                    "Удаление невозможно", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var res = MessageBox.Show(
                $"Удалить категорию «{c.Name}»?",
                "Подтверждение", MessageBoxButton.YesNo, MessageBoxImage.Question);
            if (res != MessageBoxResult.Yes) return;
            try
            {
                DbProcedures.DeleteCategory(_ctx, c.Id);
                _categories.Remove(c);
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
            _categories.Clear();
            foreach (var c in _ctx.Categories.OrderBy(x => x.Id).ToList())
                _categories.Add(c);
            View.Refresh();
        }
    }
}