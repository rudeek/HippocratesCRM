using System.Configuration;
using System.Data;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

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
            if(e.Text == "+" && textBox.Text.Contains("+"))
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
            // Запрещаем всё кроме цифр и точки
            // И не даём ввести вторую точку
            if (e.Text == "." && tb.Text.Contains("."))
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

        private void TextBox_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {

        }
    }

}
