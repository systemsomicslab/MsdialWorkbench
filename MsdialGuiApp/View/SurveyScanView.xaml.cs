using CompMs.Graphics.Core.Base;
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
    /// Interaction logic for SurveyScanView.xaml
    /// </summary>
    public partial class SurveyScanView : UserControl
    {
        public static readonly DependencyProperty SpectrumProperty =
            DependencyProperty.Register(
                nameof(Spectrum), typeof(IEnumerable), typeof(SurveyScanView),
                new FrameworkPropertyMetadata(null));

        public IEnumerable Spectrum
        {
            get => (IEnumerable)GetValue(SpectrumProperty);
            set => SetValue(SpectrumProperty, value);
        }

        public static readonly DependencyProperty MaxIntensityProperty =
            DependencyProperty.Register(
                nameof(MaxIntensity), typeof(double), typeof(SurveyScanView),
                new FrameworkPropertyMetadata(0d));

        public double MaxIntensity
        {
            get => (double)GetValue(MaxIntensityProperty);
            set => SetValue(MaxIntensityProperty, value);
        }

        public static readonly DependencyProperty MassAxisProperty =
            DependencyProperty.Register(
                nameof(MassAxis), typeof(IAxisManager), typeof(SurveyScanView),
                new FrameworkPropertyMetadata(null));

        public IAxisManager MassAxis {
            get => (IAxisManager)GetValue(MassAxisProperty);
            set => SetValue(MassAxisProperty, value);
        }

        public static readonly DependencyProperty SplashKeyProperty =
            DependencyProperty.Register(
                nameof(SplashKey), typeof(string), typeof(SurveyScanView),
                new FrameworkPropertyMetadata(string.Empty));
        
        public string SplashKey
        {
            get => (string)GetValue(SplashKeyProperty);
            set => SetValue(SplashKeyProperty, value);
        }

        public SurveyScanView() {
            InitializeComponent();
        }
    }
}
