using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace CompMs.App.Msdial.View.Dims
{
    /// <summary>
    /// Interaction logic for MainWindowRibbon.xaml
    /// </summary>
    public partial class MainRibbon : UserControl
    {
        public readonly DependencyProperty IsAnalysisViewFocusedProperty =
            DependencyProperty.Register(
                nameof(IsAnalysisViewFocused), typeof(bool), typeof(MainRibbon),
                new FrameworkPropertyMetadata(true));

        public bool IsAnalysisViewFocused {
            get => (bool)GetValue(IsAnalysisViewFocusedProperty);
            set => SetValue(IsAnalysisViewFocusedProperty, value);
        }

        public readonly DependencyProperty IsAlignmentViewFocusedProperty =
            DependencyProperty.Register(
                nameof(IsAlignmentViewFocusedProperty), typeof(bool), typeof(MainRibbon),
                new FrameworkPropertyMetadata(false));

        public bool IsAlignmentViewFocused {
            get => (bool)GetValue(IsAlignmentViewFocusedProperty);
            set => SetValue(IsAlignmentViewFocusedProperty, value);
        }

        public MainRibbon() {
            InitializeComponent();
        }
    }
}
