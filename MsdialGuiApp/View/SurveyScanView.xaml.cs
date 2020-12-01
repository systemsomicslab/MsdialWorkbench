using CompMs.Graphics.Core.Base;
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
    /// Interaction logic for SurveyScanView.xaml
    /// </summary>
    public partial class SurveyScanView : UserControl
    {
        public static readonly DependencyProperty MassAxisProperty = DependencyProperty.Register(nameof(MassAxis), typeof(AxisManager), typeof(SurveyScanView));

        public AxisManager MassAxis {
            get => (AxisManager)GetValue(MassAxisProperty);
            set => SetValue(MassAxisProperty, value);
        }

        public SurveyScanView() {
            InitializeComponent();
        }
    }
}
