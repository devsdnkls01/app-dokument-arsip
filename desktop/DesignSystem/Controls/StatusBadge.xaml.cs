using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace DesignSystem.Controls
{
    public enum BadgeVariant
    {
        Success,
        Warning,
        Danger,
        Info,
        Neutral
    }

    public partial class StatusBadge : UserControl
    {
        public static readonly DependencyProperty TextProperty =
            DependencyProperty.Register(nameof(Text), typeof(string), typeof(StatusBadge), new PropertyMetadata("Status", OnStateChanged));

        public static readonly DependencyProperty VariantProperty =
            DependencyProperty.Register(nameof(Variant), typeof(BadgeVariant), typeof(StatusBadge), new PropertyMetadata(BadgeVariant.Neutral, OnStateChanged));

        public static readonly DependencyProperty CurrentBackgroundProperty =
            DependencyProperty.Register(nameof(CurrentBackground), typeof(Brush), typeof(StatusBadge), new PropertyMetadata(null));

        public static readonly DependencyProperty CurrentForegroundProperty =
            DependencyProperty.Register(nameof(CurrentForeground), typeof(Brush), typeof(StatusBadge), new PropertyMetadata(null));

        public static readonly DependencyProperty CurrentBorderBrushProperty =
            DependencyProperty.Register(nameof(CurrentBorderBrush), typeof(Brush), typeof(StatusBadge), new PropertyMetadata(null));

        public static readonly DependencyProperty CurrentDotBrushProperty =
            DependencyProperty.Register(nameof(CurrentDotBrush), typeof(Brush), typeof(StatusBadge), new PropertyMetadata(null));

        public StatusBadge()
        {
            InitializeComponent();
            UpdateVisuals();
        }

        public string Text
        {
            get => (string)GetValue(TextProperty);
            set => SetValue(TextProperty, value);
        }

        public BadgeVariant Variant
        {
            get => (BadgeVariant)GetValue(VariantProperty);
            set => SetValue(VariantProperty, value);
        }

        public Brush CurrentBackground
        {
            get => (Brush)GetValue(CurrentBackgroundProperty);
            set => SetValue(CurrentBackgroundProperty, value);
        }

        public Brush CurrentForeground
        {
            get => (Brush)GetValue(CurrentForegroundProperty);
            set => SetValue(CurrentForegroundProperty, value);
        }

        public Brush CurrentBorderBrush
        {
            get => (Brush)GetValue(CurrentBorderBrushProperty);
            set => SetValue(CurrentBorderBrushProperty, value);
        }

        public Brush CurrentDotBrush
        {
            get => (Brush)GetValue(CurrentDotBrushProperty);
            set => SetValue(CurrentDotBrushProperty, value);
        }

        private static void OnStateChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is StatusBadge badge)
            {
                badge.UpdateVisuals();
            }
        }

        public void UpdateVisuals()
        {
            switch (Variant)
            {
                case BadgeVariant.Success:
                    CurrentBackground = (Brush)FindResource("StatusSuccessBgBrush");
                    CurrentForeground = (Brush)FindResource("StatusSuccessFgBrush");
                    CurrentBorderBrush = (Brush)FindResource("StatusSuccessBorderBrush");
                    CurrentDotBrush = (Brush)FindResource("StatusSuccessDotBrush");
                    break;
                case BadgeVariant.Warning:
                    CurrentBackground = (Brush)FindResource("StatusWarningBgBrush");
                    CurrentForeground = (Brush)FindResource("StatusWarningFgBrush");
                    CurrentBorderBrush = (Brush)FindResource("StatusWarningBorderBrush");
                    CurrentDotBrush = (Brush)FindResource("StatusWarningDotBrush");
                    break;
                case BadgeVariant.Danger:
                    CurrentBackground = (Brush)FindResource("StatusDangerBgBrush");
                    CurrentForeground = (Brush)FindResource("StatusDangerFgBrush");
                    CurrentBorderBrush = (Brush)FindResource("StatusDangerBorderBrush");
                    CurrentDotBrush = (Brush)FindResource("StatusDangerDotBrush");
                    break;
                case BadgeVariant.Info:
                    CurrentBackground = (Brush)FindResource("StatusInfoBgBrush");
                    CurrentForeground = (Brush)FindResource("StatusInfoFgBrush");
                    CurrentBorderBrush = (Brush)FindResource("StatusInfoBorderBrush");
                    CurrentDotBrush = (Brush)FindResource("StatusInfoDotBrush");
                    break;
                case BadgeVariant.Neutral:
                default:
                    CurrentBackground = (Brush)FindResource("StatusNeutralBgBrush");
                    CurrentForeground = (Brush)FindResource("StatusNeutralFgBrush");
                    CurrentBorderBrush = (Brush)FindResource("StatusNeutralBorderBrush");
                    CurrentDotBrush = (Brush)FindResource("StatusNeutralDotBrush");
                    break;
            }
        }
    }
}
