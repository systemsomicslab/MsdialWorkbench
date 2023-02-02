using CompMs.Common.Components;
using CompMs.Common.Interfaces;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media;

namespace CompMs.Graphics.Legacy {
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

    public class ChromatogramBeanLegacy
    {
        private bool isVisible;
        private IReadOnlyList<IChromatogramPeak> Peaks;
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

        public ChromatogramBeanLegacy(bool isVisible, SolidColorBrush displayBrush, double lineTickness, string metaboliteName,
            float mz, float massTolerance, IReadOnlyList<IChromatogramPeak> Peaks) {
            this.Peaks = Peaks;
            this.isVisible = isVisible;
            this.displayBrush = displayBrush;
            this.lineTickness = lineTickness;
            this.mz = mz;
            this.massTolerance = massTolerance;
            this.metaboliteName = metaboliteName;

            SetInitializeValue();
        }

        private void SetInitializeValue()
        {
            if (this.Peaks == null || this.Peaks.Count == 0) return;

            this.minRt = (float)this.Peaks[0].ChromXs.Value;
            this.maxRt = (float)this.Peaks[this.Peaks.Count - 1].ChromXs.Value;

            float maxIntensity = float.MinValue, minIntensity = float.MaxValue;
            for (int i = 0; i < this.Peaks.Count; i++)
            {
                if (maxIntensity < this.Peaks[i].Intensity)
                    maxIntensity = (float)this.Peaks[i].Intensity;
                if (minIntensity > this.Peaks[i].Intensity)
                    minIntensity = (float)this.Peaks[i].Intensity;
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

        public IReadOnlyList<IChromatogramPeak> ChromatogramDataPointCollection
        {
            get { return Peaks; }
            set { Peaks = value; }
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
       
        #endregion
    }
}
