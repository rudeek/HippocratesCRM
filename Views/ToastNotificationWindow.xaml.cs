using System;
using System.Windows;
using System.Windows.Media.Animation;
using System.Windows.Threading;

namespace MyHippocrates.Views
{
    public partial class ToastNotificationWindow : Window
    {
        public string Title { get; }
        public string Message { get; }

        public ToastNotificationWindow(string title, string message)
        {
            InitializeComponent();
            Title = title;
            Message = message;
            DataContext = this;
        }

        protected override void OnContentRendered(EventArgs e)
        {
            base.OnContentRendered(e);
            PositionBottomRight();
            BeginFadeIn();

            var timer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(2.4) };
            timer.Tick += (_, _) =>
            {
                timer.Stop();
                BeginFadeOut();
            };
            timer.Start();
        }

        private void PositionBottomRight()
        {
            var area = SystemParameters.WorkArea;
            Left = area.Right - Width - 22;
            Top = area.Bottom - Height - 22;
        }

        private void BeginFadeIn()
        {
            BeginAnimation(OpacityProperty, new DoubleAnimation(0, 1, TimeSpan.FromMilliseconds(180)));
        }

        private void BeginFadeOut()
        {
            var animation = new DoubleAnimation(1, 0, TimeSpan.FromMilliseconds(420));
            animation.Completed += (_, _) => Close();
            BeginAnimation(OpacityProperty, animation);
        }
    }

    public static class ToastService
    {
        public static void ShowSuccess(string message, string title = "Готово")
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                var toast = new ToastNotificationWindow(title, message);
                if (Application.Current.MainWindow?.IsVisible == true)
                    toast.Owner = Application.Current.MainWindow;

                toast.Show();
            });
        }
    }
}
