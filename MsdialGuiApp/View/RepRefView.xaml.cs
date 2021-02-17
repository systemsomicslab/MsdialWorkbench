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

namespace CompMs.App.Msdial.View
{
    /// <summary>
    /// Interaction logic for RepRefView.xaml
    /// </summary>
    public partial class RepRefView : UserControl
    {
        public static readonly DependencyProperty SpectrumProperty =
            DependencyProperty.Register(
                nameof(Spectrum), typeof(IEnumerable), typeof(RepRefView),
                new FrameworkPropertyMetadata(null));

        public IEnumerable Spectrum
        {
            get => (IEnumerable)GetValue(SpectrumProperty);
            set => SetValue(SpectrumProperty, value);
        }

        public static readonly DependencyProperty ReferenceSpectrumProperty =
            DependencyProperty.Register(
                nameof(ReferenceSpectrum), typeof(IEnumerable), typeof(RepRefView),
                new FrameworkPropertyMetadata(null));

        public IEnumerable ReferenceSpectrum
        {
            get => (IEnumerable)GetValue(ReferenceSpectrumProperty);
            set => SetValue(ReferenceSpectrumProperty, value);
        }

        public static readonly DependencyProperty MassMinProperty =
            DependencyProperty.Register(
                nameof(MassMin), typeof(double), typeof(RepRefView),
                new FrameworkPropertyMetadata(null));

        public double MassMin
        {
            get => (double)GetValue(MassMinProperty);
            set => SetValue(MassMinProperty, value);
        }

        public static readonly DependencyProperty MassMaxProperty =
            DependencyProperty.Register(
                nameof(MassMax), typeof(double), typeof(RepRefView),
                new FrameworkPropertyMetadata(null));

        public double MassMax
        {
            get => (double)GetValue(MassMaxProperty);
            set => SetValue(MassMaxProperty, value);
        }

        public RepRefView() {
            InitializeComponent();
        }
    }
}
