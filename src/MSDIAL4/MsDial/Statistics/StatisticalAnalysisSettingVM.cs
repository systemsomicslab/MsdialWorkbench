using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;

namespace Rfx.Riken.OsakaUniv
{
    [ValueConversion(typeof(ScaleMethod), typeof(int))]
    public class ScaleEnumToIntConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            ScaleMethod scale = (ScaleMethod)value;

            switch (scale)
            {
                case ScaleMethod.None:
                    return 0;
                case ScaleMethod.MeanCenter:
                    return 1;
                case ScaleMethod.ParetoScale:
                    return 2;
                case ScaleMethod.AutoScale:
                    return 3;
                default:
                    return 0;
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var scaleID = (int)value;
            var scale = (ScaleMethod)scaleID;

            return scale;
        }
    }

    [ValueConversion(typeof(TransformMethod), typeof(int))]
    public class TransformEnumToIntConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            TransformMethod transform = (TransformMethod)value;

            switch (transform)
            {
                case TransformMethod.None:
                    return 0;
                case TransformMethod.Log10:
                    return 1;
                case TransformMethod.QuadRoot:
                    return 2;
                default:
                    return 0;
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var transformID = (int)value;
            var transform = (TransformMethod)transformID;
           
            return transform;
        }
    }

    public class PcaSettingVM : ViewModelBase
    {
        private MainWindow mainWindow;
        private Window window;

        //PCA
        private int maxPcNumber;
        private ScaleMethod scale;
        private TransformMethod transform;
        private ObservableCollection<string> scaleMethod;
        private ObservableCollection<string> transformMethod;
        private bool isIdentifiedImportedInStatistics;
        private bool isAnnotatedImportedInStatistics;
        private bool isUnknownImportedInStatistics;

        public PcaSettingVM(MainWindow mainWindow, Window window)
        {
            this.mainWindow = mainWindow;
            this.window = window;
            this.scaleMethod = new ObservableCollection<string>() { "None", "Mean center", "Pareto scale", "Auto scale" };
            this.transformMethod = new ObservableCollection<string>() { "None", "Log10", "Quad root" };

            if (this.mainWindow.ProjectProperty.Ionization == Ionization.ESI)
            {
                var param = mainWindow.AnalysisParamForLC;

                this.scale = param.Scale;
                this.transform = param.Transform;
                if (param.MaxComponent < 5)
                    this.maxPcNumber = 5;
                else
                    this.maxPcNumber = param.MaxComponent;

                this.IsIdentifiedImportedInStatistics = param.IsIdentifiedImportedInStatistics;
                this.IsAnnotatedImportedInStatistics = param.IsAnnotatedImportedInStatistics;
                this.IsUnknownImportedInStatistics = param.IsUnknownImportedInStatistics;

                if (!param.IsIdentifiedImportedInStatistics &&
                    !param.IsAnnotatedImportedInStatistics &&
                    !param.IsUnknownImportedInStatistics) {
                    this.IsIdentifiedImportedInStatistics = true;
                    this.IsAnnotatedImportedInStatistics = true;
                    this.IsUnknownImportedInStatistics = true;
                }
            }
            else
            {
                var param = mainWindow.AnalysisParamForGC;

                this.scale = param.Scale;
                this.transform = param.Transform;
                if (param.MaxComponent < 5)
                    this.maxPcNumber = 5;
                else
                    this.maxPcNumber = param.MaxComponent;

                this.IsIdentifiedImportedInStatistics = param.IsIdentifiedImportedInStatistics;
                this.IsAnnotatedImportedInStatistics = param.IsAnnotatedImportedInStatistics;
                this.IsUnknownImportedInStatistics = param.IsUnknownImportedInStatistics;

                if (!param.IsIdentifiedImportedInStatistics &&
                  !param.IsAnnotatedImportedInStatistics &&
                  !param.IsUnknownImportedInStatistics) {
                    this.IsIdentifiedImportedInStatistics = true;
                    this.IsAnnotatedImportedInStatistics = true;
                    this.IsUnknownImportedInStatistics = true;
                }
            }
        }

        protected override void executeCommand(object parameter)
        {
            base.executeCommand(parameter);

            if (this.isIdentifiedImportedInStatistics == false &&
              this.isAnnotatedImportedInStatistics == false &&
              this.isUnknownImportedInStatistics == false) {
                MessageBox.Show("Please select at least one metabolite selection option.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            if (this.mainWindow.ProjectProperty.Ionization == Ionization.ESI)
            {
                this.mainWindow.AnalysisParamForLC.Scale = this.scale;
                this.mainWindow.AnalysisParamForLC.Transform = this.transform;
                this.mainWindow.AnalysisParamForLC.MaxComponent = this.maxPcNumber;

                this.mainWindow.AnalysisParamForLC.IsIdentifiedImportedInStatistics = this.isIdentifiedImportedInStatistics;
                this.mainWindow.AnalysisParamForLC.IsAnnotatedImportedInStatistics = this.isAnnotatedImportedInStatistics;
                this.mainWindow.AnalysisParamForLC.IsUnknownImportedInStatistics = this.isUnknownImportedInStatistics;
            }
            else
            {
                this.mainWindow.AnalysisParamForGC.Scale = this.scale;
                this.mainWindow.AnalysisParamForGC.Transform = this.transform;
                this.mainWindow.AnalysisParamForGC.MaxComponent = this.maxPcNumber;

                this.mainWindow.AnalysisParamForGC.IsIdentifiedImportedInStatistics = this.isIdentifiedImportedInStatistics;
                this.mainWindow.AnalysisParamForGC.IsAnnotatedImportedInStatistics = this.isAnnotatedImportedInStatistics;
                this.mainWindow.AnalysisParamForGC.IsUnknownImportedInStatistics = this.isUnknownImportedInStatistics;
            }

            MsDialStatistics.PrincipalComponentAnalysis(this.mainWindow, this.scale, this.transform, 
                this.isIdentifiedImportedInStatistics, this.isAnnotatedImportedInStatistics, this.isUnknownImportedInStatistics, 
                this.maxPcNumber);
            this.window.DialogResult = true;
            this.window.Close();
        }

        #region
        public int MaxPcNumber
        {
            get { return maxPcNumber; }
            set { if (maxPcNumber == value) return; maxPcNumber = value; OnPropertyChanged("MaxPcNumber"); }
        }

        public ObservableCollection<string> ScaleMethod
        {
            get { return scaleMethod; }
            set { scaleMethod = value; }
        }

        public ObservableCollection<string> TransformMethod
        {
            get { return transformMethod; }
            set { transformMethod = value; }
        }

        public ScaleMethod Scale
        {
            get { return scale; }
            set { if (scale == value) return; scale = value; OnPropertyChanged("Scale"); }
        }

        public TransformMethod Transform
        {
            get { return transform; }
            set { if (transform == value) return; transform = value; OnPropertyChanged("Transform"); }
        }

        public bool IsIdentifiedImportedInStatistics {
            get { return isIdentifiedImportedInStatistics; }
            set { if (isIdentifiedImportedInStatistics == value) return;
                isIdentifiedImportedInStatistics = value;
                OnPropertyChanged("IsIdentifiedImportedInStatistics");
            }
        }

        public bool IsAnnotatedImportedInStatistics {
            get { return isAnnotatedImportedInStatistics; }
            set { if (isAnnotatedImportedInStatistics == value) return;
                isAnnotatedImportedInStatistics = value;
                OnPropertyChanged("IsAnnotatedImportedInStatistics");
            }
        }

        public bool IsUnknownImportedInStatistics {
            get { return isUnknownImportedInStatistics; }
            set { if (isUnknownImportedInStatistics == value) return;
                isUnknownImportedInStatistics = value;
                OnPropertyChanged("IsUnknownImportedInStatistics");
            }
        }
        #endregion
    }

    public class PlsSettingVM : ViewModelBase {
        private MainWindow mainWindow;
        private Window window;

        //PLS        
        private int componentNumber = 2;
        private bool isAutoFitPls = true;
        private bool isUserDefined;
        private ScaleMethod scale = OsakaUniv.ScaleMethod.AutoScale;
        private TransformMethod transform = OsakaUniv.TransformMethod.None;
        private MultivariateAnalysisOption plsOption = MultivariateAnalysisOption.Plsda;
        private ObservableCollection<string> scaleMethod;
        private ObservableCollection<string> transformMethod;

        private bool isIdentifiedImportedInStatistics;
        private bool isAnnotatedImportedInStatistics;
        private bool isUnknownImportedInStatistics;

        public PlsSettingVM(MainWindow mainWindow, Window window) {

            this.mainWindow = mainWindow;
            this.window = window;
            this.scaleMethod = new ObservableCollection<string>() { "None", "Mean center", "Pareto scale", "Auto scale" };
            this.transformMethod = new ObservableCollection<string>() { "None", "Log10", "Quad root" };


            if (this.mainWindow.ProjectProperty.Ionization == Ionization.ESI) {

                var param = this.mainWindow.AnalysisParamForLC;
                this.scale = param.ScalePls;
                this.transform = param.TransformPls;
                if (param.ComponentPls < 5)
                    this.componentNumber = 2;
                else
                    this.componentNumber = param.ComponentPls;
                this.plsOption = param.MultivariateAnalysisOption;
                this.isAutoFitPls = param.IsAutoFitPls;
                this.isUserDefined = !this.isAutoFitPls;

                this.IsIdentifiedImportedInStatistics = param.IsIdentifiedImportedInStatistics;
                this.IsAnnotatedImportedInStatistics = param.IsAnnotatedImportedInStatistics;
                this.IsUnknownImportedInStatistics = param.IsUnknownImportedInStatistics;

                if (!param.IsIdentifiedImportedInStatistics &&
                    !param.IsAnnotatedImportedInStatistics &&
                    !param.IsUnknownImportedInStatistics) {
                    this.IsIdentifiedImportedInStatistics = true;
                    this.IsAnnotatedImportedInStatistics = true;
                    this.IsUnknownImportedInStatistics = true;
                }
            }
            else {

                var param = this.mainWindow.AnalysisParamForGC;

                this.scale = param.ScalePls;
                this.transform = param.TransformPls;
                if (param.ComponentPls < 5)
                    this.componentNumber = 2;
                else
                    this.componentNumber = param.ComponentPls;
                this.plsOption = param.MultivariateAnalysisOption;
                this.isAutoFitPls = param.IsAutoFitPls;
                this.isUserDefined = !this.isAutoFitPls;

                this.IsIdentifiedImportedInStatistics = param.IsIdentifiedImportedInStatistics;
                this.IsAnnotatedImportedInStatistics = param.IsAnnotatedImportedInStatistics;
                this.IsUnknownImportedInStatistics = param.IsUnknownImportedInStatistics;

                if (!param.IsIdentifiedImportedInStatistics &&
                    !param.IsAnnotatedImportedInStatistics &&
                    !param.IsUnknownImportedInStatistics) {
                    this.IsIdentifiedImportedInStatistics = true;
                    this.IsAnnotatedImportedInStatistics = true;
                    this.IsUnknownImportedInStatistics = true;
                }
            }
        }

        protected override void executeCommand(object parameter) {
            base.executeCommand(parameter);

            if (this.isIdentifiedImportedInStatistics == false &&
                this.isAnnotatedImportedInStatistics == false &&
                this.isUnknownImportedInStatistics == false) {
                MessageBox.Show("Please select at least one metabolite selection option.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            if (this.isAutoFitPls == false && this.componentNumber <= 0) {
                MessageBox.Show("For user-defined component calculations, a positive integer value should be added.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }
            var yValues = mainWindow.AnalysisFiles.
                Where(n => n.AnalysisFilePropertyBean.AnalysisFileIncluded == true).
                Select(n => n.AnalysisFilePropertyBean.ResponseVariable).ToList();

            // all same value check
            var isAllSame = true;
            for (int i = 1; i < yValues.Count; i++) {
                if (yValues[0] != yValues[i]) {
                    isAllSame = false; break;
                }
            }
            if (isAllSame) {
                MessageBox.Show("All of Y values is same. Please set Y values at file property option.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            var zeroValueCount = 0;
            if (this.plsOption == MultivariateAnalysisOption.Plsda || this.plsOption == MultivariateAnalysisOption.Oplsda) {
                for (int i = 0; i < yValues.Count; i++) {
                    if (yValues[i] == 0.0) {
                        zeroValueCount++;
                    }
                }
                if (zeroValueCount == 0 || zeroValueCount == yValues.Count) {
                    MessageBox.Show("Set zero values correctly for (O)PLS-DA.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }
            }

            if (this.mainWindow.ProjectProperty.Ionization == Ionization.ESI) {
                this.mainWindow.AnalysisParamForLC.ScalePls = this.scale;
                this.mainWindow.AnalysisParamForLC.TransformPls = this.transform;
                this.mainWindow.AnalysisParamForLC.ComponentPls = this.componentNumber;
                this.mainWindow.AnalysisParamForLC.IsAutoFitPls = this.isAutoFitPls;
                this.mainWindow.AnalysisParamForLC.MultivariateAnalysisOption = this.plsOption;
                this.mainWindow.AnalysisParamForLC.IsIdentifiedImportedInStatistics = this.isIdentifiedImportedInStatistics;
                this.mainWindow.AnalysisParamForLC.IsAnnotatedImportedInStatistics = this.isAnnotatedImportedInStatistics;
                this.mainWindow.AnalysisParamForLC.IsUnknownImportedInStatistics = this.isUnknownImportedInStatistics;
            }
            else {
                this.mainWindow.AnalysisParamForGC.ScalePls = this.scale;
                this.mainWindow.AnalysisParamForGC.TransformPls = this.transform;
                this.mainWindow.AnalysisParamForGC.ComponentPls = this.componentNumber;
                this.mainWindow.AnalysisParamForGC.IsAutoFitPls = this.isAutoFitPls;
                this.mainWindow.AnalysisParamForGC.MultivariateAnalysisOption = this.plsOption;
                this.mainWindow.AnalysisParamForGC.IsIdentifiedImportedInStatistics = this.isIdentifiedImportedInStatistics;
                this.mainWindow.AnalysisParamForGC.IsAnnotatedImportedInStatistics = this.isAnnotatedImportedInStatistics;
                this.mainWindow.AnalysisParamForGC.IsUnknownImportedInStatistics = this.isUnknownImportedInStatistics;
            }

            MsDialStatistics.PartialLeastSquares(this.mainWindow, this.scale, this.transform, this.isAutoFitPls, 
                this.componentNumber, this.isIdentifiedImportedInStatistics, this.isAnnotatedImportedInStatistics, this.isUnknownImportedInStatistics,
                this.plsOption);
            this.window.DialogResult = true;
            this.window.Close();
        }

        #region
        public int ComponentNumber {
            get { return componentNumber; }
            set { if (componentNumber == value) return; componentNumber = value; OnPropertyChanged("ComponentNumber"); }
        }

        public ObservableCollection<string> ScaleMethod {
            get { return scaleMethod; }
            set { scaleMethod = value; }
        }

        public ObservableCollection<string> TransformMethod {
            get { return transformMethod; }
            set { transformMethod = value; }
        }

        public ScaleMethod Scale {
            get { return scale; }
            set { if (scale == value) return; scale = value; OnPropertyChanged("Scale"); }
        }

        public TransformMethod Transform {
            get { return transform; }
            set { if (transform == value) return; transform = value; OnPropertyChanged("Transform"); }
        }

        public MultivariateAnalysisOption PlsOption {
            get {
                return plsOption;
            }

            set {
                if (plsOption == value) return; plsOption = value; OnPropertyChanged("PlsOption");
            }
        }

        public bool IsAutoFitPls {
            get {
                return isAutoFitPls;
            }

            set {
                if (isAutoFitPls == value)
                    return;
                isAutoFitPls = value;
                IsUserDefined = !IsAutoFitPls;
                OnPropertyChanged("IsAutoFitPls");
            }
        }

        public bool IsUserDefined {
            get {
                return isUserDefined;
            }

            set {
                if (isUserDefined == value) return; isUserDefined = value; OnPropertyChanged("IsUserDefined");
            }
        }

        public bool IsIdentifiedImportedInStatistics {
            get { return isIdentifiedImportedInStatistics; }
            set {
                if (isIdentifiedImportedInStatistics == value) return;
                isIdentifiedImportedInStatistics = value;
                OnPropertyChanged("IsIdentifiedImportedInStatistics");
            }
        }

        public bool IsAnnotatedImportedInStatistics {
            get { return isAnnotatedImportedInStatistics; }
            set {
                if (isAnnotatedImportedInStatistics == value) return;
                isAnnotatedImportedInStatistics = value;
                OnPropertyChanged("IsAnnotatedImportedInStatistics");
            }
        }

        public bool IsUnknownImportedInStatistics {
            get { return isUnknownImportedInStatistics; }
            set {
                if (isUnknownImportedInStatistics == value) return;
                isUnknownImportedInStatistics = value;
                OnPropertyChanged("IsUnknownImportedInStatistics");
            }
        }
        #endregion
    }

    public class HcaSettingVM : ViewModelBase
    {
        private MainWindow mainWindow;
        private Window window;

        //HCA
        private int maxPcNumber;
        private ScaleMethod scale;
        private TransformMethod transform;
        private ObservableCollection<string> scaleMethod;
        private ObservableCollection<string> transformMethod;
        private bool isIdentifiedImportedInStatistics;
        private bool isAnnotatedImportedInStatistics;
        private bool isUnknownImportedInStatistics;

        public HcaSettingVM(MainWindow mainWindow, Window window)
        {
            this.mainWindow = mainWindow;
            this.window = window;
            this.scaleMethod = new ObservableCollection<string>() { "None", "Mean center", "Pareto scale", "Auto scale" };
            this.transformMethod = new ObservableCollection<string>() { "None", "Log10", "Quad root" };

            if (this.mainWindow.ProjectProperty.Ionization == Ionization.ESI)
            {
                var param = mainWindow.AnalysisParamForLC;

                this.scale = param.Scale;
                this.transform = param.Transform;
                if (param.MaxComponent < 5)
                    this.maxPcNumber = 5;
                else
                    this.maxPcNumber = param.MaxComponent;

                this.IsIdentifiedImportedInStatistics = param.IsIdentifiedImportedInStatistics;
                this.IsAnnotatedImportedInStatistics = param.IsAnnotatedImportedInStatistics;
                this.IsUnknownImportedInStatistics = param.IsUnknownImportedInStatistics;

                if (!param.IsIdentifiedImportedInStatistics &&
                    !param.IsAnnotatedImportedInStatistics &&
                    !param.IsUnknownImportedInStatistics) {
                    this.IsIdentifiedImportedInStatistics = true;
                    this.IsAnnotatedImportedInStatistics = true;
                    this.IsUnknownImportedInStatistics = true;
                }
            }
            else
            {
                var param = mainWindow.AnalysisParamForGC;

                this.scale = param.Scale;
                this.transform = param.Transform;

                this.IsIdentifiedImportedInStatistics = param.IsIdentifiedImportedInStatistics;
                this.IsAnnotatedImportedInStatistics = param.IsAnnotatedImportedInStatistics;
                this.IsUnknownImportedInStatistics = param.IsUnknownImportedInStatistics;

                if (!param.IsIdentifiedImportedInStatistics &&
                  !param.IsAnnotatedImportedInStatistics &&
                  !param.IsUnknownImportedInStatistics) {
                    this.IsIdentifiedImportedInStatistics = true;
                    this.IsAnnotatedImportedInStatistics = true;
                    this.IsUnknownImportedInStatistics = true;
                }
            }
        }

        protected override void executeCommand(object parameter)
        {
            base.executeCommand(parameter);

            if (this.isIdentifiedImportedInStatistics == false &&
              this.isAnnotatedImportedInStatistics == false &&
              this.isUnknownImportedInStatistics == false) {
                MessageBox.Show("Please select at least one metabolite selection option.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            if (this.mainWindow.ProjectProperty.Ionization == Ionization.ESI)
            {
                this.mainWindow.AnalysisParamForLC.Scale = this.scale;
                this.mainWindow.AnalysisParamForLC.Transform = this.transform;
                this.mainWindow.AnalysisParamForLC.MaxComponent = this.maxPcNumber;

                this.mainWindow.AnalysisParamForLC.IsIdentifiedImportedInStatistics = this.isIdentifiedImportedInStatistics;
                this.mainWindow.AnalysisParamForLC.IsAnnotatedImportedInStatistics = this.isAnnotatedImportedInStatistics;
                this.mainWindow.AnalysisParamForLC.IsUnknownImportedInStatistics = this.isUnknownImportedInStatistics;
            }
            else
            {
                this.mainWindow.AnalysisParamForGC.Scale = this.scale;
                this.mainWindow.AnalysisParamForGC.Transform = this.transform;
                this.mainWindow.AnalysisParamForGC.MaxComponent = this.maxPcNumber;

                this.mainWindow.AnalysisParamForGC.IsIdentifiedImportedInStatistics = this.isIdentifiedImportedInStatistics;
                this.mainWindow.AnalysisParamForGC.IsAnnotatedImportedInStatistics = this.isAnnotatedImportedInStatistics;
                this.mainWindow.AnalysisParamForGC.IsUnknownImportedInStatistics = this.isUnknownImportedInStatistics;
            }

            Mouse.OverrideCursor = Cursors.Wait;
            MsDialStatistics.HierarchicalClusteringAnalysis(this.mainWindow, this.scale, this.transform, 
                this.isIdentifiedImportedInStatistics, this.isAnnotatedImportedInStatistics, this.isUnknownImportedInStatistics);
            Mouse.OverrideCursor = null;

            this.window.DialogResult = true;
            this.window.Close();
        }

        #region
        public ObservableCollection<string> ScaleMethod
        {
            get { return scaleMethod; }
            set { scaleMethod = value; }
        }

        public ObservableCollection<string> TransformMethod
        {
            get { return transformMethod; }
            set { transformMethod = value; }
        }

        public ScaleMethod Scale
        {
            get { return scale; }
            set { if (scale == value) return; scale = value; OnPropertyChanged("Scale"); }
        }

        public TransformMethod Transform
        {
            get { return transform; }
            set { if (transform == value) return; transform = value; OnPropertyChanged("Transform"); }
        }

        public bool IsIdentifiedImportedInStatistics {
            get { return isIdentifiedImportedInStatistics; }
            set { if (isIdentifiedImportedInStatistics == value) return;
                isIdentifiedImportedInStatistics = value;
                OnPropertyChanged("IsIdentifiedImportedInStatistics");
            }
        }

        public bool IsAnnotatedImportedInStatistics {
            get { return isAnnotatedImportedInStatistics; }
            set { if (isAnnotatedImportedInStatistics == value) return;
                isAnnotatedImportedInStatistics = value;
                OnPropertyChanged("IsAnnotatedImportedInStatistics");
            }
        }

        public bool IsUnknownImportedInStatistics {
            get { return isUnknownImportedInStatistics; }
            set { if (isUnknownImportedInStatistics == value) return;
                isUnknownImportedInStatistics = value;
                OnPropertyChanged("IsUnknownImportedInStatistics");
            }
        }
        #endregion
    }
}
