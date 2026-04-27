using MyHippocrates.Data;
using MyHippocrates.Models;
using System.Collections.ObjectModel;
using System.Linq;

namespace MyHippocrates.ViewModels
{
    internal class MainViewModel : BaseViewModel
    {
        public AppDbContext Db { get; }

        // Общие справочники — одна коллекция на всё приложение.
        // Когда добавляешь аптеку в PharmaciesVM — она сразу появится в ComboBox у Receipts.
        public ObservableCollection<Manufacturer> Manufacturers { get; } = new();
        public ObservableCollection<Pharmacy> Pharmacies { get; } = new();
        public ObservableCollection<Employee> Employees { get; } = new();
        public ObservableCollection<Product> Products { get; } = new();
        public ObservableCollection<Receipt> Receipts { get; } = new();

        public ManufacturersViewModel ManufacturersVM { get; }
        public ProductsViewModel ProductsVM { get; }
        public PharmaciesViewModel PharmaciesVM { get; }
        public EmployeesViewModel EmployeesVM { get; }
        public ReceiptsViewModel ReceiptsVM { get; }
        public OrderItemsViewModel OrderItemsVM { get; }
        public StockBalanceViewModel StockBalanceVM { get; }

        public MainViewModel(AppDbContext dbContext)
        {
            Db = dbContext;
            // Загружаем общие справочники один раз
            foreach (var m in dbContext.Manufacturers.OrderBy(x => x.Id).ToList()) Manufacturers.Add(m);
            foreach (var p in dbContext.Pharmacies.OrderBy(x => x.Id).ToList()) Pharmacies.Add(p);
            foreach (var e in dbContext.Employees.OrderBy(x => x.Id).ToList()) Employees.Add(e);
            foreach (var p in dbContext.Products.OrderBy(x => x.Id).ToList()) Products.Add(p);
            foreach (var r in dbContext.Receipts.OrderBy(x => x.Id).ToList()) Receipts.Add(r);

            // Передаём общие коллекции в VM
            ManufacturersVM = new ManufacturersViewModel(dbContext, Manufacturers);
            ProductsVM = new ProductsViewModel(dbContext, Manufacturers, Products);
            PharmaciesVM = new PharmaciesViewModel(dbContext, Pharmacies);
            EmployeesVM = new EmployeesViewModel(dbContext, Employees);
            ReceiptsVM = new ReceiptsViewModel(dbContext, Pharmacies, Employees, Receipts);
            OrderItemsVM = new OrderItemsViewModel(dbContext, Receipts, Products);
            StockBalanceVM = new StockBalanceViewModel(dbContext, Pharmacies, Products);
        }
    }
}