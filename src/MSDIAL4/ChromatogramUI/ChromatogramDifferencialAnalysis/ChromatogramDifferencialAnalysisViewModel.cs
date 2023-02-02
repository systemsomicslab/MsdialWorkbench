using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Windows.Media;

namespace Rfx.Riken.OsakaUniv
{
    public class ChromatogramDifferencialAnalysisViewModel
    {
        private ChromatogramEditMode editMode;
        private ChromatogramDisplayLabel displayLabel;
        private ChromatogramQuantitativeMode quantitativeMode;
        private ChromatogramIntensityMode intensityMode;

        private ChromatogramBean referenceChromatogramBean;
        private ObservableCollection<ChromatogramBean> sampleChromatogramBeanCollection;
        private Dictionary<string, SolidColorBrush> classId_SolidColorBrush_Dictionary;

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
        private string transitionName;
        private int transitionId;
        private string referenceFileName;
        private int referenceFileId;

        private float referenceRetentionTime;
        private float rtTolerance;

        public ChromatogramDifferencialAnalysisViewModel(ChromatogramBean referenceChromatogramBean, ObservableCollection<ChromatogramBean> sampleChromatogramBeanCollection, Dictionary<string, SolidColorBrush> classId_SolidColorBrush_Dictionary, ChromatogramEditMode editMode, ChromatogramDisplayLabel displayLabel, ChromatogramQuantitativeMode quantitativeMode, ChromatogramIntensityMode intensityMode, int selectedPeakId, string graphTitle, string transitionName, string referenceFileName, int transitionId, int referenceFileId, float referenceRetentionTime, float rtTolerance)
        {
            this.referenceChromatogramBean = referenceChromatogramBean;
            this.sampleChromatogramBeanCollection = sampleChromatogramBeanCollection;
            this.classId_SolidColorBrush_Dictionary = classId_SolidColorBrush_Dictionary;
            
            this.editMode = editMode;
            this.displayLabel = displayLabel;
            this.quantitativeMode = quantitativeMode;
            this.intensityMode = intensityMode;
            
            this.selectedPeakId = selectedPeakId;

            this.graphTitle = graphTitle;
            this.transitionName = transitionName;
            this.referenceFileName = referenceFileName;
            this.transitionId = transitionId;
            this.referenceFileId = referenceFileId;

            this.referenceRetentionTime = referenceRetentionTime;
            this.rtTolerance = rtTolerance;

            setInitialValuesByChromatogramBeanCollection(referenceChromatogramBean, sampleChromatogramBeanCollection);
        }

        private void setInitialValuesByChromatogramBeanCollection(ChromatogramBean referenceChromatogram, ObservableCollection<ChromatogramBean> sampleChromatogramBeanCollection)
        {
            this.minRt = referenceChromatogram.MinRt;
            this.maxRt = referenceChromatogram.MaxRt;
            this.minIntensity = referenceChromatogram.MinIntensity;
            this.maxIntensity = referenceChromatogram.MaxIntensity;

            for (int i = 0; i < sampleChromatogramBeanCollection.Count; i++)
            {
                if (sampleChromatogramBeanCollection[i].MinRt < this.minRt)
                    this.minRt = sampleChromatogramBeanCollection[i].MinRt;

                if (sampleChromatogramBeanCollection[i].MaxRt > this.maxRt)
                    this.maxRt = sampleChromatogramBeanCollection[i].MaxRt;

                if (sampleChromatogramBeanCollection[i].MinIntensity < this.minIntensity)
                    this.minIntensity = sampleChromatogramBeanCollection[i].MinIntensity;

                if (sampleChromatogramBeanCollection[i].MaxIntensity > this.maxIntensity)
                    this.maxIntensity = sampleChromatogramBeanCollection[i].MaxIntensity;
            }

            if (this.minIntensity > 0)
                this.minIntensity = 0;

            this.displayRangeRtMin = this.minRt;
            this.displayRangeRtMax = this.maxRt;
            this.displayRangeIntensityMin = this.minIntensity;
            this.displayRangeIntensityMax = this.maxIntensity;
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

        public ChromatogramBean ReferenceChromatogramBean
        {
            get { return referenceChromatogramBean; }
            set { referenceChromatogramBean = value; }
        }

        public ObservableCollection<ChromatogramBean> SampleChromatogramBeanCollection
        {
            get { return sampleChromatogramBeanCollection; }
            set { sampleChromatogramBeanCollection = value; }
        }

        public Dictionary<string, SolidColorBrush> ClassId_SolidColorBrush_Dictionary
        {
            get { return classId_SolidColorBrush_Dictionary; }
            set { classId_SolidColorBrush_Dictionary = value; }
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

        public string TransitionName
        {
            get { return transitionName; }
            set { transitionName = value; }
        }

        public string ReferenceFileName
        {
            get { return referenceFileName; }
            set { referenceFileName = value; }
        }

        public int ReferenceFileId
        {
            get { return referenceFileId; }
            set { referenceFileId = value; }
        }
        
        public int TransitionId
        {
            get { return transitionId; }
            set { transitionId = value; }
        }

        public float ReferenceRetentionTime
        {
            get { return referenceRetentionTime; }
            set { referenceRetentionTime = value; }
        }

        public float RtTolerance
        {
            get { return rtTolerance; }
            set { rtTolerance = value; }
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
