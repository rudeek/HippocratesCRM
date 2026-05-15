using System;
using System.IO;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using MyHippocrates.ViewModels;

namespace MyHippocrates
{
    public partial class App : Application
    {
        private void OnlyLetters_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            e.Handled = !e.Text.All(char.IsLetter);
        }

        private void PhoneNumber_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            var textBox = (TextBox)sender;
            if (e.Text == "+" && textBox.Text.Contains("+"))
            {
                e.Handled = true;
                return;
            }
            if (textBox.Text.Length >= 15)
            {
                e.Handled = true;
                return;
            }
            e.Handled = !e.Text.All(c => char.IsDigit(c) || c == '+');
        }

        private void OnlyDigits_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            e.Handled = !e.Text.All(char.IsDigit);
        }

        private void Price_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            var tb = (TextBox)sender;
            var input = e.Text;

            if (input == ".")
            {
                if (tb.Text.Contains("."))
                {
                    e.Handled = true;
                    return;
                }
                e.Handled = true;
                var selStart = tb.SelectionStart;
                tb.Text = tb.Text.Insert(selStart, ".");
                tb.SelectionStart = selStart + 1;
                return;
            }

            if (!input.All(c => char.IsDigit(c) || c.ToString() == "."))
            {
                e.Handled = true;
                return;
            }
            e.Handled = !e.Text.All(c => char.IsDigit(c) || c == '.');
        }

        private void Time_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            e.Handled = !Regex.IsMatch(e.Text, "[0-9:]");
        }

        private void BrowseImage_Click(object sender, RoutedEventArgs e)
        {
            var defaultDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Images");

            var dlg = new Microsoft.Win32.OpenFileDialog
            {
                Title = "Выберите изображение",
                Filter = "Изображения|*.jpg;*.jpeg;*.png;*.bmp;*.gif|Все файлы|*.*",
                InitialDirectory = Directory.Exists(defaultDir)
                    ? defaultDir
                    : Environment.GetFolderPath(Environment.SpecialFolder.MyPictures)
            };

            if (dlg.ShowDialog() != true) return;

            if ((sender as FrameworkElement)?.Tag is not ProductEditorViewModel vm) return;

            // Сохраняем относительный путь если файл внутри папки приложения
            var relativePath = Path.GetRelativePath(AppDomain.CurrentDomain.BaseDirectory, dlg.FileName);
            vm.FilePath = relativePath.StartsWith("..") ? dlg.FileName : relativePath;
        }
    }
}