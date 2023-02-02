using Riken.Metabolomics.StructureFinder.RtPrediction;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;

namespace Rfx.Riken.OsakaUniv {
    public class RtPredictionVM : ViewModelBase {

        private RtPredictionSummaryWin window;
        private AnalysisParameterSettingVM paramVM;
        private RtPredictionResult result;
        private List<RtStructureQuery> rtStructureQueries;

        public RtPredictionVM(AnalysisParameterSettingVM param) {
            this.paramVM = param;
            var loadedFilepath = param.RtSmilesDictionaryFilepath;
            Mouse.OverrideCursor = Cursors.Wait;
            if (!System.IO.File.Exists(loadedFilepath)) {
                MessageBox.Show("File not exist.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                Mouse.OverrideCursor = null;
                return;
            }

            var queries = RtPrediction.ReadQueries(loadedFilepath);
            if (queries == null || queries.Count == 0) {
                MessageBox.Show("No suitable RT-SMILES query.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                Mouse.OverrideCursor = null;
                return;
            }

            this.result = RtPrediction.GetRtPredictionResult(queries);
            if (this.result == null) {
                MessageBox.Show("No result returned.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                Mouse.OverrideCursor = null;
                return;
            }
            this.rtStructureQueries = this.result.RtStructureQueries;

            Mouse.OverrideCursor = null;
        }

        private void showResult(RtPredictionResult result) {
            this.window.Label_Equation.Content = Math.Round(result.Coefficient, 3) + "* XLogP + " + Math.Round(result.Intercept, 3);
            this.window.Label_Rsquared.Content = Math.Round(result.Rsqure, 5);
            this.window.Label_RtErrorMax.Content = Math.Round(result.MaxRtDiff, 3);
            this.window.Label_RtErrorMin.Content = Math.Round(result.MinRtDiff, 3);

            var xArray_ExpRTs = new ObservableCollection<double>();
            var yArray_PredRTs = new ObservableCollection<double>();
            var labels = new ObservableCollection<string>();
            var colors = new ObservableCollection<SolidColorBrush>();

            foreach (var predResult in result.RtStructureQueries) {
                xArray_ExpRTs.Add(predResult.ExperimentRt);
                yArray_PredRTs.Add(predResult.PredictedRt);
                labels.Add(predResult.Name);
                colors.Add(Brushes.Red);
            }

            var xAxisTitle = "Experimental RT (min)";
            var yAxisTitle = "Predicted RT (min)";

            var pairwise = new PairwisePlotBean("Prediction result", xAxisTitle, yAxisTitle,
                xArray_ExpRTs, yArray_PredRTs, labels, colors, PairwisePlotDisplayLabel.Label);

            this.window.PairwiseUI.Content = new PairwisePlotUI(pairwise);

            OnPropertyChanged("RtStructureQueries");
        }

        #region properties
        public List<RtStructureQuery> RtStructureQueries {
            get {
                return rtStructureQueries;
            }

            set {
                rtStructureQueries = value;
                OnPropertyChanged("RtStructureQueries");
            }
        }

        public RtPredictionResult Result {
            get {
                return result;
            }

            set {
                result = value;
            }
        }
        #endregion

        /// <summary>
        /// Sets up the view model for the RtPredictionSummary window in InvokeCommandAction
        /// </summary>
        private DelegateCommand windowLoaded;
        public DelegateCommand WindowLoaded {
            get {
                return windowLoaded ?? (windowLoaded = new DelegateCommand(Window_Loaded, obj => { return true; }));
            }
        }
        
        private void Window_Loaded(object obj) {
            this.window = (RtPredictionSummaryWin)obj;
            showResult(this.result);
        }

        /// <summary>
        /// cancel command
        /// </summary>
        private DelegateCommand cancel;
        public DelegateCommand Cancel {
            get {
                return cancel ?? (cancel = new DelegateCommand(obj => { this.window.Close(); }, obj => { return true; }));
            }
        }

        /// <summary>
        /// reflect this result to param
        /// </summary>
        private DelegateCommand reflectResult;
        public DelegateCommand ReflectResult {
            get {
                return reflectResult ?? (reflectResult = new DelegateCommand(resultReflection, obj => { return true; }));
            }
        }

        private void resultReflection(object obj) {
            this.paramVM.Coeff_RtPrediction = this.result.Coefficient;
            this.paramVM.Intercept_RtPrediction = this.result.Intercept;
            this.paramVM.RtPredictionSummaryReport = this.window.Label_Equation.Content.ToString() + " (min); R-squared: " + this.window.Label_Rsquared.Content.ToString();

            this.window.Close();
        }
    }
}
