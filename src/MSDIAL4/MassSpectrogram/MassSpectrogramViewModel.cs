using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;

namespace Rfx.Riken.OsakaUniv
{
    public class MassSpectrogramViewModel
    {
        private MassSpectrogramIntensityMode intensityMode;
        private MassSpectrogramBean measuredMassSpectrogramBean;
        private MassSpectrogramBean referenceMassSpectrogramBean;

        private int scanNumber;
        private float retentionTime;
        private float targetMz;

        private float minIntensity;
        private float maxIntensity;
        private float minMass;
        private float maxMass;
        private float? displayRangeMassMin;
        private float? displayRangeMassMax;
        private float? displayRangeIntensityMin;
        private float? displayRangeIntensityMax;

        private float minIntensityReference;
        private float maxIntensityReference;
        private float minMassReference;
        private float maxMassReference;

        private float? displayRangeMassMinReference;
        private float? displayRangeMassMaxReference;
        private float? displayRangeIntensityMinReference;
        private float? displayRangeIntensityMaxReference;

        
        private string graphTitle;

        public MassSpectrogramViewModel(MassSpectrogramBean measuredMassSpectramBean, MassSpectrogramIntensityMode intensityMode, int scanNumber, float retentionTime, float targetMz, string graphTitle)
        {
            this.measuredMassSpectrogramBean = measuredMassSpectramBean;
            this.intensityMode = intensityMode;
            this.scanNumber = scanNumber;
            this.retentionTime = retentionTime;
            this.targetMz = targetMz;
            this.graphTitle = graphTitle;
            setInitialValuesByChromatogramBeanCollection(measuredMassSpectramBean);
        }

        public MassSpectrogramViewModel(MassSpectrogramBean measuredMassSpectramBean, MassSpectrogramBean referenceMassSpectrogramBean, MassSpectrogramIntensityMode intensityMode, int scanNumber, float retentionTime, string graphTitle)
        {
            this.measuredMassSpectrogramBean = measuredMassSpectramBean;
            this.referenceMassSpectrogramBean = referenceMassSpectrogramBean;
            this.intensityMode = intensityMode;
            this.scanNumber = scanNumber;
            this.retentionTime = retentionTime;
            this.graphTitle = graphTitle;
            setInitialValuesByChromatogramBeanCollection(measuredMassSpectramBean, referenceMassSpectrogramBean);
        }

        private void setInitialValuesByChromatogramBeanCollection(MassSpectrogramBean measuredMassSpectramBean, MassSpectrogramBean referenceMassSpectrogramBean)
        {
            if (measuredMassSpectramBean == null || measuredMassSpectramBean.MassSpectraCollection == null || measuredMassSpectramBean.MassSpectraCollection.Count == 0) return;

            this.minMass = measuredMassSpectramBean.MinMass - (measuredMassSpectramBean.MaxMass - measuredMassSpectramBean.MinMass) * 0.1F;
            this.maxMass = measuredMassSpectramBean.MaxMass + (measuredMassSpectramBean.MaxMass - measuredMassSpectramBean.MinMass) * 0.1F;

            if (this.maxMass - this.minMass == 0) {
                this.maxMass += 10.0F;
                this.minMass -= 10.0F;
            }

            this.minIntensity = 0;
            this.maxIntensity = measuredMassSpectramBean.MaxIntensity;

            this.displayRangeMassMin = this.minMass;
            this.displayRangeMassMax = this.maxMass;
            this.displayRangeIntensityMin = this.minIntensity;
            this.displayRangeIntensityMax = this.maxIntensity;

            if (referenceMassSpectrogramBean == null || referenceMassSpectrogramBean.MassSpectraCollection == null || referenceMassSpectrogramBean.MassSpectraCollection.Count == 0)
            {
                this.minMassReference = this.minMass;
                this.maxMassReference = this.maxMass;
                this.minIntensity = 0;
                this.maxIntensityReference = 100;

                this.displayRangeMassMinReference = this.minMass;
                this.displayRangeMassMaxReference = this.maxMass;
                this.displayRangeIntensityMinReference = 0;
                this.displayRangeIntensityMaxReference = 100;
            }
            else
            {
                this.minMassReference = referenceMassSpectrogramBean.MinMass - (referenceMassSpectrogramBean.MaxMass - referenceMassSpectrogramBean.MinMass) * 0.1F;
                this.maxMassReference = referenceMassSpectrogramBean.MaxMass + (referenceMassSpectrogramBean.MaxMass - referenceMassSpectrogramBean.MinMass) * 0.1F;

                if (this.maxMassReference - this.minMassReference == 0) {
                    this.maxMassReference += 10.0F;
                    this.minMassReference -= 10.0F;
                }
                
                this.minIntensity = 0;
                this.maxIntensityReference = referenceMassSpectrogramBean.MaxIntensity;

                this.displayRangeMassMinReference = Math.Min(this.minMassReference, this.minMass);
                this.displayRangeMassMaxReference = Math.Max(this.maxMassReference, this.maxMass);

                this.displayRangeMassMin = Math.Min(this.minMassReference, this.minMass);
                this.displayRangeMassMax = Math.Max(this.maxMassReference, this.maxMass);

                this.displayRangeIntensityMinReference = this.minIntensity;
                this.displayRangeIntensityMaxReference = this.maxIntensityReference;
            }
        }

        private void setInitialValuesByChromatogramBeanCollection(MassSpectrogramBean massSpectramBean)
        {
            if (massSpectramBean == null || massSpectramBean.MassSpectraCollection == null || massSpectramBean.MassSpectraCollection.Count == 0) return;

            this.minMass = massSpectramBean.MinMass;
            this.maxMass = massSpectramBean.MaxMass;
            
            if (this.maxMass - this.minMass == 0) {
                this.maxMass += 10.0F;
                this.minMass -= 10.0F;
            }

            this.minIntensity = 0;
            this.maxIntensity = massSpectramBean.MaxIntensity;

            this.displayRangeMassMin = this.minMass;
            this.displayRangeMassMax = this.maxMass;
            this.displayRangeIntensityMin = this.minIntensity;
            this.displayRangeIntensityMax = this.maxIntensity;
        }

        #region
        public MassSpectrogramIntensityMode IntensityMode
        {
            get { return intensityMode; }
            set { intensityMode = value; }
        }

        public MassSpectrogramBean MeasuredMassSpectrogramBean
        {
            get { return measuredMassSpectrogramBean; }
            set { measuredMassSpectrogramBean = value; }
        }

        public MassSpectrogramBean ReferenceMassSpectrogramBean
        {
            get { return referenceMassSpectrogramBean; }
            set { referenceMassSpectrogramBean = value; }
        }

        public int ScanNumber
        {
            get { return scanNumber; }
            set { scanNumber = value; }
        }

        public float RetentionTime
        {
            get { return retentionTime; }
            set { retentionTime = value; }
        }

        public float TargetMz
        {
            get { return targetMz; }
            set { targetMz = value; }
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

        public float MinMass
        {
            get { return minMass; }
            set { minMass = value; }
        }

        public float MaxMass
        {
            get { return maxMass; }
            set { maxMass = value; }
        }

        public float? DisplayRangeMassMin
        {
            get { return displayRangeMassMin; }
            set { if (displayRangeMassMin == value) return; displayRangeMassMin = value; OnPropertyChanged("DisplayRangeMassMin"); }
        }

        public float? DisplayRangeMassMax
        {
            get { return displayRangeMassMax; }
            set { if (displayRangeMassMax == value) return; displayRangeMassMax = value; OnPropertyChanged("DisplayRangeMassMax"); }
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

        public float MinIntensityReference
        {
            get { return minIntensityReference; }
            set { minIntensityReference = value; }
        }

        public float MaxIntensityReference
        {
            get { return maxIntensityReference; }
            set { maxIntensityReference = value; }
        }

        public float MinMassReference
        {
            get { return minMassReference; }
            set { minMassReference = value; }
        }

        public float MaxMassReference
        {
            get { return maxMassReference; }
            set { maxMassReference = value; }
        }

        public float? DisplayRangeMassMinReference
        {
            get { return displayRangeMassMinReference; }
            set { displayRangeMassMinReference = value; }
        }

        public float? DisplayRangeMassMaxReference
        {
            get { return displayRangeMassMaxReference; }
            set { displayRangeMassMaxReference = value; }
        }

        public float? DisplayRangeIntensityMinReference
        {
            get { return displayRangeIntensityMinReference; }
            set { displayRangeIntensityMinReference = value; }
        }

        public float? DisplayRangeIntensityMaxReference
        {
            get { return displayRangeIntensityMaxReference; }
            set { displayRangeIntensityMaxReference = value; }
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
