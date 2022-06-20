using System.Windows;
using System.Windows.Controls.Ribbon;

namespace CompMs.App.Msdial.View.Dims
{
    /// <summary>
    /// Interaction logic for DimsRibbon.xaml
    /// </summary>
    public partial class DimsRibbon : Ribbon
    {
        public readonly DependencyProperty IsAnalysisViewFocusedProperty =
            DependencyProperty.Register(
                nameof(IsAnalysisViewFocused), typeof(bool), typeof(DimsRibbon),
                new FrameworkPropertyMetadata(true));

        public bool IsAnalysisViewFocused {
            get => (bool)GetValue(IsAnalysisViewFocusedProperty);
            set => SetValue(IsAnalysisViewFocusedProperty, value);
        }

        public readonly DependencyProperty IsAlignmentViewFocusedProperty =
            DependencyProperty.Register(
                nameof(IsAlignmentViewFocusedProperty), typeof(bool), typeof(DimsRibbon),
                new FrameworkPropertyMetadata(false));

        public bool IsAlignmentViewFocused {
            get => (bool)GetValue(IsAlignmentViewFocusedProperty);
            set => SetValue(IsAlignmentViewFocusedProperty, value);
        }

        public DimsRibbon() {
            InitializeComponent();
        }
    }
}
