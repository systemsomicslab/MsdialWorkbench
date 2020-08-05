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

namespace MsdialDimsCoreUiTestApp.View
{
    /// <summary>
    /// Interaction logic for MS2SpectrumControl.xaml
    /// </summary>
    public partial class MS2SpectrumControl : UserControl
    {
        public MS2SpectrumControl()
        {
            InitializeComponent();
        }



        public IEnumerable ItemsSource {
            get { return (IEnumerable)GetValue(ItemsSourceProperty); }
            set { SetValue(ItemsSourceProperty, value); }
        }

        public static readonly DependencyProperty ItemsSourceProperty =
            DependencyProperty.Register(nameof(ItemsSource), typeof(IEnumerable), typeof(MS2SpectrumControl), new PropertyMetadata(null));

    }
}
