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

namespace Rfx.Riken.OsakaUniv
{
    /// <summary>
    /// SelectAdductType.xaml の相互作用ロジック
    /// </summary>
    public partial class SelectAdductType : Window
    {
        private SelectAdductTypeVM SelectAdductTypeVM;


        public SelectAdductType() {
            InitializeComponent();
        }

        public SelectAdductType(AnalysisParamOfMsfinder param) {
            InitializeComponent();

            SelectAdductTypeVM = new SelectAdductTypeVM(param);
            SelectAdductTypeVM.CloseViewHandler += (o, e) => { Close(); };

            this.DataContext = SelectAdductTypeVM;
            TabControl_AdductType.SelectedIndex = 2;
            TamItem_MS1_Positive.IsEnabled = false;
            TamItem_MS1_Negative.IsEnabled = false;
        }

    }
}
