using CompMs.Common.Components;
using CompMs.Common.Enum;
using CompMs.Common.Interfaces;
using CompMs.Common.Utility;
using CompMs.CommonMVVM;
using CompMs.Graphics.Chromatogram.ManualPeakModification;
using CompMs.Graphics.Core.Base;
using CompMs.MsdialCore.Parameter;
using CompMs.MsdialGcMsApi.Parameter;
using System.Collections.Generic;
using System.ComponentModel;
using System.Windows;
using System.Windows.Media;

namespace CompMs.App.Msdial.View.PeakCuration
{
    /// <summary>
    /// Interaction logic for ChromatogramManualPeakPickViewerLegacy.xaml
    /// </summary>
    public partial class ChromatogramManualPeakPickViewerLegacy : Window {
        public ChromatogramManualPeakPickViewModelLegacy? VM { get; set; }

        public ChromatogramManualPeakPickViewerLegacy() {
            InitializeComponent();
        }

        public ChromatogramManualPeakPickViewerLegacy(SampleTableRow selectedData, ParameterBase param) {

            InitializeComponent();

            this.VM = new ChromatogramManualPeakPickViewModelLegacy(selectedData, param);
            this.DataContext = this.VM;

            var isRI = selectedData.AlignmentProperty.ChromXType == CompMs.Common.Components.ChromXType.RI ? true : false;
            var isDrift = selectedData.AlignmentProperty.ChromXType == CompMs.Common.Components.ChromXType.Drift ? true : false;

            if (isRI) {
                this.Label_RtTop.Content = "Retention index";
                this.Label_RtLeft.Content = "RI left";
                this.Label_RtRight.Content = "RI right";
            }
            else if (isDrift) {
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

            VM?.UpdateAlignedProp();

            this.Close();
        }
    }

    public class ChromatogramManualPeakPickViewModelLegacy : ViewModelBase {

        public SampleTableRow SelectedData { get; set; }
        public PeakModUCLegacy ChromUC { get; set; }
        public ParameterBase Param { get; set; }

        //GC
        private Dictionary<int, float>? carbonRtDict;
        private Dictionary<int, float> fiehnRiDictionary;
        private FiehnRiCoefficient? fiehnRiCoeff;
        private FiehnRiCoefficient? revFiehnRiCoeff;

        //Ion mobility
        public bool IsIonMobility { get; set; }
        public bool IsRi { get; set; }

        #region
        public int? ScanTop {
            get { return ChromUC.drawing?.ChromPeakProperty.ScanTop; }
            set { OnPropertyChanged("ScanTop"); }
        }

        public float? RtTop {
            get { return ChromUC.drawing?.ChromPeakProperty.RtTop; }
            set { OnPropertyChanged("RtTop"); }
        }

        public float? HeightFromZero {
            get { return ChromUC.drawing?.ChromPeakProperty.HeightFromZero; }
            set { OnPropertyChanged("HeightFromZero"); }
        }

        public float? AreaFromZero {
            get { return ChromUC.drawing?.ChromPeakProperty.AreaFromZero; }
            set { OnPropertyChanged("AreaFromZero"); }
        }

        public int? ScanLeft {
            get { return ChromUC.drawing?.ChromPeakProperty.ScanLeft; }
            set { OnPropertyChanged("ScanLeft"); }
        }

        public float? ScanRight {
            get { return ChromUC.drawing?.ChromPeakProperty.ScanRight; }
            set { OnPropertyChanged("ScanRight"); }
        }

        public float? RtLeft {
            get { return ChromUC.drawing?.ChromPeakProperty.RtLeft; }
            set { OnPropertyChanged("RtLeft"); }
        }

        public float? RtRight {
            get { return ChromUC.drawing?.ChromPeakProperty.RtRight; }
            set { OnPropertyChanged("RtRight"); }
        }

        public float? HeightLeftFromZero {
            get { return ChromUC.drawing?.ChromPeakProperty.HeightLeftFromZero; }
            set { OnPropertyChanged("HeightLeftFromZero"); }
        }

        public float? HeightRightFromZero {
            get { return ChromUC.drawing?.ChromPeakProperty.HeightRightFromZero; }
            set { OnPropertyChanged("HeightRightFromZero"); }
        }

        public float? HeightFromBaseline {
            get { return ChromUC.drawing?.ChromPeakProperty.HeightFromBaseline; }
            set { OnPropertyChanged("HeightFromBaseline"); }
        }

        public float? HeightFromParallelBaseline {
            get { return ChromUC.drawing?.ChromPeakProperty.HeightFromParallelBaseline; }
            set { OnPropertyChanged("HeightFromParallelBaseline"); }
        }

        public float? AreaFromBaseline {
            get { return ChromUC.drawing?.ChromPeakProperty.AreaFromBaseline; }
            set { OnPropertyChanged("AreaFromBaseline"); }
        }

        public float? AreaFromParallelBaseline {
            get { return ChromUC.drawing?.ChromPeakProperty.AreaFromParallelBaseline; }
            set { OnPropertyChanged("AreaFromParallelBaseline"); }
        }

        public float? SignalToNoise {
            get { return ChromUC.drawing?.ChromPeakProperty.SignalToNoise; }
            set { OnPropertyChanged("SignalToNoise"); }
        }
        #endregion

        public ChromatogramManualPeakPickViewModelLegacy(SampleTableRow selectedData, ParameterBase param) {
            this.SelectedData = selectedData;
            this.fiehnRiDictionary = RetentionIndexHandler.GetFiehnFamesDictionary();
            this.Param = param;

            var fileID = this.SelectedData.AlignedPeakProperty.FileID;
            var filename = this.SelectedData.AlignedPeakProperty.FileName;
            var peaklist = this.SelectedData.ChromVM.ChromatogramBean.ChromatogramDataPointCollection;

            var rttop = this.SelectedData.AlignedPeakProperty.ChromXsTop.Value;
            var rtleft = this.SelectedData.AlignedPeakProperty.ChromXsLeft.Value;
            var rtRight = this.SelectedData.AlignedPeakProperty.ChromXsRight.Value;

            var noise = this.SelectedData.AlignedPeakProperty.EstimatedNoise;
            var sn = this.SelectedData.AlignedPeakProperty.SignalToNoise;
            var areaFactore = 1.0;

            if (this.SelectedData.AlignmentProperty.IsMultiLayeredData)
                this.IsIonMobility = true;

            var isRI = this.SelectedData.AlignmentProperty.ChromXType == CompMs.Common.Components.ChromXType.RI ? true : false;
            this.IsRi = isRI;
            if (isRI) {
                rttop = this.SelectedData.AlignedPeakProperty.ChromXsTop.RI.Value;
                rtleft = this.SelectedData.AlignedPeakProperty.ChromXsLeft.RI.Value;
                rtRight = this.SelectedData.AlignedPeakProperty.ChromXsRight.RI.Value;

                areaFactore = (this.SelectedData.AlignedPeakProperty.ChromXsRight.RT.Value - this.SelectedData.AlignedPeakProperty.ChromXsLeft.RT.Value) / (rtRight - rtleft);

                this.carbonRtDict = Param.FileIdRiInfoDictionary[fileID].RiDictionary;

                if (isRI && Param.MachineCategory == MachineCategory.GCMS && ((MsdialGcmsParameter)Param).RiCompoundType == RiCompoundType.Fames && Param.FileIdRiInfoDictionary[fileID].RiDictionary is { } ridict) {
                    this.fiehnRiCoeff = RetentionIndexHandler.GetFiehnRiCoefficient(fiehnRiDictionary, ridict);
                    this.revFiehnRiCoeff = RetentionIndexHandler.GetFiehnRiCoefficient(ridict, fiehnRiDictionary);
                }
            }
            else if (this.IsIonMobility) {
                rttop = this.SelectedData.AlignedPeakProperty.ChromXsTop.Drift.Value;
                rtleft = this.SelectedData.AlignedPeakProperty.ChromXsLeft.Drift.Value;
                rtRight = this.SelectedData.AlignedPeakProperty.ChromXsRight.Drift.Value;
            }


            var dv = getChromatogramDrawingVisual(filename, peaklist, rttop, rtleft, rtRight, noise, sn, areaFactore);
            dv.PropertyChanged += DrawVisual_PropertyChanged;
            dv.CalculateChromPeakProperties(dv.SeriesList.Series[0]);

            this.ChromUC = new PeakModUCLegacy(dv, true);
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

        private DrawVisualManualPeakModification getChromatogramDrawingVisual(string filename, IReadOnlyList<IChromatogramPeak> peaklist,
            double rtTop, double rtLeft, double rtRight, double estimatedNoise, double signalToNoise, double areaFactor) {

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
                point.AddPoint((float)p.ChromXs.Value, (float)p.Intensity);
            }
            point.Accessory = new Accessory();
            point.Accessory.SetChromatogram(rtTop, rtLeft, rtRight, estimatedNoise, signalToNoise, areaFactor);
            slist.Series.Add(point);

            return new DrawVisualManualPeakModification(area, title, slist);
        }

        public void UpdateAlignedProp() {
            if (this.ChromUC.drawing?.ChromPeakProperty is null) {
                return;
            }
            var prop = this.ChromUC.drawing.ChromPeakProperty;
            var type = this.SelectedData.AlignedPeakProperty.ChromXsTop.Type;
            var unit = this.SelectedData.AlignedPeakProperty.ChromXsTop.Unit;
            var centralTime = this.SelectedData.AlignmentProperty.TimesCenter;

            if (prop.RtTop <= 0) { // meaning not detected
                this.SelectedData.AlignedPeakProperty.ChromXsTop = new ChromXs(centralTime, type, unit);
                this.SelectedData.AlignedPeakProperty.ChromXsLeft = new ChromXs(centralTime, type, unit);
                this.SelectedData.AlignedPeakProperty.ChromXsRight = new ChromXs(centralTime, type, unit);
            }
            else {
                this.SelectedData.AlignedPeakProperty.ChromXsTop = new ChromXs(prop.RtTop, type, unit);
                this.SelectedData.AlignedPeakProperty.ChromXsLeft = new ChromXs(prop.RtLeft, type, unit);
                this.SelectedData.AlignedPeakProperty.ChromXsRight = new ChromXs(prop.RtRight, type, unit);

                if (this.IsRi) {

                    var rtTop = 0.0F;
                    var rtLeft = 0.0F;
                    var rtRight = 0.0F;

                    var isFiehn = Param.MachineCategory == CompMs.Common.Enum.MachineCategory.GCMS && ((MsdialGcmsParameter)Param).RiCompoundType == RiCompoundType.Fames;

                    if (isFiehn && revFiehnRiCoeff is not null) {
                        rtTop = (float)RetentionIndexHandler.ConvertFiehnRiToRetentionTime(revFiehnRiCoeff, prop.RtTop);
                        rtLeft = (float)RetentionIndexHandler.ConvertFiehnRiToRetentionTime(revFiehnRiCoeff, prop.RtLeft);
                        rtRight = (float)RetentionIndexHandler.ConvertFiehnRiToRetentionTime(revFiehnRiCoeff, prop.RtRight);
                    }
                    else if (carbonRtDict is not null) {
                        rtTop = (float)RetentionIndexHandler.ConvertKovatsRiToRetentiontime(carbonRtDict, prop.RtTop);
                        rtLeft = (float)RetentionIndexHandler.ConvertKovatsRiToRetentiontime(carbonRtDict, prop.RtLeft);
                        rtRight = (float)RetentionIndexHandler.ConvertKovatsRiToRetentiontime(carbonRtDict, prop.RtRight);
                    }

                    this.SelectedData.AlignedPeakProperty.ChromXsTop.RT = new RetentionTime(rtTop, unit: SelectedData.AlignedPeakProperty.ChromXsTop.RT.Unit);
                    this.SelectedData.AlignedPeakProperty.ChromXsLeft.RT = new RetentionTime(rtLeft, unit: SelectedData.AlignedPeakProperty.ChromXsLeft.RT.Unit);
                    this.SelectedData.AlignedPeakProperty.ChromXsRight.RT = new RetentionTime(rtRight, unit: SelectedData.AlignedPeakProperty.ChromXsRight.RT.Unit);

                }
            }

            this.SelectedData.AlignedPeakProperty.PeakHeightTop = prop.HeightFromZero;
            this.SelectedData.AlignedPeakProperty.PeakAreaAboveZero = prop.AreaFromZero;
            this.SelectedData.AlignedPeakProperty.PeakAreaAboveBaseline = prop.AreaFromBaseline;
            this.SelectedData.AlignedPeakProperty.IsManuallyModifiedForQuant = true;
            this.SelectedData.AlignedPeakProperty.SignalToNoise = prop.SignalToNoise;

            this.SelectedData.ChromVM.TargetRt = prop.RtTop;
            this.SelectedData.ChromVM.TargetLeftRt = prop.RtLeft;
            this.SelectedData.ChromVM.TargetRightRt = prop.RtRight;
            var isPeakDetected = prop.RtTop > 0;
            this.SelectedData.ChromVM.setInitialValuesByChromatogramBeanCollection(this.SelectedData.ChromVM.ChromatogramBean, prop.RtTop, isPeakDetected);
            this.SelectedData.UpdateDrawing();
        }
    }
}
