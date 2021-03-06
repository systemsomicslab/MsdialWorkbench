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
    /// Interaction logic for MS1Control.xaml
    /// </summary>
    public partial class MS1Control : UserControl
    {
        public MS1Control()
        {
            InitializeComponent();
        }

        public IAxisManager HorizontalAxis {
            get { return (IAxisManager)GetValue(HorizontalAxisProperty); }
            set { SetValue(HorizontalAxisProperty, value); }
        }

        public static readonly DependencyProperty HorizontalAxisProperty =
            DependencyProperty.Register(nameof(HorizontalAxis), typeof(IAxisManager), typeof(MS1Control), new PropertyMetadata(null));
    }
}
