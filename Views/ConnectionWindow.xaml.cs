using System;
using System.Windows;
using MyHippocrates.Data;
using MyHippocrates.ViewModels;

namespace MyHippocrates
{
    public partial class ConnectionWindow : Window
    {
        public ConnectionWindow()
        {
            InitializeComponent();
        }

        private void ConnectButton_Click(object sender, RoutedEventArgs e)
        {
            TxtError.Visibility = Visibility.Collapsed;

            var cs = $"Host={TxtHost.Text};Port={TxtPort.Text};" +
                     $"Database={TxtDatabase.Text};" +
                     $"Username={TxtUser.Text};Password=root";
            try
            {
                var ctx = new AppDbContext(cs);
                if (!ctx.Database.CanConnect())
                {
                    ShowError("Не удалось подключиться. Проверьте параметры.");
                    return;
                }
                var vm = new MainViewModel(ctx);
                var main = new MainWindow { DataContext = vm };
                main.Show();
                Close();
            }
            catch (Exception ex)
            {
                ShowError("Ошибка: " + (ex.InnerException?.Message ?? ex.Message));
            }
        }

        private void ShowError(string msg)
        {
            TxtError.Text = msg;
            TxtError.Visibility = Visibility.Visible;
        }
    }
}