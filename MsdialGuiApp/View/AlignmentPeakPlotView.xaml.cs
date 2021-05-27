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

namespace CompMs.App.Msdial.View
{
    /// <summary>
    /// Interaction logic for AlignmentPeakPlotView.xaml
    /// </summary>
    public partial class AlignmentPeakPlotView : UserControl
    {
        public AlignmentPeakPlotView() {
            InitializeComponent();
        }

        public static readonly DependencyProperty LabelTemplateProperty
            = DependencyProperty.Register(
                nameof(LabelTemplate), typeof(DataTemplate), typeof(AlignmentPeakPlotView));

        public DataTemplate LabelTemplate {
            get => (DataTemplate)GetValue(LabelTemplateProperty);
            set => SetValue(LabelTemplateProperty, value);
        }
    }
}
