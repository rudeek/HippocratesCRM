using System.Windows;
using MyHippocrates.Models;

namespace MyHippocrates.Views
{
    public partial class ProductDetailsWindow : Window
    {
        public Product Product { get; }
        public int RemainingQty { get; }
        public string RemainingText => $"{RemainingQty} {Product.Unit}";
        public string PrescriptionText => Product.PrescriptionRequired ? "Требуется" : "Не требуется";
        public string DescriptionText => string.IsNullOrWhiteSpace(Product.Description)
            ? "Описание не указано."
            : Product.Description;

        public ProductDetailsWindow(Product product, int remainingQty)
        {
            InitializeComponent();
            Product = product;
            RemainingQty = remainingQty;
            DataContext = this;
        }

        private void Close_Click(object sender, RoutedEventArgs e) => Close();
    }
}
