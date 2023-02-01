using Msdial.Gcms.Dataprocess.Algorithm;
using Rfx.Riken.OsakaUniv.ManualPeakMod;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
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

using CompMs.Graphics.Core.Base;
using CompMs.Graphics.Chromatogram.ManualPeakModification;

namespace Rfx.Riken.OsakaUniv {
    /// <summary>
    /// Interaction logic for ChromatogramManualPeakPickViewer.xaml
    /// </summary>
    public partial class ChromatogramManualPeakPickViewer : Window {

        public ChromatogramManualPeakPickVM VM { get; set; }

        public ChromatogramManualPeakPickViewer() {
            InitializeComponent();
        }

        public ChromatogramManualPeakPickViewer(SampleTableRow selectedData, AnalysisParamOfMsdialGcms gcparam = null) {

            InitializeComponent();

            this.VM = new ChromatogramManualPeakPickVM(selectedData, gcparam);
            this.DataContext = this.VM;

            if (gcparam != null && gcparam.AlignmentIndexType == AlignmentIndexType.RI) {
                this.Label_RtTop.Content = "Retention index";
                this.Label_RtLeft.Content = "RI left";
                this.Label_RtRight.Content = "RI right";
            }
            else if (selectedData.AlignedDriftSpotPropertyBean != null) {
                this.Label_RtTop.Content = "Mobility";
                this.Label_RtLeft.Content = "Mobility left";
                this.Label_RtRight.Content = "Mobility right";
            }
        }

        private void Button_Cancel_Click(object sender, RoutedEventArgs e) {
            this.Close();
        }

        private void Button_Update_Click(object sender, RoutedEventArgs e) {
            this.DialogResult = true;

            VM.UpdateAlignedProp();

            this.Close();
        }
    }

    public class ChromatogramManualPeakPickVM : ViewModelBase {

        public SampleTableRow SelectedData { get; set; }
        public PeakModUC ChromUC { get; set; }

        //GC
        public AnalysisParamOfMsdialGcms GcParam { get; set; }
        private Dictionary<int, float> carbonRtDict;
        private Dictionary<int, float> fiehnRiDictionary;
        private FiehnRiCoefficient fiehnRiCoeff;
        private FiehnRiCoefficient revFiehnRiCoeff;

        //Ion mobility
        public bool IsIonMobility { get; set; }

        #region
        public int ScanTop {
            get { return ChromUC.drawing.ChromPeakProperty.ScanTop; }
            set { OnPropertyChanged("ScanTop"); }
        }

        public float RtTop {
            get { return ChromUC.drawing.ChromPeakProperty.RtTop; }
            set { OnPropertyChanged("RtTop"); }
        }

        public float HeightFromZero {
            get { return ChromUC.drawing.ChromPeakProperty.HeightFromZero; }
            set { OnPropertyChanged("HeightFromZero"); }
        }

        public float AreaFromZero {
            get { return ChromUC.drawing.ChromPeakProperty.AreaFromZero; }
            set { OnPropertyChanged("AreaFromZero"); }
        }

        public int ScanLeft {
            get { return ChromUC.drawing.ChromPeakProperty.ScanLeft; }
            set { OnPropertyChanged("ScanLeft"); }
        }

        public float ScanRight {
            get { return ChromUC.drawing.ChromPeakProperty.ScanRight; }
            set { OnPropertyChanged("ScanRight"); }
        }

        public float RtLeft {
            get { return ChromUC.drawing.ChromPeakProperty.RtLeft; }
            set { OnPropertyChanged("RtLeft"); }
        }

        public float RtRight {
            get { return ChromUC.drawing.ChromPeakProperty.RtRight; }
            set { OnPropertyChanged("RtRight"); }
        }

        public float HeightLeftFromZero {
            get { return ChromUC.drawing.ChromPeakProperty.HeightLeftFromZero; }
            set { OnPropertyChanged("HeightLeftFromZero"); }
        }

        public float HeightRightFromZero {
            get { return ChromUC.drawing.ChromPeakProperty.HeightRightFromZero; }
            set { OnPropertyChanged("HeightRightFromZero"); }
        }

        public float HeightFromBaseline {
            get { return ChromUC.drawing.ChromPeakProperty.HeightFromBaseline; }
            set { OnPropertyChanged("HeightFromBaseline"); }
        }

        public float HeightFromParallelBaseline {
            get { return ChromUC.drawing.ChromPeakProperty.HeightFromParallelBaseline; }
            set { OnPropertyChanged("HeightFromParallelBaseline"); }
        }

        public float AreaFromBaseline {
            get { return ChromUC.drawing.ChromPeakProperty.AreaFromBaseline; }
            set { OnPropertyChanged("AreaFromBaseline"); }
        }

        public float AreaFromParallelBaseline {
            get { return ChromUC.drawing.ChromPeakProperty.AreaFromParallelBaseline; }
            set { OnPropertyChanged("AreaFromParallelBaseline"); }
        }

        public float SignalToNoise {
            get { return ChromUC.drawing.ChromPeakProperty.SignalToNoise; }
            set { OnPropertyChanged("SignalToNoise"); }
        }
        #endregion

        public ChromatogramManualPeakPickVM(SampleTableRow selectedData, AnalysisParamOfMsdialGcms gcparam = null) {
            this.SelectedData = selectedData;
            this.GcParam = gcparam;
            this.fiehnRiDictionary = MspFileParcer.GetFiehnFamesDictionary();

            var fileID = this.SelectedData.AlignedPeakPropertyBeanCollection.FileID;
            var filename = this.SelectedData.AlignedPeakPropertyBeanCollection.FileName;
            var peaklist = this.SelectedData.ChromVM.ChromatogramBean.ChromatogramDataPointCollection;

            var rttop = this.SelectedData.AlignedPeakPropertyBeanCollection.RetentionTime;
            var rtleft = this.SelectedData.AlignedPeakPropertyBeanCollection.RetentionTimeLeft;
            var rtRight = this.SelectedData.AlignedPeakPropertyBeanCollection.RetentionTimeRight;
            var noise = this.SelectedData.AlignedPeakPropertyBeanCollection.EstimatedNoise;
            var sn = this.SelectedData.AlignedPeakPropertyBeanCollection.SignalToNoise;
            var areaFactore = 1.0F;

            if (this.SelectedData.AlignedDriftSpotPropertyBean != null)
                this.IsIonMobility = true;

            if (gcparam != null && GcParam.AlignmentIndexType == AlignmentIndexType.RI) {
                rttop = this.SelectedData.AlignedPeakPropertyBeanCollection.RetentionIndex;
                rtleft = this.SelectedData.AlignedPeakPropertyBeanCollection.RetentionIndexLeft;
                rtRight = this.SelectedData.AlignedPeakPropertyBeanCollection.RetentionIndexRight;

                areaFactore = (this.SelectedData.AlignedPeakPropertyBeanCollection.RetentionTimeRight - this.SelectedData.AlignedPeakPropertyBeanCollection.RetentionTimeLeft) / (rtRight - rtleft);

                this.carbonRtDict = GcParam.FileIdRiInfoDictionary[fileID].RiDictionary;
                if (GcParam.RiCompoundType == RiCompoundType.Fames) {
                    this.fiehnRiCoeff = FiehnRiCalculator.GetFiehnRiCoefficient(fiehnRiDictionary, GcParam.FileIdRiInfoDictionary[fileID].RiDictionary);
                    this.revFiehnRiCoeff = FiehnRiCalculator.GetFiehnRiCoefficient(GcParam.FileIdRiInfoDictionary[fileID].RiDictionary, fiehnRiDictionary);
                }
            } else if (this.IsIonMobility) {
                rttop = this.SelectedData.AlignedPeakPropertyBeanCollection.DriftTime;
                rtleft = this.SelectedData.AlignedPeakPropertyBeanCollection.DriftTimeLeft;
                rtRight = this.SelectedData.AlignedPeakPropertyBeanCollection.DriftTimeRight;
            }


            var dv = getChromatogramDrawingVisual(filename, peaklist, rttop, rtleft, rtRight, noise, sn, areaFactore);
            dv.PropertyChanged += DrawVisual_PropertyChanged;
            dv.CalculateChromPeakProperties(dv.SeriesList.Series[0]);

            this.ChromUC = new PeakModUC(dv, true);
        }

        private void DrawVisual_PropertyChanged(object sender, PropertyChangedEventArgs e) {
            if (e.PropertyName == "IsPeakPropertyChanged") {
                var dv = (DrawVisualManualPeakModification)sender;
                if (dv != null) {
                    var prop = dv.ChromPeakProperty;
                    if (prop != null) {
                        updateVM();
                    }
                }
            }
        }

        private void updateVM() {
            OnPropertyChanged("ScanTop");
            OnPropertyChanged("RtTop");
            OnPropertyChanged("HeightFromZero");
            OnPropertyChanged("AreaFromZero");
            OnPropertyChanged("ScanLeft");
            OnPropertyChanged("ScanRight");
            OnPropertyChanged("RtLeft");
            OnPropertyChanged("RtRight");
            OnPropertyChanged("HeightLeftFromZero");
            OnPropertyChanged("HeightRightFromZero");
            OnPropertyChanged("HeightFromBaseline");
            OnPropertyChanged("HeightFromParallelBaseline");
            OnPropertyChanged("AreaFromBaseline");
            OnPropertyChanged("AreaFromParallelBaseline");
            OnPropertyChanged("SignalToNoise");
        }

        private DrawVisualManualPeakModification getChromatogramDrawingVisual(string filename, 
            ObservableCollection<double[]> peaklist,
            float rtTop, float rtLeft, float rtRight, float estimatedNoise, float signalToNoise, float areaFactor) {

            var xTitle = "Retention time (min)";
            if (areaFactor != 1.0F)
                xTitle = "Retention index";

            var area = new Area() {
                AxisX = new AxisX() { AxisLabel = xTitle },
                AxisY = new AxisY() { AxisLabel = "Intensity" },
                LabelSpace = new LabelSpace(0, 10, 0, 0)
            };
            var title = new Title() { Label = "Extracted ion chromatogram" };

            var slist = new SeriesList();
            var point = new Series() {
                ChartType = ChartType.Chromatogram,
                MarkerType = MarkerType.None,
                XaxisUnit = XaxisUnit.Minuites,
                MarkerSize = new Size(7, 7),
                Pen = new Pen(Brushes.Blue, 1.0),
                Brush = Brushes.Blue
            };

            foreach (var p in peaklist) {
                point.AddPoint((float)p[1], (float)p[3]);
                Debug.WriteLine("X {0}, Y {1}", (float)p[1], (float)p[3]);
            }
            point.Accessory = new Accessory();
            point.Accessory.SetChromatogram(rtTop, rtLeft, rtRight, estimatedNoise, signalToNoise, areaFactor);
            slist.Series.Add(point);

            return new DrawVisualManualPeakModification(area, title, slist);
        }

        public void UpdateAlignedProp() {

            var prop = this.ChromUC.drawing.ChromPeakProperty;
            if (prop.RtTop <= 0) { // meaning not detected
                if (GcParam != null && GcParam.AlignmentIndexType == AlignmentIndexType.RI) {
                    var centralRI = this.SelectedData.AlignmentProperty.CentralRetentionIndex;
                    this.SelectedData.AlignedPeakPropertyBeanCollection.RetentionTime = centralRI;
                    this.SelectedData.AlignedPeakPropertyBeanCollection.RetentionTimeLeft = centralRI;
                    this.SelectedData.AlignedPeakPropertyBeanCollection.RetentionTimeRight = centralRI;
                }
                else if (this.IsIonMobility) {
                    var centralMobility = this.SelectedData.AlignedDriftSpotPropertyBean.CentralDriftTime;
                    this.SelectedData.AlignedPeakPropertyBeanCollection.DriftTime = centralMobility;
                    this.SelectedData.AlignedPeakPropertyBeanCollection.DriftTimeLeft = centralMobility;
                    this.SelectedData.AlignedPeakPropertyBeanCollection.DriftTimeRight = centralMobility;
                }
                else {
                    var centralRT = this.SelectedData.AlignmentProperty.CentralRetentionTime;
                    this.SelectedData.AlignedPeakPropertyBeanCollection.RetentionTime = centralRT;
                    this.SelectedData.AlignedPeakPropertyBeanCollection.RetentionTimeLeft = centralRT;
                    this.SelectedData.AlignedPeakPropertyBeanCollection.RetentionTimeRight = centralRT;
                }
            }
            else {
                if (GcParam != null && GcParam.AlignmentIndexType == AlignmentIndexType.RI) {
                    this.SelectedData.AlignedPeakPropertyBeanCollection.RetentionIndex = prop.RtTop;
                    this.SelectedData.AlignedPeakPropertyBeanCollection.RetentionIndexLeft = prop.RtLeft;
                    this.SelectedData.AlignedPeakPropertyBeanCollection.RetentionIndexRight = prop.RtRight;

                    var rtTop = 0.0F;
                    var rtLeft = 0.0F;
                    var rtRight = 0.0F;

                    if (GcParam.RiCompoundType == RiCompoundType.Alkanes) {
                        rtTop = GcmsScoring.ConvertKovatsRiToRetentiontime(carbonRtDict, prop.RtTop);
                        rtLeft = GcmsScoring.ConvertKovatsRiToRetentiontime(carbonRtDict, prop.RtLeft);
                        rtRight = GcmsScoring.ConvertKovatsRiToRetentiontime(carbonRtDict, prop.RtRight);
                    }
                    else {
                        rtTop = FiehnRiCalculator.ConvertFiehnRiToRetentionTime(revFiehnRiCoeff, prop.RtTop);
                        rtLeft = FiehnRiCalculator.ConvertFiehnRiToRetentionTime(revFiehnRiCoeff, prop.RtLeft);
                        rtRight = FiehnRiCalculator.ConvertFiehnRiToRetentionTime(revFiehnRiCoeff, prop.RtRight);
                    }

                    this.SelectedData.AlignedPeakPropertyBeanCollection.RetentionTime = rtTop;
                    this.SelectedData.AlignedPeakPropertyBeanCollection.RetentionTimeLeft = rtLeft;
                    this.SelectedData.AlignedPeakPropertyBeanCollection.RetentionTimeRight = rtRight;
                }
                else if (this.IsIonMobility) {
                    this.SelectedData.AlignedPeakPropertyBeanCollection.DriftTime = prop.RtTop;
                    this.SelectedData.AlignedPeakPropertyBeanCollection.DriftTimeLeft = prop.RtLeft;
                    this.SelectedData.AlignedPeakPropertyBeanCollection.DriftTimeRight = prop.RtRight;
                }
                else {
                    this.SelectedData.AlignedPeakPropertyBeanCollection.RetentionTime = prop.RtTop;
                    this.SelectedData.AlignedPeakPropertyBeanCollection.RetentionTimeLeft = prop.RtLeft;
                    this.SelectedData.AlignedPeakPropertyBeanCollection.RetentionTimeRight = prop.RtRight;
                }

            }

            this.SelectedData.AlignedPeakPropertyBeanCollection.Variable = prop.HeightFromZero;
            this.SelectedData.AlignedPeakPropertyBeanCollection.Area = prop.AreaFromZero;
            this.SelectedData.AlignedPeakPropertyBeanCollection.IsManuallyModified = true;
            this.SelectedData.AlignedPeakPropertyBeanCollection.SignalToNoise = prop.SignalToNoise;

            this.SelectedData.ChromVM.TargetRt = prop.RtTop;
            this.SelectedData.ChromVM.TargetLeftRt = prop.RtLeft;
            this.SelectedData.ChromVM.TargetRightRt = prop.RtRight;
            var isPeakDetected = prop.RtTop > 0;
            this.SelectedData.ChromVM.setInitialValuesByChromatogramBeanCollection(this.SelectedData.ChromVM.ChromatogramBean, prop.RtTop, isPeakDetected);
            this.SelectedData.Image = new PlainChromatogramXicForTableViewer(40, 200, 100, 100).DrawChromatogramXic2BitmapSource(this.SelectedData.ChromVM);
        }
    }
}
