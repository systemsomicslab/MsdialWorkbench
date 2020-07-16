using CompMs.Common.Enum;
using MessagePack;
using System;
using System.Collections.Generic;
using System.Text;

namespace CompMs.MsdialCore.DataObj {
    [MessagePackObject]
    public class AnalysisFileBean {
        [Key(0)]
        public string AnalysisFilePath { get; set; } = string.Empty;
        [Key(1)]
        public string AnalysisFileName { get; set; } = string.Empty;
        [Key(2)]
        public AnalysisFileType AnalysisFileType { get; set; }
        [Key(3)]
        public string AnalysisFileClass { get; set; } = string.Empty;
        [Key(4)]
        public int AnalysisFileAnalyticalOrder { get; set; }
        [Key(5)]
        public int AnalysisFileId { get; set; }
        [Key(6)]
        public bool AnalysisFileIncluded { get; set; }
        [Key(7)]
        public string DeconvolutionFilePath { get; set; } = string.Empty;// *.dcl
        [Key(8)]
        public List<string> DeconvolutionFilePathList { get; set; } = new List<string>(); // *.dcl
        [Key(9)]
        public string PeakAreaBeanInformationFilePath { get; set; } = string.Empty; // *.pai
        [Key(10)]
        public string RiDictionaryFilePath { get; set; } = string.Empty;
        [Key(11)]
        public int AnalysisBatch { get; set; } = 1;
        [Key(12)]
        public double ResponseVariable { get; set; } // for PLS
        [Key(13)]
        public double InjectionVolume { get; set; } = 1.9;
        [Key(14)]
        public string AnalysisFileSuperClass { get; set; } = string.Empty;
        [Key(15)]
        public RetentionTimeCorrectionBean RetentionTimeCorrectionBean { get; set; } = new RetentionTimeCorrectionBean();
        [Key(16)]
        public ChromatogramPeaksDataSummary ChromPeakFeaturesSummary { get; set; } = new ChromatogramPeaksDataSummary();
    }
}
