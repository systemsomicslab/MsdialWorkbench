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

namespace CompMs.App.Msdial.View.Chart
{
    /// <summary>
    /// Interaction logic for PeakView.xaml
    /// </summary>
    public partial class PeakView : UserControl
    {
        public PeakView() {
            InitializeComponent();
        }

        public static readonly DependencyProperty LabelTemplateProperty
            = DependencyProperty.Register(
                nameof(LabelTemplate), typeof(DataTemplate), typeof(PeakView));

        public DataTemplate LabelTemplate {
            get => (DataTemplate)GetValue(LabelTemplateProperty);
            set => SetValue(LabelTemplateProperty, value);
        }
    }
}
