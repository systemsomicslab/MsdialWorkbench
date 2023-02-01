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
using System.Windows.Shapes;

namespace Rfx.Riken.OsakaUniv.ForAIF.CorrDec {
    /// <summary>
    /// Interaction logic for AskDecMethodWindow.xaml
    /// </summary>
    public partial class AskDecMethodWindow : Window {
        public AskDecMethodWindow(Action ms2decAction, Action corrDecAction) {
            InitializeComponent();
            this.DataContext = new AskDecMethodVM(ms2decAction, corrDecAction);
        }
    }

    public enum DeconvolutionType { MS2Dec, CorrDec }
    public class AskDecMethodVM : ViewModelBase {
        public DeconvolutionType[] ItemsSource { get; set; } = new DeconvolutionType[] { DeconvolutionType.MS2Dec, DeconvolutionType.CorrDec };
        public DeconvolutionType SelectedItem { get; set; } = DeconvolutionType.CorrDec;
        public DelegateCommand FinishCommand { get; set; }

        public AskDecMethodVM(Action ms2decAction, Action corrDecAction) {
            FinishCommand = new DelegateCommand(x => {
                switch (SelectedItem) {
                    case DeconvolutionType.MS2Dec:
                        ms2decAction?.Invoke();
                        break;
                    case DeconvolutionType.CorrDec:
                        corrDecAction?.Invoke();
                        break;
                    default:
                        break;
                }
                var w = x as AskDecMethodWindow;
                w.Close();
            },
            x => true);
        }
    }
}
