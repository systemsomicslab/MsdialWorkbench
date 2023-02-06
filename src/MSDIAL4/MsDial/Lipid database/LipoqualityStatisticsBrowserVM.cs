using Common.BarChart;
using Msdial.Lcms.Dataprocess.Algorithm;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media;

namespace Rfx.Riken.OsakaUniv {
    public class LipoqualityStatisticsBrowserVM : ViewModelBase {

        private LipoqualityQuantTree lipoqualityQuantTrees;
        private LipoqualityQuantTree selectedQuantTree;
        private string lqID;

        private LipoqualityStatisticsBrowserWin window;
        private List<SolidColorBrush> solidColorBrushList;

        private bool isIntensity;
        private bool isCount;
        private bool isIntensityCount;


        public LipoqualityStatisticsBrowserVM(string lqID, LipoqualityQuantTree lipoqualityQuantTree) {
            this.lipoqualityQuantTrees = lipoqualityQuantTree;
            this.lqID = lqID;
            this.selectedQuantTree = lipoqualityQuantTree[0];
            this.isIntensity = true;
        }

        public string LqID {
            get { return lqID; }
            set { if (lqID == value) return; lqID = value; OnPropertyChanged("LqID"); }
        }

        public int StudyCount {
            get { return (int)lipoqualityQuantTrees.StudyCount; }
            set { if (lipoqualityQuantTrees.StudyCount == value) return; lipoqualityQuantTrees.StudyCount = value; OnPropertyChanged("StudyCount"); }
        }

        public LipoqualityQuantTree LipoqualityQuantTree {
            get { return lipoqualityQuantTrees; }
            set { if (lipoqualityQuantTrees == value) return; lipoqualityQuantTrees = value; OnPropertyChanged("LipoqualityQuantTree"); }
        }

        public LipoqualityQuantTree SelectedQuantTree {
            get { return selectedQuantTree; }
            set { if (selectedQuantTree == value) return; selectedQuantTree = value; OnPropertyChanged("SelectedQuantTree"); updateSubClassUI(); }
        }

        public bool IsIntensity {
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

        public bool IsCount {
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

        public bool IsIntensityCount {
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
        public DelegateCommand WindowLoaded {
            get {
                return windowLoaded ?? (windowLoaded = new DelegateCommand(Window_Loaded, obj => { return true; }));
            }
        }

        /// <summary>
        /// Action for the WindowLoaded command
        /// </summary>
        /// <param name="obj"></param>
        private void Window_Loaded(object obj) {
            var view = (LipoqualityStatisticsBrowserWin)obj;
            var mainWindow = (MainWindow)((LipoqualitySpectrumSearchWin)view.Owner).Owner;

            this.window = view;
            this.solidColorBrushList = mainWindow.SolidColorBrushList;

            updateMainClassUI();
            updateSubClassUI();
        }

        private void updateMainClassUI() {
            if (this.lipoqualityQuantTrees == null) return;
            if (this.solidColorBrushList == null) return;
            var barChartBean = GetMainBarChartBean(this.lipoqualityQuantTrees, this.solidColorBrushList, this.isIntensity, this.isCount, this.isIntensityCount);
            if (barChartBean == null) return;
            this.window.MainClass_BarGraphUI.Content = new BarChartUI(barChartBean);
        }

        public void updateSubClassUI() {
            if (this.selectedQuantTree == null) return;
            if (this.solidColorBrushList == null) return;
            var barChartBean = GetSubClassBarChartBean(this.selectedQuantTree, this.solidColorBrushList, this.isIntensity, this.isCount, this.isIntensityCount);
            if (barChartBean == null) return;
            this.window.SubClass_BarGraphUI.Content = new BarChartUI(barChartBean);
        }


        public BarChartBean GetMainBarChartBean(LipoqualityQuantTree lipoqualityQuantTree, List<SolidColorBrush> colorBrushes
            , bool isIntensity, bool isCount, bool isIntensityCount) {
            var barElements = new List<BarElement>();
            var counter = 0;
            foreach (var bin in lipoqualityQuantTree) {

                var average = bin.StudyCount; if (isIntensity) average = bin.Intensity; else if (isIntensityCount) average = bin.Intensity * bin.StudyCount;
                var stdev = bin.Error;
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

            return new BarChartBean(barElements, "Biological statistics of " + this.lqID, "Class", "Intensity");
        }

        public BarChartBean GetSubClassBarChartBean(LipoqualityQuantTree lipoqualityQuantTree, List<SolidColorBrush> colorBrushes
            , bool isIntensity, bool isCount, bool isIntensityCount) {
            var barElements = new List<BarElement>();
            var counter = 0;
            if (lipoqualityQuantTree.SubClass == null) return null;
            foreach (var bin in lipoqualityQuantTree.SubClass) {

                var average = bin.StudyCount; if (isIntensity) average = bin.Intensity; else if (isIntensityCount) average = bin.Intensity * bin.StudyCount;
                var stdev = bin.Error;
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

            return new BarChartBean(barElements, lipoqualityQuantTree.ClassName, "Class", "Intensity");
        }

    }
}
