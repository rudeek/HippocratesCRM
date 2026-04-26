using System.Configuration;
using System.Data;
using System.Text.RegularExpressions;
using System.Windows;
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
            e.Handled = !e.Text.All(c => char.IsDigit(c) || c == ' ' || c == '+');
        }

        private void OnlyDigits_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            e.Handled = !e.Text.All(char.IsDigit);
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
