using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using MessagePack;

namespace CompMs.MsdialCore.DataObj {
    [MessagePackObject]
    public class ChromatogramPeaksDataSummary
    {
        // property
        #region
        [Key(0)]
        public int MinScanNumber { get; set; }
        [Key(1)]
        public int MaxScanNumber { get; set; }
        [Key(2)]
        public float MinRetentionTime { get; set; }
        [Key(3)]
        public float MaxRetentionTime { get; set; }
        [Key(4)]
        public float MinMass { get; set; }
        [Key(5)]
        public float MaxMass { get; set; }

        [Key(6)]
        public float MinIntensity { get; set; }
        [Key(7)]
        public float MaxIntensity { get; set; }
       
        [Key(9)]
        public float MinPeakWidth { get; set; }
        [Key(10)]
        public float AveragePeakWidth { get; set; }
        [Key(11)]
        public float MedianPeakWidth { get; set; }
        [Key(12)]
        public float MaxPeakWidth { get; set; }
        [Key(13)]
        public float StdevPeakWidth { get; set; }
        [Key(14)]
        public float MinPeakHeight { get; set; }
        [Key(15)]
        public float MedianPeakHeight { get; set; }
        [Key(16)]
        public float AveragePeakHeight { get; set; }
        [Key(17)]
        public float MaxPeakHeight { get; set; }
        [Key(18)]
        public float StdevPeakHeight { get; set; }
        [Key(19)]
        public float MinPeakTopRT { get; set; }
        [Key(20)]
        public float AverageminPeakTopRT { get; set; }
        [Key(21)]
        public float MedianminPeakTopRT { get; set; }
        [Key(22)]
        public float MaxPeakTopRT { get; set; }
        [Key(23)]
        public float StdevPeakTopRT { get; set; }
        [Key(24)]
        public float MinDriftTime { get; set; }
        [Key(25)]
        public float MaxDriftTime { get; set; }

        #endregion
    }
}
