using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using MyHippocrates.Models;

namespace MyHippocrates.ViewModels
{
    // ── ProductEditorViewModel ────────────────────────────────────
    public class ProductEditorViewModel : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler? PropertyChanged;
        private void OnPropertyChanged([CallerMemberName] string name = "") =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));

        public Product Entity { get; }
        public ObservableCollection<Manufacturer> Manufacturers { get; }
        public ObservableCollection<Category> Categories { get; }

        public ProductEditorViewModel(Product entity,
            ObservableCollection<Manufacturer> manufacturers,
            ObservableCollection<Category> categories)
        {
            Entity = entity;
            Manufacturers = manufacturers;
            Categories = categories;
            if (Entity.ProductionDate == default)
                Entity.ProductionDate = DateTime.UtcNow;
            if (Entity.ExpirationDate == default)
                Entity.ExpirationDate = DateTime.UtcNow.AddYears(2);
        }
    }

    public class EmployeeEditorViewModel
    {
        public Employee Entity { get; }
        public ObservableCollection<Role> Roles { get; }

        public EmployeeEditorViewModel(Employee entity, ObservableCollection<Role> roles)
        {
            Entity = entity;
            Roles = roles;
        }
    }

    // ── ReceiptEditorViewModel ────────────────────────────────────
    public class ReceiptEditorViewModel
    {
        public Receipt Entity { get; }
        public ObservableCollection<Pharmacy> Pharmacies { get; }
        public ObservableCollection<Employee> Employees { get; }

        public ReceiptEditorViewModel(Receipt entity,
            ObservableCollection<Pharmacy> pharmacies,
            ObservableCollection<Employee> employees)
        {
            Entity = entity;
            Pharmacies = pharmacies;
            Employees = employees;
            if (Entity.Date == default)
                Entity.Date = DateTime.Today;
        }
    }

    // ── OrderItemEditorViewModel ──────────────────────────────────
    // Фильтрует список товаров по аптеке выбранного чека.
    public class OrderItemEditorViewModel : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler? PropertyChanged;
        private void OnPropertyChanged([CallerMemberName] string name = "") =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));

        public OrderItem Entity { get; }

        //Все чеки для ComboBox
        public ObservableCollection<Receipt> Receipts { get; }

        //Все товары + все остатки (для фильтрации)
        private readonly ObservableCollection<Product> _allProducts;
        private readonly IReadOnlyList<StockBalance> _stockBalances;

        //Товары, доступные в аптеке выбранного чека
        private ObservableCollection<Product> _availableProducts = new();
        public ObservableCollection<Product> AvailableProducts
        {
            get => _availableProducts;
            private set { _availableProducts = value; OnPropertyChanged(); }
        }

        //Выбранный чек — при смене обновляем список товаров
        private Receipt? _selectedReceipt;
        public Receipt? SelectedReceipt
        {
            get => _selectedReceipt;
            set
            {
                _selectedReceipt = value;
                Entity.ReceiptId = value?.Id ?? 0;
                OnPropertyChanged();
                RefreshAvailableProducts();

                //Если выбранный товар недоступен в новой аптеке — сбрасываем
                if (SelectedProduct != null &&
                    !AvailableProducts.Any(p => p.Id == SelectedProduct.Id))
                    SelectedProduct = null;
            }
        }

        //Выбранный товар
        private Product? _selectedProduct;
        public Product? SelectedProduct
        {
            get => _selectedProduct;
            set
            {
                _selectedProduct = value;
                Entity.ProductId = value?.Id ?? 0;
                OnPropertyChanged();
            }
        }

        public OrderItemEditorViewModel(
            OrderItem entity,
            ObservableCollection<Receipt> receipts,
            ObservableCollection<Product> allProducts,
            IReadOnlyList<StockBalance> stockBalances)
        {
            Entity = entity;
            Receipts = receipts;
            _allProducts = allProducts;
            _stockBalances = stockBalances;

            //Восстанавливаем выбранные значения при редактировании
            _selectedReceipt = receipts.FirstOrDefault(r => r.Id == entity.ReceiptId);
            RefreshAvailableProducts();
            _selectedProduct = _availableProducts.FirstOrDefault(p => p.Id == entity.ProductId);
        }

        private void RefreshAvailableProducts()
        {
            if (_selectedReceipt == null)
            {
                AvailableProducts = new ObservableCollection<Product>(_allProducts);
                return;
            }

            var pharmacyId = _selectedReceipt.PharmacyId;
            //Товары, у которых есть остаток > 0 в данной аптеке
            var available = _stockBalances
                .Where(sb => sb.PharmacyId == pharmacyId && sb.RemainingQty > 0)
                .Select(sb => _allProducts.FirstOrDefault(p => p.Id == sb.ProductId))
                .Where(p => p != null)
                .Cast<Product>()
                .ToList();

            AvailableProducts = new ObservableCollection<Product>(available);
        }
    }

    // ── StockBalanceEditorViewModel ───────────────────────────────
    public class StockBalanceEditorViewModel
    {
        public StockBalance Entity { get; }
        public ObservableCollection<Pharmacy> Pharmacies { get; }
        public ObservableCollection<Product> Products { get; }

        public StockBalanceEditorViewModel(StockBalance entity,
            ObservableCollection<Pharmacy> pharmacies,
            ObservableCollection<Product> products)
        {
            Entity = entity;
            Pharmacies = pharmacies;
            Products = products;
        }
    }


    // ══ Добавить в ViewModels/EditorViewModels.cs ══

    // ── CategoryEditorViewModel ───────────────────────────────────
    // Category — простая сущность, DataTemplate в App.xaml
    // использует Category напрямую (как Manufacturer/Pharmacy),
    // поэтому отдельный editor VM не нужен.
    // Но для единообразия с паттерном добавляем пустую обёртку:
    // (можно не добавлять — EditDialog умеет работать с Category напрямую)

    // ── RoleEditorViewModel ───────────────────────────────────────
    // Аналогично — Role простая, DataTemplate работает с Role напрямую.

    // ── SystemUserEditorViewModel ─────────────────────────────────
    // SystemUser требует обёртки: нужен список сотрудников для ComboBox
    // и отдельное поле plain-text пароля (не хранится в модели).
    public class SystemUserEditorViewModel : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler? PropertyChanged;
        private void OnPropertyChanged([CallerMemberName] string name = "") =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));

        public SystemUser Entity { get; }

        /// <summary>
        /// Только для записей, у которых ещё нет аккаунта (фильтрованный список).
        /// При редактировании — все сотрудники (текущий сотрудник всегда доступен).
        /// </summary>
        public ObservableCollection<Employee> AvailableEmployees { get; }

        /// <summary>
        /// Plain-text пароль. При добавлении — обязателен.
        /// При редактировании — оставить пустым, чтобы не менять пароль.
        /// </summary>
        private string _plainPassword = "";
        public string PlainPassword
        {
            get => _plainPassword;
            set { _plainPassword = value; OnPropertyChanged(); }
        }

        private Employee? _selectedEmployee;
        public Employee? SelectedEmployee
        {
            get => _selectedEmployee;
            set
            {
                _selectedEmployee = value;
                Entity.EmployeeId = value?.Id ?? 0;
                OnPropertyChanged();
            }
        }

        public SystemUserEditorViewModel(
            SystemUser entity,
            ObservableCollection<Employee> availableEmployees)
        {
            Entity = entity;
            AvailableEmployees = availableEmployees;
            _selectedEmployee = availableEmployees.FirstOrDefault(e => e.Id == entity.EmployeeId);
        }
    }
}