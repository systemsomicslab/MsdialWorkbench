using System;
using System.Collections;
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

        public IAxisManager HorizontalAxis {
            get { return (IAxisManager)GetValue(HorizontalAxisProperty); }
            set { SetValue(HorizontalAxisProperty, value); }
        }

        public IEnumerable ItemsSource {
            get { return (IEnumerable)GetValue(ItemsSourceProperty); }
            set { SetValue(ItemsSourceProperty, value); }
        }

        public static readonly DependencyProperty HorizontalAxisProperty =
            DependencyProperty.Register(nameof(HorizontalAxis), typeof(IAxisManager), typeof(PeakSelectControl), new PropertyMetadata(null));

        public static readonly DependencyProperty ItemsSourceProperty =
            DependencyProperty.Register(nameof(ItemsSource), typeof(IEnumerable), typeof(PeakSelectControl), new PropertyMetadata(null));


    }
}
