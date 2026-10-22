using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace ClientApp.Services
{
    public static class ModernDialogService
    {
        public static string Prompt(string promptText, string title = "Input Data", string defaultResponse = "")
        {
            var win = new Window
            {
                Title = title,
                Width = 440,
                Height = 220,
                WindowStartupLocation = WindowStartupLocation.CenterScreen,
                ResizeMode = ResizeMode.NoResize,
                Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#F8FAFC")),
                FontFamily = new FontFamily("Segoe UI Variable Text, Segoe UI, Arial")
            };

            var grid = new Grid { Margin = new Thickness(20) };
            grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            grid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });

            var lbl = new TextBlock
            {
                Text = promptText,
                FontSize = 13,
                FontWeight = FontWeights.SemiBold,
                Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#0F172A")),
                TextWrapping = TextWrapping.Wrap,
                Margin = new Thickness(0, 0, 0, 10)
            };
            Grid.SetRow(lbl, 0);

            var txt = new TextBox
            {
                Text = defaultResponse,
                Height = 36,
                Padding = new Thickness(8, 4, 8, 4),
                FontSize = 13,
                Margin = new Thickness(0, 0, 0, 16)
            };
            Grid.SetRow(txt, 1);

            var sp = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                HorizontalAlignment = HorizontalAlignment.Right,
                VerticalAlignment = VerticalAlignment.Bottom
            };
            Grid.SetRow(sp, 2);

            var btnCancel = new Button
            {
                Content = "Batal",
                Height = 34,
                Width = 90,
                Margin = new Thickness(0, 0, 8, 0)
            };
            btnCancel.Click += (s, e) => { win.DialogResult = false; win.Close(); };

            var btnOk = new Button
            {
                Content = "Simpan",
                Height = 34,
                Width = 90,
                Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#1E40AF")),
                Foreground = Brushes.White,
                FontWeight = FontWeights.Bold
            };
            btnOk.Click += (s, e) => { win.DialogResult = true; win.Close(); };

            sp.Children.Add(btnCancel);
            sp.Children.Add(btnOk);

            grid.Children.Add(lbl);
            grid.Children.Add(txt);
            grid.Children.Add(sp);

            win.Content = grid;
            txt.Focus();
            txt.SelectAll();

            var result = win.ShowDialog();
            return result == true ? txt.Text.Trim() : string.Empty;
        }
    }
}
