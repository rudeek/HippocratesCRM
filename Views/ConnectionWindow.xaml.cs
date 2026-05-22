using Microsoft.EntityFrameworkCore;
using MyHippocrates.Data;
using MyHippocrates.ViewModels;
using MyHippocrates.Views;
using System;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.IO;
using System.Text.Json;
using System.Windows;

namespace MyHippocrates
{
    public partial class ConnectionWindow : Window
    {
        private const string DefaultConnection =
     "Host=localhost;Port=5432;Database=Hippocrates;Username=postgres;Password=root";

        private static string GetConnectionString()
        {
            var path = Path.Combine(
                AppDomain.CurrentDomain.BaseDirectory,
                "appsettings.json");

            if (!File.Exists(path))
                return DefaultConnection;

            try
            {
                var json = File.ReadAllText(path);
                var doc = JsonDocument.Parse(json);
                return doc.RootElement
                          .GetProperty("ConnectionString")
                          .GetString() ?? DefaultConnection;
            }
            catch
            {
                return DefaultConnection;
            }
        }

        public ConnectionWindow()
        {
            InitializeComponent();
        }

        private void ConnectButton_Click(object sender, RoutedEventArgs e)
        {
            ErrorPanel.Visibility = Visibility.Collapsed;

            var login = TxtLogin.Text.Trim();
            var password = PwdBox.Password;

            if (string.IsNullOrWhiteSpace(login) || string.IsNullOrWhiteSpace(password))
            {
                ShowError("Введите логин и пароль.");
                return;
            }

            try
            {
                var ctx = new AppDbContext(GetConnectionString());
                if (!ctx.Database.CanConnect())
                {
                    ShowError("Не удалось подключиться к базе данных.");
                    return;
                }

                var passwordHash = Md5Hash(password);
                var user = ctx.SystemUsers
                    .Include(u => u.Employee)
                    .ThenInclude(e => e!.Role)
                    .Include(u => u.Employee)       
                    .ThenInclude(e => e!.Pharmacy)
                    .FirstOrDefault(u => u.Login == login &&
                                         u.PasswordHash == passwordHash &&
                                         u.IsActive);

                if (user == null)
                {
                    ShowError("Неверный логин или пароль.");
                    return;
                }

                Window nextWindow;
                if (string.Equals(user.SystemRole, "admin", StringComparison.OrdinalIgnoreCase))
                {
                    nextWindow = new MainWindow { DataContext = new MainViewModel(ctx) };
                }
                else
                {
                    nextWindow = new CashierWindow(ctx, user);
                }

                Application.Current.MainWindow = nextWindow;
                nextWindow.Show();
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
            ErrorPanel.Visibility = Visibility.Visible;
        }

        private static string Md5Hash(string input)
        {
            using var md5 = MD5.Create();
            var bytes = md5.ComputeHash(Encoding.UTF8.GetBytes(input));
            return string.Concat(bytes.Select(b => b.ToString("x2")));
        }
    }
}
