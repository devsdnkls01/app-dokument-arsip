using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media;

namespace DesignSystem.Converters
{
    public class NullOrEmptyToPlaceholderConverter : IValueConverter
    {
        public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (value == null || (value is string str && string.IsNullOrWhiteSpace(str)))
            {
                return parameter as string ?? "Belum terhubung";
            }
            return value;
        }

        public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) => throw new NotImplementedException();
    }

    public class NullOrEmptyToDimmedBrushConverter : IValueConverter
    {
        public Brush NormalBrush { get; set; } = new SolidColorBrush(Color.FromRgb(15, 23, 42)); // Slate 900
        public Brush DimmedBrush { get; set; } = new SolidColorBrush(Color.FromRgb(148, 163, 184)); // Slate 400

        public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (value == null || (value is string str && string.IsNullOrWhiteSpace(str)))
            {
                return DimmedBrush;
            }
            return NormalBrush;
        }

        public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) => throw new NotImplementedException();
    }

    public class InverseBooleanToVisibilityConverter : IValueConverter
    {
        public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            bool val = value is bool b && b;
            return val ? Visibility.Collapsed : Visibility.Visible;
        }

        public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) => throw new NotImplementedException();
    }

    public class BooleanToVisibilityConverter : IValueConverter
    {
        public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            bool val = value is bool b && b;
            return val ? Visibility.Visible : Visibility.Collapsed;
        }

        public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) => throw new NotImplementedException();
    }

    public class StringNullOrEmptyToVisibilityConverter : IValueConverter
    {
        public bool Invert { get; set; }

        public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            bool isEmpty = value == null || (value is string s && string.IsNullOrWhiteSpace(s));
            if (Invert) isEmpty = !isEmpty;
            return isEmpty ? Visibility.Collapsed : Visibility.Visible;
        }

        public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) => throw new NotImplementedException();
    }
}
