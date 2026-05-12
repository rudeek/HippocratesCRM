using MyHippocrates.ViewModels;
using System.Windows;
using System.Windows.Controls;
using MyHippocrates.Views;
using MyHippocrates.Data;

namespace MyHippocrates
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
        }

        private void OpenDashboard(object sender, RoutedEventArgs e)
        {
            var vm = (MainViewModel)DataContext;

            var w = new DashboardWindow(vm.Db);

            w.Owner = this;
            w.Show();
        }

        private void OpenReports_Click(object sender, RoutedEventArgs e)
        {
            var vm = (MainViewModel)DataContext;

            var reportsWindow = new ReportsWindow(vm.Db) // ВАЖНО: передаём DbContext
            {
                Owner = this
            };

            reportsWindow.ShowDialog();
        }

        // Нумерация строк в DataGrid через Header
        private void DataGrid_LoadingRow(object sender, DataGridRowEventArgs e)
        {
            e.Row.Header = (e.Row.GetIndex() + 1).ToString();
        }

        private void DataGrid_UnloadingRow(object sender, DataGridRowEventArgs e)
        {
            if (sender is not DataGrid dg) return;
            foreach (var item in dg.Items)
            {
                var row = dg.ItemContainerGenerator.ContainerFromItem(item) as DataGridRow;
                if (row != null)
                    row.Header = (row.GetIndex() + 1).ToString();
            }
        }

        private int _lastTabIndex = -1;

        private void TabControl_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (DataContext is not MainViewModel vm) return;
            if (sender is not TabControl tc) return;

            // Срабатывает только при смене вкладки
            if (tc.SelectedIndex == _lastTabIndex) return;
            _lastTabIndex = tc.SelectedIndex;

            switch (tc.SelectedIndex)
            {
                case 0: vm.ManufacturersVM.Reload(); break;
                case 1: vm.ProductsVM.Reload(); break;
                case 2: vm.PharmaciesVM.Reload(); break;
                case 3: vm.EmployeesVM.Reload(); break;
                case 4: vm.ReceiptsVM.Reload(); break;
                case 5: vm.OrderItemsVM.Reload(); break;
                case 6: vm.StockBalanceVM.Reload(); break;
                case 7: vm.CategoryVM.Reload(); break;
                case 8: vm.RoleVM.Reload(); break;
                case 9: vm.SystemUserVM.Reload(); break;
            }
        }
    }
}