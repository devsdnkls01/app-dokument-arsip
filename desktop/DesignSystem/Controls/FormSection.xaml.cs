using System.Windows;
using System.Windows.Controls;

namespace DesignSystem.Controls
{
    public partial class FormSection : UserControl
    {
        public static readonly DependencyProperty TitleProperty =
            DependencyProperty.Register(nameof(Title), typeof(string), typeof(FormSection), new PropertyMetadata("Informasi"));

        public static readonly DependencyProperty DescriptionProperty =
            DependencyProperty.Register(nameof(Description), typeof(string), typeof(FormSection), new PropertyMetadata(string.Empty, OnDescriptionChanged));

        public static readonly DependencyProperty SectionContentProperty =
            DependencyProperty.Register(nameof(SectionContent), typeof(object), typeof(FormSection), new PropertyMetadata(null));

        public static readonly DependencyProperty HasDescriptionProperty =
            DependencyProperty.Register(nameof(HasDescription), typeof(bool), typeof(FormSection), new PropertyMetadata(false));

        public FormSection()
        {
            InitializeComponent();
        }

        public string Title
        {
            get => (string)GetValue(TitleProperty);
            set => SetValue(TitleProperty, value);
        }

        public string Description
        {
            get => (string)GetValue(DescriptionProperty);
            set => SetValue(DescriptionProperty, value);
        }

        public object SectionContent
        {
            get => GetValue(SectionContentProperty);
            set => SetValue(SectionContentProperty, value);
        }

        public bool HasDescription
        {
            get => (bool)GetValue(HasDescriptionProperty);
            set => SetValue(HasDescriptionProperty, value);
        }

        private static void OnDescriptionChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is FormSection section)
            {
                section.HasDescription = !string.IsNullOrWhiteSpace(section.Description);
            }
        }
    }
}
