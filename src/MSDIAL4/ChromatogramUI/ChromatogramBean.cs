using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media;

namespace Rfx.Riken.OsakaUniv
{
    public enum ChromatogramEditMode
    {
        Edit,
        Display
    }

    public enum ChromatogramIntensityMode
    {
        Absolute,
        Relative
    }

    public enum ChromatogramZoomMode
    {
        FromZero,
        FromMousePoint
    }

    public enum ChromatogramQuantitativeMode
    {
        Height, AreaAboveZero, AreaAboveBaseline
    }

    public enum ChromatogramDisplayLabel
    {
        None,
        PeakID,
        ScanNumAtLeftPeakEdge,
        RtAtLeftPeakEdge,
        IntensityAtLeftPeakEdge,
        ScanNumAtRightPeakEdge,
        RtAtRightPeakEdge,
        IntensityAtRightPeakEdge,
        ScanNumAtPeakTop,
        RtAtPeakTop,
        IntensityAtPeakTop,
        AreaAboveZero,
        AreaAboveBaseline,
        PeakPureValue,
        ShapenessValue,
        GauusianSimilarityValue,
        IdealSlopeValue,
        BasePeakValue,
        SymmetryValue,
        AmplitudeScoreValue,
        AmplitudeOrderValue,
        RtSimilarityValue,
        AmplitudeRatioSimilatiryValue,
        PeakTopDifferencialValue,
        PeakShapeSimilarityValue,
        TotalScore, 
        ReferenceRt,
        AlignedRetentionTime,
        AnnotatedMetabolite
    }

    public class ChromatogramBean
    {
        private bool isVisible;
        private ObservableCollection<double[]> chromatogramDataPointCollection;
        private ObservableCollection<PeakAreaBean> peakAreaBeanCollection;
        private ObservableCollection<DriftSpotBean> driftSpotCollection;
        private SolidColorBrush displayBrush;
        private double lineTickness;

        private float precursorMz;
        private float productMz;
        private int selectedPeakId;
        private float minIntensity;
        private float maxIntensity;
        private float minRt;
        private float maxRt;
        private float mz;
        private int fileId;
        private string fileName;
        private string metaboliteName;
        private int metaboliteId;
        private float massTolerance;
        private string graphTitle;

        public ChromatogramBean(bool isVisible, SolidColorBrush displayBrush, string samplename, ObservableCollection<double[]> chromatogramDataPointCollection, ObservableCollection<PeakAreaBean> peakAreaBeanCollection)
        {
            this.chromatogramDataPointCollection = new ObservableCollection<double[]>();
            this.peakAreaBeanCollection = new ObservableCollection<PeakAreaBean>();

            this.isVisible = isVisible;
            this.displayBrush = displayBrush;
            this.fileName = samplename;
            this.chromatogramDataPointCollection = chromatogramDataPointCollection;
            this.lineTickness = 1.0;
           
            SetInitializeValue();
        }

        public ChromatogramBean(bool isVisible, SolidColorBrush displayBrush, double lineTickness, string samplename, float mz, ObservableCollection<double[]> chromatogramDataPointCollection, ObservableCollection<PeakAreaBean> peakAreaBeanCollection)
        {
            this.chromatogramDataPointCollection = new ObservableCollection<double[]>();
            this.peakAreaBeanCollection = new ObservableCollection<PeakAreaBean>();

            this.isVisible = isVisible;
            this.displayBrush = displayBrush;
            this.lineTickness = lineTickness;
            this.fileName = samplename;
            this.mz = mz;
            this.chromatogramDataPointCollection = chromatogramDataPointCollection;
            this.peakAreaBeanCollection = peakAreaBeanCollection;

            SetInitializeValue();
        }

        public ChromatogramBean(bool isVisible, SolidColorBrush displayBrush, double lineTickness, string samplename, float mz, ObservableCollection<double[]> chromatogramDataPointCollection, ObservableCollection<DriftSpotBean> driftSpotCollection) {
            this.chromatogramDataPointCollection = new ObservableCollection<double[]>();
            this.peakAreaBeanCollection = new ObservableCollection<PeakAreaBean>();

            this.isVisible = isVisible;
            this.displayBrush = displayBrush;
            this.lineTickness = lineTickness;
            this.fileName = samplename;
            this.mz = mz;
            this.chromatogramDataPointCollection = chromatogramDataPointCollection;
            this.driftSpotCollection = driftSpotCollection;

            SetInitializeValue();
        }

        public ChromatogramBean(bool isVisible, SolidColorBrush displayBrush, double lineTickness, float precursorMz,
            float productMz, ObservableCollection<double[]> chromatogramDataPointCollection, ObservableCollection<PeakAreaBean> peakAreaBeanCollection)
        {
            this.chromatogramDataPointCollection = new ObservableCollection<double[]>();
            this.peakAreaBeanCollection = new ObservableCollection<PeakAreaBean>();

            this.isVisible = isVisible;
            this.displayBrush = displayBrush;
            this.lineTickness = lineTickness;
            this.precursorMz = precursorMz;
            this.productMz = productMz;
            this.chromatogramDataPointCollection = chromatogramDataPointCollection;
            this.peakAreaBeanCollection = peakAreaBeanCollection;

            SetInitializeValue();
        }


        public ChromatogramBean(bool isVisible, SolidColorBrush displayBrush, double lineTickness, string metaboliteName,
            float mz, float massTolerance, ObservableCollection<double[]> chromatogramDataPointCollection)
        {
            this.chromatogramDataPointCollection = new ObservableCollection<double[]>();
            this.peakAreaBeanCollection = new ObservableCollection<PeakAreaBean>();

            this.isVisible = isVisible;
            this.displayBrush = displayBrush;
            this.lineTickness = lineTickness;
            this.mz = mz;
            this.massTolerance = massTolerance;
            this.metaboliteName = metaboliteName;
            this.chromatogramDataPointCollection = chromatogramDataPointCollection;

            SetInitializeValue();
        }

        public ChromatogramBean(bool isVisible, SolidColorBrush displayBrush, double lineTickness, string filename,
            float mz, float massTolerance, float rtTop, float rtLeft, float rtRight, bool gapfilled,
            ObservableCollection<double[]> chromatogramDataPointCollection) {
            this.chromatogramDataPointCollection = new ObservableCollection<double[]>();
            this.peakAreaBeanCollection = new ObservableCollection<PeakAreaBean>();

            this.isVisible = isVisible;
            this.displayBrush = displayBrush;
            this.lineTickness = lineTickness; if (gapfilled) this.lineTickness = 0.5;
            this.mz = mz;
            this.massTolerance = massTolerance;
            this.FileName = filename;
            this.chromatogramDataPointCollection = chromatogramDataPointCollection;
            RtPeakTop = rtTop;
            RtPeakLeft = rtLeft;
            RtPeakRight = rtRight;
            GapFilled = gapfilled;

            SetInitializeValue();
        }


        public ChromatogramBean(bool isVisible, SolidColorBrush displayBrush, double lineTickness, float precursorMz, float productMz, string fileName, int fileId, ObservableCollection<double[]> chromatogramDataPointCollection, ObservableCollection<PeakAreaBean> peakAreaBeanCollection)
        {
            this.chromatogramDataPointCollection = new ObservableCollection<double[]>();
            this.peakAreaBeanCollection = new ObservableCollection<PeakAreaBean>();

            this.isVisible = isVisible;
            this.displayBrush = displayBrush;
            this.lineTickness = lineTickness;
            this.precursorMz = precursorMz;
            this.productMz = productMz;
            this.fileName = fileName;
            this.fileId = fileId;
            this.chromatogramDataPointCollection = chromatogramDataPointCollection;
            this.peakAreaBeanCollection = peakAreaBeanCollection;

            SetInitializeValue();
        }

        private void SetInitializeValue()
        {
            if (this.chromatogramDataPointCollection == null || this.chromatogramDataPointCollection.Count == 0) return;

            this.minRt = (float)this.chromatogramDataPointCollection[0][1];
            this.maxRt = (float)this.chromatogramDataPointCollection[this.chromatogramDataPointCollection.Count - 1][1];

            float maxIntensity = float.MinValue, minIntensity = float.MaxValue;
            for (int i = 0; i < this.chromatogramDataPointCollection.Count; i++)
            {
                if (maxIntensity < this.chromatogramDataPointCollection[i][3])
                    maxIntensity = (float)this.chromatogramDataPointCollection[i][3];
                if (minIntensity > this.chromatogramDataPointCollection[i][3])
                    minIntensity = (float)this.chromatogramDataPointCollection[i][3];
            }
            this.maxIntensity = maxIntensity;
            this.minIntensity = minIntensity;
        }

        #region // properties
        public bool IsVisible
        {
            get { return isVisible; }
            set { isVisible = value; }
        }

        public double LineTickness
        {
            get { return lineTickness; }
            set { lineTickness = value; }
        }

        public string GraphTitle
        {
            get { return graphTitle; }
            set { graphTitle = value; }
        }
 
        public int SelectedPeakId
        {
            get { return selectedPeakId; }
            set { selectedPeakId = value; }
        }

        public int FileId
        {
            get { return fileId; }
            set { fileId = value; }
        }

        public string FileName
        {
            get { return fileName; }
            set { fileName = value; }
        }

        public float PrecursorMz
        {
            get { return precursorMz; }
            set { precursorMz = value; }
        }

        public float ProductMz
        {
            get { return productMz; }
            set { productMz = value; }
        }

        public SolidColorBrush DisplayBrush
        {
            get { return displayBrush; }
            set { displayBrush = value; }
        }

        public ObservableCollection<double[]> ChromatogramDataPointCollection
        {
            get { return chromatogramDataPointCollection; }
            set { chromatogramDataPointCollection = value; }
        }

        public ObservableCollection<PeakAreaBean> PeakAreaBeanCollection
        {
            get { return peakAreaBeanCollection; }
            set { peakAreaBeanCollection = value; }
        }

        public float MinIntensity
        {
            get { return minIntensity; }
            set { minIntensity = value; }
        }

        public float MaxIntensity
        {
            get { return maxIntensity; }
            set { maxIntensity = value; }
        }

        public float MinRt
        {
            get { return minRt; }
            set { minRt = value; }
        }

        public float MaxRt
        {
            get { return maxRt; }
            set { maxRt = value; }
        }

        public float Mz
        {
            get { return mz; }
            set { mz = value; }
        }

        public string MetaboliteName
        {
            get { return metaboliteName; }
            set { metaboliteName = value; }
        }

        public int MetaboliteId
        {
            get { return metaboliteId; }
            set { metaboliteId = value; }
        }

        public float MassTolerance
        {
            get { return massTolerance; }
            set { massTolerance = value; }
        }

        public float RtPeakTop { get; set; }
        public float RtPeakLeft { get; set; }
        public float RtPeakRight { get; set; }
        public bool GapFilled { get; set; }

        public ObservableCollection<DriftSpotBean> DriftSpotCollection {
            get {
                return driftSpotCollection;
            }

            set {
                driftSpotCollection = value;
            }
        }
        #endregion
    }
}
