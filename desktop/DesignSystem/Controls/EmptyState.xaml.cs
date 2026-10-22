using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Wpf.Ui.Controls;

namespace DesignSystem.Controls
{
    public partial class EmptyState : UserControl
    {
        public static readonly DependencyProperty TitleProperty =
            DependencyProperty.Register(nameof(Title), typeof(string), typeof(EmptyState), new PropertyMetadata("Tidak Ada Data"));

        public static readonly DependencyProperty MessageProperty =
            DependencyProperty.Register(nameof(Message), typeof(string), typeof(EmptyState), new PropertyMetadata("Data belum tersedia atau tidak cocok dengan filter pencarian."));

        public static readonly DependencyProperty IconProperty =
            DependencyProperty.Register(nameof(Icon), typeof(SymbolRegular), typeof(EmptyState), new PropertyMetadata(SymbolRegular.DocumentSearch24));

        public static readonly DependencyProperty ActionTextProperty =
            DependencyProperty.Register(nameof(ActionText), typeof(string), typeof(EmptyState), new PropertyMetadata(string.Empty, OnActionTextChanged));

        public static readonly DependencyProperty ActionCommandProperty =
            DependencyProperty.Register(nameof(ActionCommand), typeof(ICommand), typeof(EmptyState), new PropertyMetadata(null));

        public static readonly DependencyProperty HasActionProperty =
            DependencyProperty.Register(nameof(HasAction), typeof(bool), typeof(EmptyState), new PropertyMetadata(false));

        public EmptyState()
        {
            InitializeComponent();
        }

        public string Title
        {
            get => (string)GetValue(TitleProperty);
            set => SetValue(TitleProperty, value);
        }

        public string Message
        {
            get => (string)GetValue(MessageProperty);
            set => SetValue(MessageProperty, value);
        }

        public SymbolRegular Icon
        {
            get => (SymbolRegular)GetValue(IconProperty);
            set => SetValue(IconProperty, value);
        }

        public string ActionText
        {
            get => (string)GetValue(ActionTextProperty);
            set => SetValue(ActionTextProperty, value);
        }

        public ICommand ActionCommand
        {
            get => (ICommand)GetValue(ActionCommandProperty);
            set => SetValue(ActionCommandProperty, value);
        }

        public bool HasAction
        {
            get => (bool)GetValue(HasActionProperty);
            set => SetValue(HasActionProperty, value);
        }

        private static void OnActionTextChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is EmptyState empty)
            {
                empty.HasAction = !string.IsNullOrWhiteSpace(empty.ActionText);
            }
        }
    }
}
