using CompMs.App.Msdial.Property;
using CompMs.App.Msdial.StartUp;
using CompMs.App.Msdial.View;
using CompMs.App.Msdial.ViewModel;
using CompMs.CommonMVVM.WindowService;
using CompMs.MsdialCore.DataObj;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Ribbon;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace CompMs.App.Msdial
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : RibbonWindow
    {
        public MainWindow() {
            InitializeComponent();

            var startUpService = new DialogService<StartUpWindow, StartUpWindowVM>(this);
            var analysisFilePropertySetService = new DialogService<AnalysisFilePropertySetWindow, AnalysisFilePropertySetWindowVM>(this);
            var alignmentCompoundSearchService = new DialogService<CompoundSearchWindow, CompoundSearchVM<AlignmentSpotProperty>>(this);
            DataContext = new MainWindowVM(startUpService, analysisFilePropertySetService, alignmentCompoundSearchService);
        }

        public void CloseOwnedWindows() {
            foreach (var child in OwnedWindows.Cast<Window>()) {
                child.Close();
            }
        }
    }
}
