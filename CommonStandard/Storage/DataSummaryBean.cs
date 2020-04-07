using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using MessagePack;

namespace Rfx.Riken.OsakaUniv
{
    [DataContract]
    [MessagePackObject]
    public class DataSummaryBean
    {
        //Data summary
        [DataMember]
        int minScanNumber;
        [DataMember]
        int maxScanNumber;
        [DataMember]
        float minRetentionTime;
        [DataMember]
        float maxRetentionTime;
        [DataMember]
        float minMass;
        [DataMember]
        float maxMass;
        [DataMember]
        float minIntensity;
        [DataMember]
        float maxIntensity;

        //Peak summary
        [DataMember]
        ModelpeakBean modelPeakBean;

        [DataMember]
        float minPeakWidth;
        [DataMember]
        float averagePeakWidth;
        [DataMember]
        float medianPeakWidth;
        [DataMember]
        float maxPeakWidth;
        [DataMember]
        float stdevPeakWidth;

        [DataMember]
        float minPeakHeight;
        [DataMember]
        float medianPeakHeight;
        [DataMember]
        float averagePeakHeight;
        [DataMember]
        float maxPeakHeight;
        [DataMember]
        float stdevPeakHeight;

        [DataMember]
        float minPeakTopRT;
        [DataMember]
        float averageminPeakTopRT;
        [DataMember]
        float medianminPeakTopRT;
        [DataMember]
        float maxPeakTopRT;
        [DataMember]
        float stdevPeakTopRT;

        //Ion mobility
        [DataMember]
        float minDriftTime;
        [DataMember]
        float maxDriftTime;

        public DataSummaryBean() { modelPeakBean = new ModelpeakBean(); }

        // property
        #region

        [Key(0)]
        public int MinScanNumber
        {
            get { return minScanNumber; }
            set { minScanNumber = value; }
        }

        [Key(1)]
        public int MaxScanNumber
        {
            get { return maxScanNumber; }
            set { maxScanNumber = value; }
        }

        [Key(2)]
        public float MinRetentionTime
        {
            get { return minRetentionTime; }
            set { minRetentionTime = value; }
        }

        [Key(3)]
        public float MaxRetentionTime
        {
            get { return maxRetentionTime; }
            set { maxRetentionTime = value; }
        }

        [Key(4)]
        public float MinMass
        {
            get { return minMass; }
            set { minMass = value; }
        }

        [Key(5)]
        public float MaxMass
        {
            get { return maxMass; }
            set { maxMass = value; }
        }

        [Key(6)]
        public float MinIntensity
        {
            get { return minIntensity; }
            set { minIntensity = value; }
        }

        [Key(7)]
        public float MaxIntensity
        {
            get { return maxIntensity; }
            set { maxIntensity = value; }
        }

        [Key(8)]
        public ModelpeakBean ModelPeakBean
        {
            get { return modelPeakBean; }
            set { modelPeakBean = value; }
        }

        [Key(9)]
        public float MinPeakWidth
        {
            get { return minPeakWidth; }
            set { minPeakWidth = value; }
        }

        [Key(10)]
        public float AveragePeakWidth
        {
            get { return averagePeakWidth; }
            set { averagePeakWidth = value; }
        }

        [Key(11)]
        public float MedianPeakWidth
        {
            get { return medianPeakWidth; }
            set { medianPeakWidth = value; }
        }

        [Key(12)]
        public float MaxPeakWidth
        {
            get { return maxPeakWidth; }
            set { maxPeakWidth = value; }
        }

        [Key(13)]
        public float StdevPeakWidth
        {
            get { return stdevPeakWidth; }
            set { stdevPeakWidth = value; }
        }

        [Key(14)]
        public float MinPeakHeight
        {
            get { return minPeakHeight; }
            set { minPeakHeight = value; }
        }

        [Key(15)]
        public float MedianPeakHeight
        {
            get { return medianPeakHeight; }
            set { medianPeakHeight = value; }
        }

        [Key(16)]
        public float AveragePeakHeight
        {
            get { return averagePeakHeight; }
            set { averagePeakHeight = value; }
        }

        [Key(17)]
        public float MaxPeakHeight
        {
            get { return maxPeakHeight; }
            set { maxPeakHeight = value; }
        }

        [Key(18)]
        public float StdevPeakHeight
        {
            get { return stdevPeakHeight; }
            set { stdevPeakHeight = value; }
        }

        [Key(19)]
        public float MinPeakTopRT
        {
            get { return minPeakTopRT; }
            set { minPeakTopRT = value; }
        }

        [Key(20)]
        public float AverageminPeakTopRT
        {
            get { return averageminPeakTopRT; }
            set { averageminPeakTopRT = value; }
        }

        [Key(21)]
        public float MedianminPeakTopRT
        {
            get { return medianminPeakTopRT; }
            set { medianminPeakTopRT = value; }
        }

        [Key(22)]
        public float MaxPeakTopRT
        {
            get { return maxPeakTopRT; }
            set { maxPeakTopRT = value; }
        }

        [Key(23)]
        public float StdevPeakTopRT
        {
            get { return stdevPeakTopRT; }
            set { stdevPeakTopRT = value; }
        }

        [Key(24)]
        public float MinDriftTime {
            get {
                return minDriftTime;
            }

            set {
                minDriftTime = value;
            }
        }

        [Key(25)]
        public float MaxDriftTime {
            get {
                return maxDriftTime;
            }

            set {
                maxDriftTime = value;
            }
        }

 

        #endregion
    }
}
