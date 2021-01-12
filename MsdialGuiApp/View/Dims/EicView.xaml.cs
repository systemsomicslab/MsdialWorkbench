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

namespace CompMs.App.Msdial.View.Dims
{
    /// <summary>
    /// Interaction logic for EicView.xaml
    /// </summary>
    public partial class EicView : UserControl
    {
        public static readonly DependencyProperty MzAxisProperty = DependencyProperty.Register(nameof(MzAxis), typeof(AxisManager), typeof(EicView));

        public AxisManager MzAxis {
            get => (AxisManager)GetValue(MzAxisProperty);
            set => SetValue(MzAxisProperty, value);
        }

        public EicView() {
            InitializeComponent();
        }
    }
}
