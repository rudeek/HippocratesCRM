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

        // Обёртка для FilePath чтобы TextBox обновлялся через INotifyPropertyChanged
        public string? FilePath
        {
            get => Entity.FilePath;
            set
            {
                Entity.FilePath = value;
                OnPropertyChanged();
            }
        }

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
        public ObservableCollection<Pharmacy> Pharmacies { get; }

        public EmployeeEditorViewModel(Employee entity, ObservableCollection<Role> roles, ObservableCollection<Pharmacy> pharmacies)
        {
            Entity = entity;
            Roles = roles;
            Pharmacies = pharmacies;    
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

    public class OrderItemEditorViewModel : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler? PropertyChanged;
        private void OnPropertyChanged([CallerMemberName] string name = "") =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));

        public OrderItem Entity { get; }

        private readonly ObservableCollection<Receipt> _allReceipts;
        private readonly ObservableCollection<Product> _allProducts;
        private readonly IReadOnlyList<StockBalance> _stockBalances;

        public ObservableCollection<Pharmacy> Pharmacies { get; }

        private ObservableCollection<Receipt> _filteredReceipts = new();
        public ObservableCollection<Receipt> FilteredReceipts
        {
            get => _filteredReceipts;
            private set { _filteredReceipts = value; OnPropertyChanged(); }
        }

        private ObservableCollection<Product> _availableProducts = new();
        public ObservableCollection<Product> AvailableProducts
        {
            get => _availableProducts;
            private set { _availableProducts = value; OnPropertyChanged(); }
        }

        private Pharmacy? _selectedPharmacy;
        public Pharmacy? SelectedPharmacy
        {
            get => _selectedPharmacy;
            set
            {
                _selectedPharmacy = value;
                OnPropertyChanged();
                SelectedReceipt = null;
                SelectedProduct = null;
                RefreshFilteredReceipts();
            }
        }

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
                if (SelectedProduct != null &&
                    !AvailableProducts.Any(p => p.Id == SelectedProduct.Id))
                    SelectedProduct = null;
            }
        }

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
            ObservableCollection<Receipt> allReceipts,
            ObservableCollection<Product> allProducts,
            IReadOnlyList<StockBalance> stockBalances,
            ObservableCollection<Pharmacy> pharmacies)
        {
            Entity = entity;
            _allReceipts = allReceipts;
            _allProducts = allProducts;
            _stockBalances = stockBalances;
            Pharmacies = pharmacies;

            if (entity.ReceiptId != 0)
            {
                var receipt = allReceipts.FirstOrDefault(r => r.Id == entity.ReceiptId);
                if (receipt != null)
                {
                    _selectedPharmacy = pharmacies.FirstOrDefault(p => p.Id == receipt.PharmacyId);
                    RefreshFilteredReceipts();
                    _selectedReceipt = FilteredReceipts.FirstOrDefault(r => r.Id == entity.ReceiptId);
                    Entity.ReceiptId = _selectedReceipt?.Id ?? 0;
                    RefreshAvailableProducts();
                    _selectedProduct = AvailableProducts.FirstOrDefault(p => p.Id == entity.ProductId);
                    Entity.ProductId = _selectedProduct?.Id ?? 0;
                }
            }
            else
            {
                RefreshFilteredReceipts();
            }
        }

        private void RefreshFilteredReceipts()
        {
            var today = DateTime.Today;
            var result = _allReceipts
                .Where(r => r.Date.Date == today)
                .AsEnumerable();

            if (_selectedPharmacy != null)
                result = result.Where(r => r.PharmacyId == _selectedPharmacy.Id);

            FilteredReceipts = new ObservableCollection<Receipt>(result.OrderBy(r => r.ReceiptNumber));
        }

        private void RefreshAvailableProducts()
        {
            if (_selectedReceipt == null)
            {
                AvailableProducts = new ObservableCollection<Product>();
                return;
            }

            var pharmacyId = _selectedReceipt.PharmacyId;
            var available = _stockBalances
                .Where(sb => sb.PharmacyId == pharmacyId && sb.RemainingQty > 0)
                .Select(sb => _allProducts.FirstOrDefault(p => p.Id == sb.ProductId))
                .Where(p => p != null)
                .Cast<Product>()
                .OrderBy(p => p.Name)
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

    // ── SystemUserEditorViewModel ─────────────────────────────────
    public class SystemUserEditorViewModel : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler? PropertyChanged;
        private void OnPropertyChanged([CallerMemberName] string name = "") =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));

        public SystemUser Entity { get; }
        public ObservableCollection<Employee> AvailableEmployees { get; }

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
            SelectedEmployee = availableEmployees.FirstOrDefault(e => e.Id == entity.EmployeeId);
        }
    }
}