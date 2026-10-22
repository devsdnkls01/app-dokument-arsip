using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Wpf.Ui.Controls;

namespace DesignSystem.Controls
{
    public partial class TierChip : UserControl
    {
        public static readonly DependencyProperty TierIdProperty =
            DependencyProperty.Register(nameof(TierId), typeof(string), typeof(TierChip), new PropertyMetadata("standard", OnTierChanged));

        public static readonly DependencyProperty DisplayNameProperty =
            DependencyProperty.Register(nameof(DisplayName), typeof(string), typeof(TierChip), new PropertyMetadata("Standard"));

        public static readonly DependencyProperty CurrentBackgroundProperty =
            DependencyProperty.Register(nameof(CurrentBackground), typeof(Brush), typeof(TierChip), new PropertyMetadata(null));

        public static readonly DependencyProperty CurrentForegroundProperty =
            DependencyProperty.Register(nameof(CurrentForeground), typeof(Brush), typeof(TierChip), new PropertyMetadata(null));

        public static readonly DependencyProperty CurrentBorderBrushProperty =
            DependencyProperty.Register(nameof(CurrentBorderBrush), typeof(Brush), typeof(TierChip), new PropertyMetadata(null));

        public static readonly DependencyProperty CurrentIconProperty =
            DependencyProperty.Register(nameof(CurrentIcon), typeof(SymbolRegular), typeof(TierChip), new PropertyMetadata(SymbolRegular.Shield24));

        public TierChip()
        {
            InitializeComponent();
            UpdateVisuals();
        }

        public string TierId
        {
            get => (string)GetValue(TierIdProperty);
            set => SetValue(TierIdProperty, value);
        }

        public string DisplayName
        {
            get => (string)GetValue(DisplayNameProperty);
            set => SetValue(DisplayNameProperty, value);
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

        public SymbolRegular CurrentIcon
        {
            get => (SymbolRegular)GetValue(CurrentIconProperty);
            set => SetValue(CurrentIconProperty, value);
        }

        private static void OnTierChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is TierChip chip)
            {
                chip.UpdateVisuals();
            }
        }

        public void UpdateVisuals()
        {
            string t = (TierId ?? "").Trim().ToLowerInvariant();
            switch (t)
            {
                case "trial":
                    DisplayName = "TRIAL";
                    CurrentBackground = (Brush)FindResource("TierTrialBgBrush");
                    CurrentForeground = (Brush)FindResource("TierTrialFgBrush");
                    CurrentBorderBrush = (Brush)FindResource("TierTrialBorderBrush");
                    CurrentIcon = SymbolRegular.Clock24;
                    break;
                case "basic":
                    DisplayName = "BASIC";
                    CurrentBackground = (Brush)FindResource("TierBasicBgBrush");
                    CurrentForeground = (Brush)FindResource("TierBasicFgBrush");
                    CurrentBorderBrush = (Brush)FindResource("TierBasicBorderBrush");
                    CurrentIcon = SymbolRegular.CheckmarkCircle24;
                    break;
                case "standard":
                    DisplayName = "STANDARD";
                    CurrentBackground = (Brush)FindResource("TierStandardBgBrush");
                    CurrentForeground = (Brush)FindResource("TierStandardFgBrush");
                    CurrentBorderBrush = (Brush)FindResource("TierStandardBorderBrush");
                    CurrentIcon = SymbolRegular.Ribbon24;
                    break;
                case "professional":
                case "pro":
                    DisplayName = "PROFESSIONAL";
                    CurrentBackground = (Brush)FindResource("TierProBgBrush");
                    CurrentForeground = (Brush)FindResource("TierProFgBrush");
                    CurrentBorderBrush = (Brush)FindResource("TierProBorderBrush");
                    CurrentIcon = SymbolRegular.Star24;
                    break;
                case "enterprise":
                case "ent":
                    DisplayName = "ENTERPRISE";
                    CurrentBackground = (Brush)FindResource("TierEnterpriseBgBrush");
                    CurrentForeground = (Brush)FindResource("TierEnterpriseFgBrush");
                    CurrentBorderBrush = (Brush)FindResource("TierEnterpriseBorderBrush");
                    CurrentIcon = SymbolRegular.ShieldKeyhole24;
                    break;
                default:
                    DisplayName = (TierId ?? "CUSTOM").ToUpperInvariant();
                    CurrentBackground = (Brush)FindResource("StatusNeutralBgBrush");
                    CurrentForeground = (Brush)FindResource("StatusNeutralFgBrush");
                    CurrentBorderBrush = (Brush)FindResource("StatusNeutralBorderBrush");
                    CurrentIcon = SymbolRegular.Shield24;
                    break;
            }
        }
    }
}
