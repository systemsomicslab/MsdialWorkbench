using Common.BarChart;
using Riken.Metabolomics.BinVestigate;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Media;

namespace Rfx.Riken.OsakaUniv
{
    public class BinVestigateStatisticsBrowserVM : ViewModelBase
    {
        private BinBaseQuantTree binbaseQuantTrees;
        private BinBaseQuantTree selectedQuantTree;
        private int binID;

        private BinVestigateStatisticsBrowserWin window;
        private List<SolidColorBrush> solidColorBrushList;

        private bool isIntensity;
        private bool isCount;
        private bool isIntensityCount;


        public BinVestigateStatisticsBrowserVM(int binID, BinBaseQuantTree binbaseQuantTree)
        {
            this.binbaseQuantTrees = binbaseQuantTree;
            this.binID = binID;
            this.selectedQuantTree = binbaseQuantTree[0];
            this.isCount = true;
        }

        public int BinID
        {
            get { return binID; }
            set { if (binID == value) return; binID = value; OnPropertyChanged("BinID"); }
        }

        public int StudyCount
        {
            get { return (int)binbaseQuantTrees.StudyCount; }
            set { if (binbaseQuantTrees.StudyCount == value) return; binbaseQuantTrees.StudyCount = value; OnPropertyChanged("StudyCount"); }
        }

        public BinBaseQuantTree BinbaseQuantTrees
        {
           get { return binbaseQuantTrees; }
           set { if (binbaseQuantTrees == value) return; binbaseQuantTrees = value; OnPropertyChanged("BinbaseQuantTrees"); }
        }

        public BinBaseQuantTree SelectedQuantTree
        {
            get { return selectedQuantTree; }
            set { if (selectedQuantTree == value) return; selectedQuantTree = value; OnPropertyChanged("SelectedQuantTree"); updateSubClassUI(); }
        }

        public bool IsIntensity
        {
            get { return isIntensity; }
            set {
                if (isIntensity == value) return; isIntensity = value;
                OnPropertyChanged("IsIntensity");
                if (isIntensity) {
                    this.isCount = false;
                    this.isIntensityCount = false;
                }
                updateMainClassUI();
                updateSubClassUI();
            }
        }

        public bool IsCount
        {
            get { return isCount; }
            set {
                if (isCount == value) return; isCount = value;
                OnPropertyChanged("IsCount");
                if (isCount) {
                    this.isIntensity = false;
                    this.isIntensityCount = false;
                }
                updateMainClassUI();
                updateSubClassUI();
            }
        }

        public bool IsIntensityCount
        {
            get { return isIntensityCount; }
            set {
                if (isIntensityCount == value) return; isIntensityCount = value;
                OnPropertyChanged("IsIntensityCount");
                if (isIntensityCount) {
                    this.isCount = false;
                    this.isIntensity = false;
                }
                updateMainClassUI();
                updateSubClassUI();
            }
        }


        /// <summary>
        /// Sets up the view model for the BinVestigate statistics browser window in InvokeCommandAction
        /// </summary>
        private DelegateCommand windowLoaded;
        public DelegateCommand WindowLoaded
        {
            get {
                return windowLoaded ?? (windowLoaded = new DelegateCommand(Window_Loaded, obj => { return true; }));
            }
        }

        /// <summary>
        /// Action for the WindowLoaded command
        /// </summary>
        /// <param name="obj"></param>
        private void Window_Loaded(object obj)
        {
            var view = (BinVestigateStatisticsBrowserWin)obj;
            var mainWindow = (MainWindow)((BinVestigateSpectrumSearchWin)view.Owner).Owner;

            this.window = view;
            this.solidColorBrushList = mainWindow.SolidColorBrushList;

            updateMainClassUI();
            updateSubClassUI();
        }

        private void updateMainClassUI()
        {
            if (this.binbaseQuantTrees == null) return;
            if (this.solidColorBrushList == null) return;
            var barChartBean = GetMainBarChartBean(this.binbaseQuantTrees, this.solidColorBrushList, this.isIntensity, this.isCount, this.isIntensityCount);
            if (barChartBean == null) return;
            this.window.MainClass_BarGraphUI.Content = new BarChartUI(barChartBean);
        }

        public void updateSubClassUI()
        {
            if (this.selectedQuantTree == null) return;
            if (this.solidColorBrushList == null) return;
            var barChartBean = GetSubClassBarChartBean(this.selectedQuantTree, this.solidColorBrushList, this.isIntensity, this.isCount, this.isIntensityCount);
            if (barChartBean == null) return;
            this.window.SubClass_BarGraphUI.Content = new BarChartUI(barChartBean);
        }


        public BarChartBean GetMainBarChartBean(BinBaseQuantTree binbaseQuantTree, List<SolidColorBrush> colorBrushes
            , bool isIntensity, bool isCount, bool isIntensityCount)
        {
            var barElements = new List<BarElement>();
            var counter = 0;
            foreach (var bin in binbaseQuantTree) {

                var average = bin.StudyCount; if (isIntensity) average = bin.Intensity; else if (isIntensityCount) average = bin.Intensity * bin.StudyCount;
                var stdev = 0.0;
                if (counter > colorBrushes.Count - 1) counter = 0;

                var barElement = new BarElement() {
                    Value = average,
                    Error = stdev,
                    Legend = bin.ClassName,
                    Brush = colorBrushes[counter]
                };
                barElements.Add(barElement);
                counter++;
            }
            if (barElements.Count == 0) return null;
            barElements = barElements.OrderByDescending(n => n.Value).ToList();
            
            return new BarChartBean(barElements, "Biological statistics of Bin ID " + this.binID, "Class", "Intensity");
        }

        public BarChartBean GetSubClassBarChartBean(BinBaseQuantTree binbaseQuantTree, List<SolidColorBrush> colorBrushes
            , bool isIntensity, bool isCount, bool isIntensityCount)
        {
            var barElements = new List<BarElement>();
            var counter = 0;
            if (binbaseQuantTree.SubClass == null) return null;
            foreach (var bin in binbaseQuantTree.SubClass) {

                var average = bin.StudyCount; if (isIntensity) average = bin.Intensity; else if (isIntensityCount) average = bin.Intensity * bin.StudyCount;
                var stdev = 0.0;
                if (counter > colorBrushes.Count - 1) counter = 0;

                var barElement = new BarElement() {
                    Value = average,
                    Error = stdev,
                    Legend = bin.ClassName,
                    Brush = colorBrushes[counter]
                };
                barElements.Add(barElement);
                counter++;
            }
            if (barElements.Count == 0) return null;
            barElements = barElements.OrderByDescending(n => n.Value).ToList();

            return new BarChartBean(barElements, binbaseQuantTree.ClassName, "Class", "Intensity");
        }
    }
}
