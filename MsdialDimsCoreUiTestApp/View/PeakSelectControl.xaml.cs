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

using CompMs.Graphics.Core.Base;

namespace MsdialDimsCoreUiTestApp.View
{
    /// <summary>
    /// Interaction logic for PeakSelectControl.xaml
    /// </summary>
    public partial class PeakSelectControl : UserControl
    {
        public PeakSelectControl() {
            InitializeComponent();
        }

        public AxisManager HorizontalAxis {
            get { return (AxisManager)GetValue(HorizontalAxisProperty); }
            set { SetValue(HorizontalAxisProperty, value); }
        }

        public static readonly DependencyProperty HorizontalAxisProperty =
            DependencyProperty.Register(nameof(HorizontalAxis), typeof(AxisManager), typeof(PeakSelectControl), new PropertyMetadata(null));


    }
}
