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

namespace CompMs.App.Msdial.View
{
    /// <summary>
    /// Interaction logic for EicView.xaml
    /// </summary>
    public partial class EicView : UserControl
    {
        public static readonly DependencyProperty ChromAxisProperty =
            DependencyProperty.Register(nameof(ChromAxis), typeof(IAxisManager), typeof(EicView));

        public IAxisManager ChromAxis {
            get => (IAxisManager)GetValue(ChromAxisProperty);
            set => SetValue(ChromAxisProperty, value);
        }

        public static readonly DependencyProperty ChromTitleProperty =
            DependencyProperty.Register(nameof(ChromTitle), typeof(string), typeof(EicView));

        public string ChromTitle {
            get => (string)GetValue(ChromTitleProperty);
            set => SetValue(ChromTitleProperty, value);
        }

        public EicView() {
            InitializeComponent();
        }
    }
}
