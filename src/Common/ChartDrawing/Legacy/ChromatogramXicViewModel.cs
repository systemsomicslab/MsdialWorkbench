using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;

namespace CompMs.Graphics.Legacy {
    public class ChromatogramXicViewModelLegacy
    {
        private ChromatogramEditMode editMode;
        private ChromatogramDisplayLabel displayLabel;
        private ChromatogramQuantitativeMode quantitativeMode;
        private ChromatogramIntensityMode intensityMode;

        private ChromatogramBeanLegacy chromatogramBean;

        private int selectedPeakId;

        private float minIntensity;
        private float maxIntensity;
        private float minRt;
        private float maxRt;
        private float? displayRangeRtMin;
        private float? displayRangeRtMax;
        private float? displayRangeIntensityMin;
        private float? displayRangeIntensityMax;

        private string graphTitle;
        private float targetRt;
        private string xAxisTitle;

        private float mass;
        private float massTolerance;

        // filling peak area
        public float TargetRightRt { set; get; }
        public float TargetLeftRt { set; get; }
        public bool FillPeakArea { set; get; }

        public ChromatogramXicViewModelLegacy(ChromatogramBeanLegacy chromatogramBean, ChromatogramEditMode editMode, ChromatogramDisplayLabel displayLabel, ChromatogramQuantitativeMode quantitativeMode,
            ChromatogramIntensityMode intensityMode, int selectedPeakId, string graphTitle, float mass, float massTolerance, 
            float targetRt, float targetLeftRt, float targetRightRt, bool scaleToFocus = false, string xAxisTitle = "") {
            this.chromatogramBean = chromatogramBean;

            this.editMode = editMode;
            this.displayLabel = displayLabel;
            this.quantitativeMode = quantitativeMode;
            this.intensityMode = intensityMode;

            this.selectedPeakId = selectedPeakId;

            this.graphTitle = graphTitle;
            this.mass = mass;
            this.massTolerance = massTolerance;
            this.targetRt = targetRt;
            this.TargetLeftRt = targetLeftRt;
            this.TargetRightRt = targetRightRt;
            this.FillPeakArea = targetRt > 0 ? true : false;
            var Focus = targetRt > 0 ? true : false;
            this.XAxisTitle = xAxisTitle;

            if (targetRightRt - targetLeftRt < 0.0001) {
                this.FillPeakArea = false;
                Focus = false;
            }

            setInitialValuesByChromatogramBeanCollection(chromatogramBean, targetRt, Focus);
        }

        public ChromatogramXicViewModelLegacy(ChromatogramBeanLegacy chromatogramBean, ChromatogramEditMode editMode, ChromatogramDisplayLabel displayLabel, 
            ChromatogramQuantitativeMode quantitativeMode, ChromatogramIntensityMode intensityMode, int selectedPeakId, string graphTitle, float mass, float massTolerance, float targetRt, bool scaleToFocus = false)
        {
            this.chromatogramBean = chromatogramBean;
            
            this.editMode = editMode;
            this.displayLabel = displayLabel;
            this.quantitativeMode = quantitativeMode;
            this.intensityMode = intensityMode;
            
            this.selectedPeakId = selectedPeakId;

            this.graphTitle = graphTitle;
            this.mass = mass;
            this.massTolerance = massTolerance;
            this.targetRt = targetRt;

            setInitialValuesByChromatogramBeanCollection(chromatogramBean, targetRt, scaleToFocus);
        }

        public void setInitialValuesByChromatogramBeanCollection(ChromatogramBeanLegacy chromatogram, float targetRT, bool scaledToFocus)
        {
            this.minRt = chromatogram.MinRt;
            this.maxRt = chromatogram.MaxRt;
            this.minIntensity = chromatogram.MinIntensity;
            this.maxIntensity = chromatogram.MaxIntensity;

            if (this.minIntensity > 0)
                this.minIntensity = 0;

            this.displayRangeRtMin = this.minRt;
            this.displayRangeRtMax = this.maxRt;
            this.displayRangeIntensityMin = this.minIntensity;
            this.displayRangeIntensityMax = this.maxIntensity;

            if (scaledToFocus == true) { // chromatogram is scaled by its focused intensity
                var minDiff = double.MaxValue;
                var minID = -1;
                for (int i = 0; i < chromatogram.ChromatogramDataPointCollection.Count; i++) { // determine the target place
                    var rt = chromatogram.ChromatogramDataPointCollection[i].ChromXs.Value;
                    if (minDiff > Math.Abs(rt - targetRt)) {
                        minDiff = Math.Abs(rt - targetRt);
                        minID = i;
                    }
                }
                if (minID < 0) return;
                this.displayRangeIntensityMax = (float)chromatogram.ChromatogramDataPointCollection[minID].Intensity;
            }
        }

        #region // Properties
        public ChromatogramEditMode EditMode
        {
            get { return editMode; }
            set { editMode = value; }
        }

        public ChromatogramDisplayLabel DisplayLabel
        {
            get { return displayLabel; }
            set { displayLabel = value; }
        }

        public ChromatogramQuantitativeMode QuantitativeMode
        {
            get { return quantitativeMode; }
            set { quantitativeMode = value; }
        }

        public ChromatogramIntensityMode IntensityMode
        {
            get { return intensityMode; }
            set { intensityMode = value; }
        }

        public ChromatogramBeanLegacy ChromatogramBean
        {
            get { return chromatogramBean; }
            set { chromatogramBean = value; }
        }


        public int SelectedPeakId
        {
            get { return selectedPeakId; }
            set { if(selectedPeakId == value) return; selectedPeakId = value; OnPropertyChanged("SelectedPeakId"); }
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

        public float? DisplayRangeRtMin
        {
            get { return displayRangeRtMin; }
            set { displayRangeRtMin = value; }
        }

        public float? DisplayRangeRtMax
        {
            get { return displayRangeRtMax; }
            set { displayRangeRtMax = value; }
        }

        public float? DisplayRangeIntensityMin
        {
            get { return displayRangeIntensityMin; }
            set { displayRangeIntensityMin = value; }
        }

        public float? DisplayRangeIntensityMax
        {
            get { return displayRangeIntensityMax; }
            set { displayRangeIntensityMax = value; }
        }

        public string GraphTitle
        {
            get { return graphTitle; }
            set { graphTitle = value; }
        }

        public float Mass
        {
            get { return mass; }
            set { mass = value; }
        }

        public float MassTolerance
        {
            get { return massTolerance; }
            set { massTolerance = value; }
        }


        public float TargetRt
        {
            get { return targetRt; }
            set { targetRt = value; }
        }

        public string XAxisTitle {
            get {
                return xAxisTitle;
            }

            set {
                xAxisTitle = value;
            }
        }

        #endregion

        #region // Required Methods for INotifyPropertyChanged
        /// <summary>
        /// Occurs when a property value changes.
        /// </summary>
        public event PropertyChangedEventHandler PropertyChanged;

        /// <summary>
        /// Raises the <see cref="PropertyChanged"/> event.
        /// </summary>
        /// <param name="propertyName">Name of the property that changed.</param>
        protected void OnPropertyChanged(string propertyName)
        {
            this.OnPropertyChanged(new PropertyChangedEventArgs(propertyName));
        }

        /// <summary>
        /// Raises the <see cref="PropertyChanged"/> event.
        /// </summary>
        /// <param name="e">The <see cref="PropertyChangedEventArgs"/> instance containing the event data.</param>
        protected virtual void OnPropertyChanged(PropertyChangedEventArgs e)
        {
            PropertyChangedEventHandler eventHandlers = this.PropertyChanged;
            if (null != eventHandlers)
                eventHandlers(this, e);
        }
        #endregion // Required Methods for INotifyPropertyChanged
    
    }
}
