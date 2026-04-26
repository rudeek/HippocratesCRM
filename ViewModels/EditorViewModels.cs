using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Collections.ObjectModel;
using MyHippocrates.Models;

namespace MyHippocrates.ViewModels
{
    public class ProductEditorViewModel
    {
        public Product Entity { get;}
        public ObservableCollection<Manufacturer> Manufacturers { get; }

        public ProductEditorViewModel(Product entity, ObservableCollection<Manufacturer> manufacturers)
        {
            Entity = entity;
            Manufacturers = manufacturers;
            if(Entity.ProductionDate == default)
                Entity.ProductionDate = DateTime.Today;
            if (Entity.ExpirationDate == default)
                Entity.ExpirationDate = DateTime.Today.AddYears(2);
        }
    }

    public class ReceiptEditorViewModel
    {
        public Receipt Entity { get; }
        public ObservableCollection<Pharmacy> Pharmacies { get; }
        public ObservableCollection<Employee> Employees { get; }

        public ReceiptEditorViewModel(Receipt entity, ObservableCollection<Pharmacy> pharmacies, ObservableCollection<Employee> employees)
        {
            Entity = entity;
            Pharmacies = pharmacies;
            Employees = employees;
        }
    }

    public class OrderItemEditorViewModel
    {
        public OrderItem Entity { get; }
        public ObservableCollection<Receipt> Receipts { get; }
        public ObservableCollection<Product> Products { get; }

        public OrderItemEditorViewModel(OrderItem entity, ObservableCollection<Receipt> receipts, ObservableCollection<Product> products)
        {
            Entity = entity;
            Receipts = receipts;
            Products = products;
        }
    }

    public class StockBalanceEditorViewModel
    {
        public StockBalance Entity { get; }
        public ObservableCollection<Pharmacy> Pharmacies { get; }
        public ObservableCollection<Product> Products { get; }

        public StockBalanceEditorViewModel(StockBalance entity, ObservableCollection<Pharmacy> pharmacies, ObservableCollection<Product> products)
        {
            Entity = entity;
            Pharmacies = pharmacies;
            Products = products;
        }
    }
}