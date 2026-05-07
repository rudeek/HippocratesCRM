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
    public class ProductEditorViewModel
    {
        public Product Entity { get; }
        public ObservableCollection<Manufacturer> Manufacturers { get; }

        public ProductEditorViewModel(Product entity, ObservableCollection<Manufacturer> manufacturers)
        {
            Entity = entity;
            Manufacturers = manufacturers;
            if (Entity.ProductionDate == default)
                Entity.ProductionDate = DateTime.UtcNow;
            if (Entity.ExpirationDate == default)
                Entity.ExpirationDate = DateTime.UtcNow.AddYears(2);
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
}