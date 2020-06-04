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
        public float MinPeakWidthOnRtAxis { get; set; }
        [Key(10)]
        public float AveragePeakWidthOnRtAxis { get; set; }
        [Key(11)]
        public float MedianPeakWidthOnRtAxis { get; set; }
        [Key(12)]
        public float MaxPeakWidthOnRtAxis { get; set; }
        [Key(13)]
        public float StdevPeakWidthOnRtAxis { get; set; }
        [Key(14)]
        public float MinPeakHeightOnRtAxis { get; set; }
        [Key(15)]
        public float MedianPeakHeightOnRtAxis { get; set; }
        [Key(16)]
        public float AveragePeakHeightOnRtAxis { get; set; }
        [Key(17)]
        public float MaxPeakHeightOnRtAxis { get; set; }
        [Key(18)]
        public float StdevPeakHeightOnRtAxis { get; set; }
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

        [Key(26)]
        public float MinPeakWidthOnDtAxis { get; set; }
        [Key(27)]
        public float AveragePeakWidthOnDtAxis { get; set; }
        [Key(28)]
        public float MedianPeakWidthOnDtAxis { get; set; }
        [Key(29)]
        public float MaxPeakWidthOnDtAxis { get; set; }
        [Key(30)]
        public float StdevPeakWidthOnDtAxis { get; set; }

        [Key(31)]
        public float MinPeakHeightOnDtAxis { get; set; }
        [Key(32)]
        public float MedianPeakHeightOnDtAxis { get; set; }
        [Key(33)]
        public float AveragePeakHeightOnDtAxis { get; set; }
        [Key(34)]
        public float MaxPeakHeightOnDtAxis { get; set; }
        [Key(35)]
        public float StdevPeakHeightOnDtAxis { get; set; }
        [Key(36)]
        public float MinPeakTopDT { get; set; }
        [Key(37)]
        public float AverageminPeakTopDT { get; set; }
        [Key(38)]
        public float MedianminPeakTopDT { get; set; }
        [Key(39)]
        public float MaxPeakTopDT { get; set; }
        [Key(40)]
        public float StdevPeakTopDT { get; set; }

        #endregion
    }
}
