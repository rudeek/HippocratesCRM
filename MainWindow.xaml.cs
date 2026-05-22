using MyHippocrates.Data;
using MyHippocrates.ViewModels;
using MyHippocrates.Views;
using System.IO;
using System.Text.Json;
using System.Windows;
using System.Windows.Controls;

namespace MyHippocrates
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
        }

        private static readonly string BackupsFolder =
    Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Backups");

        private static readonly string PgBinPath =
            @"C:\Program Files\PostgreSQL\18\bin";

        private const string DbHost = "localhost";
        private const string DbPort = "5432";
        private const string DbName = "Hippocrates";
        private const string DbUser = "postgres";

        private static string GetDbPassword()
        {
            var path = Path.Combine(
                AppDomain.CurrentDomain.BaseDirectory,
                "appsettings.json");

            if (!File.Exists(path))
                return "root";

            try
            {
                var json = File.ReadAllText(path);
                var doc = JsonDocument.Parse(json);
                var cs = doc.RootElement
                            .GetProperty("ConnectionString")
                            .GetString() ?? "";

                // Парсим Password= из строки подключения
                foreach (var part in cs.Split(';'))
                {
                    var kv = part.Split('=', 2);
                    if (kv.Length == 2 &&
                        kv[0].Trim().Equals("Password",
                            StringComparison.OrdinalIgnoreCase))
                        return kv[1].Trim();
                }
            }
            catch { }

            return "root";
        }

        private void CreateBackup_Click(object sender, RoutedEventArgs e)
        {
            Directory.CreateDirectory(BackupsFolder);

            var dlg = new Microsoft.Win32.SaveFileDialog
            {
                Title = "Сохранить бэкап",
                Filter = "SQL бэкап (*.sql)|*.sql",
                DefaultExt = ".sql",
                FileName = $"Hippocrates_{DateTime.Now:yyyy-MM-dd_HH-mm-ss}.sql",
                InitialDirectory = BackupsFolder
            };

            if (dlg.ShowDialog() != true) return;

            try
            {
                var pgDump = Path.Combine(PgBinPath, "pg_dump.exe");
                if (!File.Exists(pgDump))
                {
                    MessageBox.Show(
                        $"pg_dump.exe не найден по пути:\n{pgDump}\n\nПроверьте путь к PostgreSQL.",
                        "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                var psi = new System.Diagnostics.ProcessStartInfo
                {
                    FileName = pgDump,
                    Arguments = $"-h {DbHost} -p {DbPort} -U {DbUser} -d {DbName} -F p -f \"{dlg.FileName}\"",
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    Environment = { ["PGPASSWORD"] = GetDbPassword() }
                };

                using var process = System.Diagnostics.Process.Start(psi)!;
                var stderr = process.StandardError.ReadToEnd();
                process.WaitForExit();

                if (process.ExitCode != 0)
                {
                    MessageBox.Show($"Ошибка при создании бэкапа:\n{stderr}",
                        "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                Views.ToastService.ShowSuccess($"Бэкап сохранён:\n{Path.GetFileName(dlg.FileName)}");
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void RestoreBackup_Click(object sender, RoutedEventArgs e)
        {
            Directory.CreateDirectory(BackupsFolder);

            var dlg = new Microsoft.Win32.OpenFileDialog
            {
                Title = "Выбрать файл бэкапа",
                Filter = "SQL бэкап (*.sql)|*.sql|Все файлы (*.*)|*.*",
                InitialDirectory = BackupsFolder
            };

            if (dlg.ShowDialog() != true) return;

            var confirm = MessageBox.Show(
                $"Восстановить базу данных из файла:\n{Path.GetFileName(dlg.FileName)}\n\n" +
                "Текущие данные будут заменены. Продолжить?",
                "Подтверждение восстановления",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning);

            if (confirm != MessageBoxResult.Yes) return;

            try
            {
                var psql = Path.Combine(PgBinPath, "psql.exe");
                if (!File.Exists(psql))
                {
                    MessageBox.Show(
                        $"psql.exe не найден по пути:\n{psql}\n\nПроверьте путь к PostgreSQL.",
                        "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                // Сначала очищаем БД, затем восстанавливаем
                var dropAndCreate = $"-h {DbHost} -p {DbPort} -U {DbUser} " +
                                    $"-c \"DROP SCHEMA public CASCADE; CREATE SCHEMA public;\" {DbName}";

                var psiDrop = new System.Diagnostics.ProcessStartInfo
                {
                    FileName = psql,
                    Arguments = dropAndCreate,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    Environment = { ["PGPASSWORD"] = GetDbPassword() }
                };

                using (var drop = System.Diagnostics.Process.Start(psiDrop)!)
                {
                    var err = drop.StandardError.ReadToEnd();
                    drop.WaitForExit();
                    if (drop.ExitCode != 0)
                    {
                        MessageBox.Show($"Ошибка при очистке БД:\n{err}",
                            "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                        return;
                    }
                }

                var psiRestore = new System.Diagnostics.ProcessStartInfo
                {
                    FileName = psql,
                    Arguments = $"-h {DbHost} -p {DbPort} -U {DbUser} -d {DbName} -f \"{dlg.FileName}\"",
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    Environment = { ["PGPASSWORD"] = GetDbPassword() }
                };

                using var restore = System.Diagnostics.Process.Start(psiRestore)!;
                var stderr = restore.StandardError.ReadToEnd();
                restore.WaitForExit();

                if (restore.ExitCode != 0)
                {
                    MessageBox.Show($"Ошибка при восстановлении:\n{stderr}",
                        "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                // Перезагружаем все данные в UI
                var vm = (MainViewModel)DataContext;
                vm.ManufacturersVM.Reload();
                vm.ProductsVM.Reload();
                vm.PharmaciesVM.Reload();
                vm.EmployeesVM.Reload();
                vm.ReceiptsVM.Reload();
                vm.OrderItemsVM.Reload();
                vm.StockBalanceVM.Reload();
                vm.CategoryVM.Reload();
                vm.RoleVM.Reload();
                vm.SystemUserVM.Reload();

                Views.ToastService.ShowSuccess("База данных успешно восстановлена.");
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
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
            var reportsWindow = new ReportsWindow(vm.Db)
            {
                Owner = this
            };
            reportsWindow.ShowDialog();
        }

        private void Logout_Click(object sender, RoutedEventArgs e)
        {
            var login = new ConnectionWindow();
            Application.Current.MainWindow = login;
            login.Show();
            Close();
        }

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