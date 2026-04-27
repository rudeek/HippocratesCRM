using System.Windows;
using MyHippocrates.Data;
using MyHippocrates.ViewModels;

namespace MyHippocrates.Views
{
    public partial class DashboardWindow : Window
    {
        public DashboardWindow(AppDbContext ctx)
        {
            InitializeComponent();
            DataContext = new DashboardViewModel(ctx);
        }
    }
}