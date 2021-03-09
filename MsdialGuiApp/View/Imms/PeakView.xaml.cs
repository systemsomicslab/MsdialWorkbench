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

namespace CompMs.App.Msdial.View.Imms
{
    /// <summary>
    /// Interaction logic for PeakView.xaml
    /// </summary>
    public partial class PeakView : UserControl
    {
        public static readonly DependencyProperty HorizontalAxisProperty =
            DependencyProperty.Register(nameof(HorizontalAxis), typeof(IAxisManager), typeof(PeakView));
        
        public IAxisManager HorizontalAxis {
            get => (IAxisManager)GetValue(HorizontalAxisProperty);
            set => SetValue(HorizontalAxisProperty, value);
        }

        public static readonly DependencyProperty HorizontalTitleProperty
            = DependencyProperty.Register(
                nameof(HorizontalTitle), typeof(string), typeof(PeakView));

        public string HorizontalTitle {
            get => (string)GetValue(HorizontalTitleProperty);
            set => SetValue(HorizontalTitleProperty, value);
        }

        public static readonly DependencyProperty VerticalAxisProperty =
            DependencyProperty.Register(nameof(VerticalAxis), typeof(IAxisManager), typeof(PeakView));

        public IAxisManager VerticalAxis {
            get => (IAxisManager)GetValue(VerticalAxisProperty);
            set => SetValue(VerticalAxisProperty, value);
        }

        public static readonly DependencyProperty VerticalTitleProperty
            = DependencyProperty.Register(
                nameof(VerticalTitle), typeof(string), typeof(PeakView));
        
        public string VerticalTitle {
            get => (string)GetValue(VerticalTitleProperty);
            set => SetValue(VerticalTitleProperty, value);
        }


        public static readonly DependencyProperty GraphTitleProperty
            = DependencyProperty.Register(
                nameof(GraphTitle), typeof(string), typeof(PeakView));

        public string GraphTitle {
            get => (string)GetValue(GraphTitleProperty);
            set => SetValue(GraphTitleProperty, value);
        }

        public static readonly DependencyProperty LabelTemplateProperty
            = DependencyProperty.Register(
                nameof(LabelTemplate), typeof(DataTemplate), typeof(PeakView));

        public DataTemplate LabelTemplate {
            get => (DataTemplate)GetValue(LabelTemplateProperty);
            set => SetValue(LabelTemplateProperty, value);
        }

        public PeakView() {
            InitializeComponent();
        }
    }
}
