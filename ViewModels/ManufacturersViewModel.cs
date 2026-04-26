using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows.Data;
using Microsoft.EntityFrameworkCore;
using MyHippocrates.Commands;
using MyHippocrates.Data;
using MyHippocrates.Models;
using MyHippocrates.Views;
using System.Windows;

namespace MyHippocrates.ViewModels
{
    internal class ManufacturersViewModel : BaseViewModel
    {
        private readonly AppDbContext dbContext;

        private readonly ObservableCollection<Manufacturer> manufacturers;

        public ICollectionView View { get; }

        public RelayCommand AddCommand { get; }
        public RelayCommand EditCommand { get; }
        public RelayCommand DeleteCommand { get; }

        private string search = "";
        public string SearchText
        {
            get => search;
            set
            {
                SetProperty(ref search, value);
                View.Refresh();
            }
        }

        public ManufacturersViewModel(AppDbContext dbContext, ObservableCollection<Manufacturer> manufacturers)
        {
            this.dbContext = dbContext;//подключаем контекст БД
            this.manufacturers = manufacturers;
            View = CollectionViewSource.GetDefaultView(manufacturers);//представляем коллекцию в виде представления для фильтрации и сортировки
            View.Filter = obj =>
            {
                if (string.IsNullOrWhiteSpace(search)) return true;
                if (obj is not Manufacturer m) return false;
                return m.Name.ToLower().Contains(search.ToLower())
                       || m.Country.ToLower().Contains(search.ToLower())
                       || m.Address.ToLower().Contains(search.ToLower())
                       || m.Phone.ToLower().Contains(search.ToLower());
            }; //задаем фильтр для поиска по названию производителя

            AddCommand = new RelayCommand(_ => Add());
            EditCommand = new RelayCommand(m => Edit(m as Manufacturer), m => m is Manufacturer);
            DeleteCommand = new RelayCommand(m => Delete(m as Manufacturer), m => m is Manufacturer);
        }

        private void Add()
        {
            var entity = new Manufacturer();

            var dialog = new EditDialog(entity, dbContext, isNew: true)
            {
                Owner = Application.Current.MainWindow,
                Title = "Добавить производителя"
            };

            if(dialog.ShowDialog() == true)
            {
                manufacturers.Add(entity);
                View.Refresh();
            }
        }
        private void Edit(Manufacturer? m)
        {
            if (m == null) return;

            //Отсоединяем оригинал от трекера EF
            dbContext.Entry(m).State = EntityState.Detached;

            var copy = new Manufacturer
            {
                Id = m.Id,
                Name = m.Name,
                Country = m.Country,
                Address = m.Address,
                Phone = m.Phone,
                Email = m.Email
            };

            var dialog = new EditDialog(copy, dbContext, isNew: false)
            {
                Owner = Application.Current.MainWindow,
                Title = "Редактировать производителя"
            };

            if (dialog.ShowDialog() == true)
            {
                var idx = manufacturers.IndexOf(m);
                if (idx >= 0) manufacturers[idx] = copy;
                View.Refresh();
            }
            else
            {
                //Если отмена — возвращаем оригинал обратно в трекер
                dbContext.Entry(m).State = EntityState.Unchanged;
            }
        }

        private void Delete(Manufacturer? m)
        {
            if(m == null) return;

            var result = MessageBox.Show($"Удалить производителя \"{m.Name}\"?", "Подтверждение", MessageBoxButton.YesNo, MessageBoxImage.Question);

            if(result != MessageBoxResult.Yes) return;

            try
            {
                dbContext.Manufacturers.Remove(m);
                dbContext.SaveChanges();
                manufacturers.Remove(m);
            }
            catch(Exception e)
            {
                MessageBox.Show(e.InnerException?.Message ?? e.Message,
                    "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}
