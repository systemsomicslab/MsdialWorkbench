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
        private List<NeutralLoss> neutralLosses;

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
        private string fileName;
        private string formula;
        private string structureID;

        private string tryImageFile;

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

        public MassSpectrogramViewModel(MassSpectrogramBean measuredMassSpectramBean, 
            List<NeutralLoss> neutralLosses, MassSpectrogramIntensityMode intensityMode, 
            int scanNumber, float retentionTime, float targetMz, string graphTitle)
        {
            this.measuredMassSpectrogramBean = measuredMassSpectramBean;
            this.neutralLosses = neutralLosses;
            this.intensityMode = intensityMode;
            this.scanNumber = scanNumber;
            this.retentionTime = retentionTime;
            this.targetMz = targetMz;
            this.graphTitle = graphTitle;
            setInitialValuesByChromatogramBeanCollection(measuredMassSpectramBean);

            if (neutralLosses == null) return;

            var neutralPreMax = neutralLosses.Max(n => n.PrecursorMz);

            if (this.maxMass < neutralPreMax)
                this.maxMass = (float)(neutralPreMax + (neutralPreMax - this.minMass) * 0.1F);

            this.displayRangeMassMax = this.maxMass;
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

        public MassSpectrogramViewModel(MassSpectrogramBean measuredMassSpectramBean, MassSpectrogramBean referenceMassSpectrogramBean, MassSpectrogramIntensityMode intensityMode, int scanNumber, float retentionTime, string fileName, string formula, string structureID, string graphTitle)
        {
            this.measuredMassSpectrogramBean = measuredMassSpectramBean;
            this.referenceMassSpectrogramBean = referenceMassSpectrogramBean;
            this.intensityMode = intensityMode;
            this.scanNumber = scanNumber;
            this.retentionTime = retentionTime;
            this.fileName = fileName;
            this.formula = formula;
            this.structureID = structureID;
            this.graphTitle = graphTitle;
            setInitialValuesByChromatogramBeanCollection(measuredMassSpectramBean, referenceMassSpectrogramBean);
        }

        private void setInitialValuesByChromatogramBeanCollection(MassSpectrogramBean measuredMassSpectramBean, MassSpectrogramBean referenceMassSpectrogramBean)
        {
            if (measuredMassSpectramBean == null) return;

            if (referenceMassSpectrogramBean == null)
            {
                if (measuredMassSpectramBean.MaxMass - measuredMassSpectramBean.MinMass < 10) {
                    this.minMass = measuredMassSpectramBean.MinMass - 10;
                    this.maxMass = measuredMassSpectramBean.MaxMass + 10;
                }
                else {
                    this.minMass = measuredMassSpectramBean.MinMass - (measuredMassSpectramBean.MaxMass - measuredMassSpectramBean.MinMass) * 0.1F;
                    this.maxMass = measuredMassSpectramBean.MaxMass + (measuredMassSpectramBean.MaxMass - measuredMassSpectramBean.MinMass) * 0.1F;
                }
               
                this.minIntensity = 0;
                this.maxIntensity = measuredMassSpectramBean.MaxIntensity;

                this.displayRangeMassMin = this.minMass;
                this.displayRangeMassMax = this.maxMass;
                this.displayRangeIntensityMin = this.minIntensity;
                this.displayRangeIntensityMax = this.maxIntensity;

                this.minMassReference = this.minMass;
                this.maxMassReference = this.maxMass;
                this.maxIntensityReference = 100;

                this.displayRangeMassMinReference = this.minMass;
                this.displayRangeMassMaxReference = this.maxMass;
                this.displayRangeIntensityMinReference = 0;
                this.displayRangeIntensityMaxReference = 100;
            }
            else
            {
                this.minMass = Math.Min(measuredMassSpectramBean.MinMass - (measuredMassSpectramBean.MaxMass - measuredMassSpectramBean.MinMass) * 0.1F, referenceMassSpectrogramBean.MinMass - (referenceMassSpectrogramBean.MaxMass - referenceMassSpectrogramBean.MinMass) * 0.1F);
                this.maxMass = Math.Max(measuredMassSpectramBean.MaxMass + (measuredMassSpectramBean.MaxMass - measuredMassSpectramBean.MinMass) * 0.1F, referenceMassSpectrogramBean.MaxMass + (referenceMassSpectrogramBean.MaxMass - referenceMassSpectrogramBean.MinMass) * 0.1F);

                if (this.maxMass - this.minMass < 10) {
                    this.maxMass = this.maxMass + 10;
                    this.minMass = this.minMass - 10;
                }


                this.minIntensity = 0;
                this.maxIntensity = measuredMassSpectramBean.MaxIntensity;

                this.displayRangeMassMin = this.minMass;
                this.displayRangeMassMax = this.maxMass;
                this.displayRangeIntensityMin = this.minIntensity;
                this.displayRangeIntensityMax = this.maxIntensity;

                this.minMassReference = this.minMass;
                this.maxMassReference = this.maxMass;
                this.maxIntensityReference = referenceMassSpectrogramBean.MaxIntensity;

                this.displayRangeMassMinReference = this.minMass;
                this.displayRangeMassMaxReference = this.maxMass;
                this.displayRangeIntensityMinReference = this.minIntensity;
                this.displayRangeIntensityMaxReference = this.maxIntensityReference;
            }
        }

        private void setInitialValuesByChromatogramBeanCollection(MassSpectrogramBean massSpectramBean)
        {
            if (massSpectramBean == null) return;
            if (massSpectramBean.MassSpectraCollection == null) return;
            if (massSpectramBean.MassSpectraCollection.Count == 0) return;
            if (massSpectramBean.MaxMass - massSpectramBean.MinMass < 10) {
                this.minMass = massSpectramBean.MinMass - 10;
                this.maxMass = massSpectramBean.MaxMass + 10;
            }
            else {
                this.minMass = massSpectramBean.MinMass - (massSpectramBean.MaxMass - massSpectramBean.MinMass) * 0.1F;
                this.maxMass = massSpectramBean.MaxMass + (massSpectramBean.MaxMass - massSpectramBean.MinMass) * 0.1F;
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

        public List<NeutralLoss> NeutralLosses
        {
            get { return neutralLosses; }
            set { neutralLosses = value; }
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
            set { if (displayRangeIntensityMin == value) return; displayRangeMassMin = value; OnPropertyChanged("DisplayRangeMassMin"); }
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

        public string FileName
        {
            get { return fileName; }
            set { fileName = value; }
        }

        public string Formula
        {
            get { return formula; }
            set { formula = value; }
        }

        public string StructureID
        {
            get { return structureID; }
            set { structureID = value; }
        }

        public string TryImageFile
        {
            get { return tryImageFile; }
            set { tryImageFile = value; }
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
