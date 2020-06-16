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

using CompMs.Common.Components;

namespace MsdialDimsCoreUiTestApp.View
{
    /// <summary>
    /// Interaction logic for MS2RawSpectrumControl.xaml
    /// </summary>
    public partial class MS2RawSpectrumControl : UserControl
    {
        public IEnumerable SelectedReferences
        {
            get { return (IEnumerable)GetValue(SelectedReferencesProperty); }
            set { SetValue(SelectedReferencesProperty, value); }
        }

        // Using a DependencyProperty as the backing store for moleculeMsReferences.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty SelectedReferencesProperty =
            DependencyProperty.Register(nameof(SelectedReferences), typeof(IEnumerable), typeof(MS2RawSpectrumControl), new PropertyMetadata(default));

        public IEnumerable SelectedDetection
        {
            get { return (IEnumerable)GetValue(SelectedDetectionProperty); }
            set { SetValue(SelectedDetectionProperty, value); }
        }

        // Using a DependencyProperty as the backing store for moleculeMsReferences.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty SelectedDetectionProperty =
            DependencyProperty.Register(nameof(SelectedDetection), typeof(IEnumerable), typeof(MS2RawSpectrumControl), new PropertyMetadata(default));

        public MS2RawSpectrumControl()
        {
            InitializeComponent();
        }
    }
}
