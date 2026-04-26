using Microsoft.EntityFrameworkCore;
using MyHippocrates.Commands;
using MyHippocrates.Data;
using MyHippocrates.Models;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Windows;
using System.Windows.Data;

namespace MyHippocrates.ViewModels
{
    internal class PharmaciesViewModel : BaseViewModel
    {
        private readonly AppDbContext dbContext;

        private ObservableCollection<Pharmacy> pharmacies;

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

        public PharmaciesViewModel(AppDbContext dbContext, ObservableCollection<Pharmacy> pharmacies)
        {
            this.dbContext = dbContext;
            this.pharmacies = pharmacies;

            View = CollectionViewSource.GetDefaultView(pharmacies);

            View.Filter = obj =>
            {
                if (string.IsNullOrWhiteSpace(search)) return true;
                if (obj is not Pharmacy p) return false;

                return (p.Address?.ToLower().Contains(search.ToLower()) ?? false);
            };

            AddCommand = new RelayCommand(_ => Add());
            EditCommand = new RelayCommand(p => Edit(p as Pharmacy), p => p is Pharmacy);
            DeleteCommand = new RelayCommand(p => Delete(p as Pharmacy), p => p is Pharmacy);
        }

        private void Add()
        {
            var entity = new Pharmacy();

            var dialog = new Views.EditDialog(entity, dbContext, isNew: true)
            {
                Owner = Application.Current.MainWindow,
                Title = "Добавить аптеку"
            };

            if (dialog.ShowDialog() == true)
            {
                pharmacies.Add(entity);
                View.Refresh();
            }
        }

        private void Edit(Pharmacy? p)
        {
            if (p == null) return;

            dbContext.Entry(p).State = EntityState.Detached;

            var copy = new Pharmacy
            {
                Id = p.Id,
                Address = p.Address,
                Phone = p.Phone,
                WorkingHours = p.WorkingHours
            };

            var dialog = new Views.EditDialog(copy, dbContext, isNew: false)
            {
                Owner = Application.Current.MainWindow,
                Title = "Редактировать аптеку"
            };

            if (dialog.ShowDialog() == true)
            {
                var idx = pharmacies.IndexOf(p);
                if (idx >= 0) pharmacies[idx] = copy;

                View.Refresh();
            }
            else
            {
                dbContext.Entry(p).State = EntityState.Unchanged;
            }
        }

        private void Delete(Pharmacy? p)
        {
            if (p == null) return;

            var result = MessageBox.Show(
                $"Удалить аптеку с адресом \"{p.Address}\"?",
                "Подтверждение",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (result != MessageBoxResult.Yes) return;

            try
            {
                dbContext.Pharmacies.Remove(p);
                dbContext.SaveChanges();
                pharmacies.Remove(p);
            }
            catch (Exception e)
            {
                MessageBox.Show(
                    e.InnerException?.Message ?? e.Message,
                    "Ошибка",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }
    }
}