using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Collections.ObjectModel;
using System.ComponentModel;

namespace Rfx.Riken.OsakaUniv
{
    public class ChromatogramMrmViewModel : INotifyPropertyChanged
    {
        private ChromatogramEditMode editMode;
        private ChromatogramDisplayLabel displayLabel;
        private ChromatogramQuantitativeMode quantitativeMode;
        private ChromatogramIntensityMode intensityMode;

        private ObservableCollection<ChromatogramBean> chromatogramBeanCollection;
        private int targetTransitionIndex;
        private int selectedPeakId;
        private int detectedPeakNumber;

        private int orderId;
        private string classId;
        private string fileType;
        
        private float minIntensity;
        private float maxIntensity;
        private float minRt;
        private float maxRt;
        private float? displayRangeRtMin;
        private float? displayRangeRtMax;
        private float? displayRangeIntensityMin;
        private float? displayRangeIntensityMax;

        private string graphTitle;
        private string fileName;
        private string metaboliteName;
        private int fileId;
        private int metaboliteId;

        private ObservableCollection<float> referenceAmplitudeRatioCollection;
        private float referenceRetentionTime;
        private float rtTolerance;
        private float amplitudeTolerance;

        public ChromatogramMrmViewModel(ObservableCollection<ChromatogramBean> chromatogramBeanCollection, ChromatogramEditMode editMode, ChromatogramDisplayLabel displayLabel, ChromatogramQuantitativeMode quantitativeMode, ChromatogramIntensityMode intensityMode, int targetTransitionIndex, int selectedPeakId, string graphTitle, int orderId, string fileType, string classId, string fileName, string metaboliteName, int fileId, int metaboliteId, float referenceRetentionTime, ObservableCollection<float> referenceAmplitudeRatioCollection, float rtTolerance, float amplitudeTolerance)
        {
            this.chromatogramBeanCollection = chromatogramBeanCollection;
            
            this.editMode = editMode;
            this.displayLabel = displayLabel;
            this.quantitativeMode = quantitativeMode;
            this.intensityMode = intensityMode;
            
            this.targetTransitionIndex = targetTransitionIndex;
            this.selectedPeakId = selectedPeakId;

            this.classId = classId;
            this.fileType = fileType;
            this.orderId = orderId;

            this.graphTitle = graphTitle;
            this.fileName = fileName;
            this.metaboliteName = metaboliteName;
            this.fileId = fileId;
            this.metaboliteId = metaboliteId;

            this.referenceAmplitudeRatioCollection = referenceAmplitudeRatioCollection;
            this.referenceRetentionTime = referenceRetentionTime;
            this.rtTolerance = rtTolerance;
            this.amplitudeTolerance = amplitudeTolerance;

            if (this.chromatogramBeanCollection == null && this.chromatogramBeanCollection[this.targetTransitionIndex].PeakAreaBeanCollection == null) this.detectedPeakNumber = this.chromatogramBeanCollection[this.targetTransitionIndex].PeakAreaBeanCollection.Count;

            setInitialValuesByChromatogramBeanCollection(chromatogramBeanCollection);
        }

        private void setInitialValuesByChromatogramBeanCollection(ObservableCollection<ChromatogramBean> chromatogramBeanCollection)
        {
            this.minRt = float.MaxValue;
            this.maxRt = float.MinValue;
            this.minIntensity = float.MaxValue;
            this.maxIntensity = float.MinValue;

            for (int i = 0; i < chromatogramBeanCollection.Count; i++)
            {
                if (chromatogramBeanCollection[i].IsVisible == false) continue;

                if (chromatogramBeanCollection[i].MinRt < this.minRt)
                    this.minRt = chromatogramBeanCollection[i].MinRt;

                if (chromatogramBeanCollection[i].MaxRt > this.maxRt)
                    this.maxRt = chromatogramBeanCollection[i].MaxRt;

                if (chromatogramBeanCollection[i].MinIntensity < this.minIntensity)
                    this.minIntensity = chromatogramBeanCollection[i].MinIntensity;

                if (chromatogramBeanCollection[i].MaxIntensity > this.maxIntensity)
                    this.maxIntensity = chromatogramBeanCollection[i].MaxIntensity;
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

        public ObservableCollection<ChromatogramBean> ChromatogramBeanCollection
        {
            get { return chromatogramBeanCollection; }
            set { chromatogramBeanCollection = value; }
        }

        public int TargetTransitionIndex
        {
            get { return targetTransitionIndex; }
            set { if (targetTransitionIndex == value) return; targetTransitionIndex = value; OnPropertyChanged("TargetTransitionIndex"); }
        }

        public int SelectedPeakId
        {
            get { return selectedPeakId; }
            set { if(selectedPeakId == value) return; selectedPeakId = value; OnPropertyChanged("SelectedPeakId"); }
        }

        public int DetectedPeakNumber
        {
            get { return detectedPeakNumber; }
            set 
            { 
                if (this.chromatogramBeanCollection == null) return; 
                if (this.chromatogramBeanCollection[this.targetTransitionIndex].PeakAreaBeanCollection == null) return;
                if (this.chromatogramBeanCollection[this.targetTransitionIndex].PeakAreaBeanCollection.Count == value) return;
                detectedPeakNumber = value;
                OnPropertyChanged("DetectedPeakNumber");
            }
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

        public int OrderId
        {
            get { return orderId; }
            set { orderId = value; }
        }

        public string ClassId
        {
            get { return classId; }
            set { classId = value; }
        }

        public string FileType
        {
            get { return fileType; }
            set { fileType = value; }
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

        public string FileName
        {
            get { return fileName; }
            set { fileName = value; }
        }

        public string MetaboliteName
        {
            get { return metaboliteName; }
            set { metaboliteName = value; }
        }


        public int FileId
        {
            get { return fileId; }
            set { fileId = value; }
        }

        public int MetaboliteId
        {
            get { return metaboliteId; }
            set { metaboliteId = value; }
        }


        public ObservableCollection<float> ReferenceAmplitudeRatioCollection
        {
            get { return referenceAmplitudeRatioCollection; }
            set { referenceAmplitudeRatioCollection = value; }
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

        public float AmplitudeTolerance
        {
            get { return amplitudeTolerance; }
            set { amplitudeTolerance = value; }
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
