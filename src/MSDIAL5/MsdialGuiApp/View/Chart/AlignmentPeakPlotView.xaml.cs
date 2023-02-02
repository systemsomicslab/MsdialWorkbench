using System;
using System.Windows;
using System.Windows.Controls;

namespace CompMs.App.Msdial.View.Chart
{
    /// <summary>
    /// Interaction logic for AlignmentPeakPlotView.xaml
    /// </summary>
    public partial class AlignmentPeakPlotView : UserControl
    {
        public AlignmentPeakPlotView() {
            InitializeComponent();
        }

        public static readonly DependencyProperty LabelTemplateProperty
            = DependencyProperty.Register(
                nameof(LabelTemplate), typeof(DataTemplate), typeof(AlignmentPeakPlotView));

        public DataTemplate LabelTemplate {
            get => (DataTemplate)GetValue(LabelTemplateProperty);
            set => SetValue(LabelTemplateProperty, value);
        }
    }
}
