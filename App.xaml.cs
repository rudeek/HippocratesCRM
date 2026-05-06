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

            if(textBox.Text.Length >= 15)
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

            // If user types dot or comma, normalize to current-culture separator
            if (input == ".")
            {
                // block if separator already present
                if (tb.Text.Contains("."))
                {
                    e.Handled = true;
                    return;
                }

                // consume original input and insert culture separator at caret
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
    }

}
