using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Wpf.Ui.Controls;

namespace DesignSystem.Controls
{
    public partial class KpiCard : UserControl
    {
        public static readonly DependencyProperty TitleProperty =
            DependencyProperty.Register(nameof(Title), typeof(string), typeof(KpiCard), new PropertyMetadata("METRIC"));

        public static readonly DependencyProperty ValueProperty =
            DependencyProperty.Register(nameof(Value), typeof(string), typeof(KpiCard), new PropertyMetadata("0"));

        public static readonly DependencyProperty DescriptionProperty =
            DependencyProperty.Register(nameof(Description), typeof(string), typeof(KpiCard), new PropertyMetadata(string.Empty));

        public static readonly DependencyProperty IconProperty =
            DependencyProperty.Register(nameof(Icon), typeof(SymbolRegular), typeof(KpiCard), new PropertyMetadata(SymbolRegular.Sparkle24));

        public static readonly DependencyProperty BadgeBackgroundProperty =
            DependencyProperty.Register(nameof(BadgeBackground), typeof(Brush), typeof(KpiCard), new PropertyMetadata(null));

        public static readonly DependencyProperty BadgeForegroundProperty =
            DependencyProperty.Register(nameof(BadgeForeground), typeof(Brush), typeof(KpiCard), new PropertyMetadata(null));

        public KpiCard()
        {
            InitializeComponent();
            BadgeBackground = new SolidColorBrush(Color.FromArgb(24, 37, 99, 235)); // Soft tint
            BadgeForeground = new SolidColorBrush(Color.FromRgb(37, 99, 235)); // Royal Blue
        }

        public string Title
        {
            get => (string)GetValue(TitleProperty);
            set => SetValue(TitleProperty, value);
        }

        public string Value
        {
            get => (string)GetValue(ValueProperty);
            set => SetValue(ValueProperty, value);
        }

        public string Description
        {
            get => (string)GetValue(DescriptionProperty);
            set => SetValue(DescriptionProperty, value);
        }

        public SymbolRegular Icon
        {
            get => (SymbolRegular)GetValue(IconProperty);
            set => SetValue(IconProperty, value);
        }

        public Brush BadgeBackground
        {
            get => (Brush)GetValue(BadgeBackgroundProperty);
            set => SetValue(BadgeBackgroundProperty, value);
        }

        public Brush BadgeForeground
        {
            get => (Brush)GetValue(BadgeForegroundProperty);
            set => SetValue(BadgeForegroundProperty, value);
        }
    }
}
