using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Wpf.Ui.Controls;

namespace DesignSystem.Controls
{
    public partial class PageHeader : UserControl
    {
        public static readonly DependencyProperty TitleProperty =
            DependencyProperty.Register(nameof(Title), typeof(string), typeof(PageHeader), new PropertyMetadata("Judul Halaman"));

        public static readonly DependencyProperty SubtitleProperty =
            DependencyProperty.Register(nameof(Subtitle), typeof(string), typeof(PageHeader), new PropertyMetadata(string.Empty));

        public static readonly DependencyProperty PrimaryActionTextProperty =
            DependencyProperty.Register(nameof(PrimaryActionText), typeof(string), typeof(PageHeader), new PropertyMetadata(string.Empty, OnActionVisibilityChanged));

        public static readonly DependencyProperty PrimaryActionCommandProperty =
            DependencyProperty.Register(nameof(PrimaryActionCommand), typeof(ICommand), typeof(PageHeader), new PropertyMetadata(null));

        public static readonly DependencyProperty PrimaryActionIconProperty =
            DependencyProperty.Register(nameof(PrimaryActionIcon), typeof(SymbolRegular), typeof(PageHeader), new PropertyMetadata(SymbolRegular.Add24));

        public static readonly DependencyProperty SecondaryActionTextProperty =
            DependencyProperty.Register(nameof(SecondaryActionText), typeof(string), typeof(PageHeader), new PropertyMetadata(string.Empty, OnActionVisibilityChanged));

        public static readonly DependencyProperty SecondaryActionCommandProperty =
            DependencyProperty.Register(nameof(SecondaryActionCommand), typeof(ICommand), typeof(PageHeader), new PropertyMetadata(null));

        public static readonly DependencyProperty SecondaryActionIconProperty =
            DependencyProperty.Register(nameof(SecondaryActionIcon), typeof(SymbolRegular), typeof(PageHeader), new PropertyMetadata(SymbolRegular.ArrowSync24));

        public static readonly DependencyProperty IsLoadingProperty =
            DependencyProperty.Register(nameof(IsLoading), typeof(bool), typeof(PageHeader), new PropertyMetadata(false));

        public static readonly DependencyProperty ExtraActionsProperty =
            DependencyProperty.Register(nameof(ExtraActions), typeof(object), typeof(PageHeader), new PropertyMetadata(null));

        public static readonly DependencyProperty HasPrimaryActionProperty =
            DependencyProperty.Register(nameof(HasPrimaryAction), typeof(bool), typeof(PageHeader), new PropertyMetadata(false));

        public static readonly DependencyProperty HasSecondaryActionProperty =
            DependencyProperty.Register(nameof(HasSecondaryAction), typeof(bool), typeof(PageHeader), new PropertyMetadata(false));

        public PageHeader()
        {
            InitializeComponent();
        }

        public string Title
        {
            get => (string)GetValue(TitleProperty);
            set => SetValue(TitleProperty, value);
        }

        public string Subtitle
        {
            get => (string)GetValue(SubtitleProperty);
            set => SetValue(SubtitleProperty, value);
        }

        public string PrimaryActionText
        {
            get => (string)GetValue(PrimaryActionTextProperty);
            set => SetValue(PrimaryActionTextProperty, value);
        }

        public ICommand PrimaryActionCommand
        {
            get => (ICommand)GetValue(PrimaryActionCommandProperty);
            set => SetValue(PrimaryActionCommandProperty, value);
        }

        public SymbolRegular PrimaryActionIcon
        {
            get => (SymbolRegular)GetValue(PrimaryActionIconProperty);
            set => SetValue(PrimaryActionIconProperty, value);
        }

        public string SecondaryActionText
        {
            get => (string)GetValue(SecondaryActionTextProperty);
            set => SetValue(SecondaryActionTextProperty, value);
        }

        public ICommand SecondaryActionCommand
        {
            get => (ICommand)GetValue(SecondaryActionCommandProperty);
            set => SetValue(SecondaryActionCommandProperty, value);
        }

        public SymbolRegular SecondaryActionIcon
        {
            get => (SymbolRegular)GetValue(SecondaryActionIconProperty);
            set => SetValue(SecondaryActionIconProperty, value);
        }

        public bool IsLoading
        {
            get => (bool)GetValue(IsLoadingProperty);
            set => SetValue(IsLoadingProperty, value);
        }

        public object ExtraActions
        {
            get => GetValue(ExtraActionsProperty);
            set => SetValue(ExtraActionsProperty, value);
        }

        public bool HasPrimaryAction
        {
            get => (bool)GetValue(HasPrimaryActionProperty);
            set => SetValue(HasPrimaryActionProperty, value);
        }

        public bool HasSecondaryAction
        {
            get => (bool)GetValue(HasSecondaryActionProperty);
            set => SetValue(HasSecondaryActionProperty, value);
        }

        private static void OnActionVisibilityChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is PageHeader header)
            {
                header.HasPrimaryAction = !string.IsNullOrWhiteSpace(header.PrimaryActionText);
                header.HasSecondaryAction = !string.IsNullOrWhiteSpace(header.SecondaryActionText);
            }
        }
    }
}
