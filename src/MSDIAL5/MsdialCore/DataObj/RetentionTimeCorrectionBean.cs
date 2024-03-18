using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Schema;
using Accord.Statistics.Models.Fields.Learning;
using CompMs.Common.Components;
using CompMs.Common.Extension;
using CompMs.Common.Interfaces;
using CompMs.Common.Utility;
using CompMs.MsdialCore.Algorithm;
using CompMs.MsdialCore.Enum;
using MessagePack;

namespace CompMs.MsdialCore.DataObj {
    public enum InterpolationMethod { Linear }
    public enum ExtrapolationMethodBegin { UserSetting, FirstPoint, LinearExtrapolation }
    public enum ExtrapolationMethodEnd { LastPoint, LinearExtrapolation }
    public enum RtDiffCalcMethod { SampleMinusSampleAverage, SampleMinusReference }
    public enum SampleListCellInfo { Normal, Zero, ManualModified, AutoModified }

    [MessagePackObject]
    public class RetentionTimeCorrectionBean
    {
        [Key(0)]
        //public List<double> OriginalRt { get; set; }
        public List<double> OriginalRt { 
            get {
                if (originalRt.IsEmptyOrNull()) {
                    RetentionTimeCorrectionMethod.LoadRetentionCorrectionResult(this.RetentionTimeCorrectionResultFilePath, out List<double> originalRt, out List<double> rtDiff, out List<double> predictedRt);
                    if (originalRt == null) {
                        return null;
                    }
                    this.originalRt = originalRt;
                    this.rtDiff = rtDiff;
                    this.predictedRt = predictedRt;
                    
                }
                return originalRt;
            } 
        }
        private List<double> originalRt;
        [Key(1)]
        //public List<double> RtDiff { get; set; }
        public List<double> RtDiff { 
            get {
                if (rtDiff.IsEmptyOrNull()) {
                    RetentionTimeCorrectionMethod.LoadRetentionCorrectionResult(this.RetentionTimeCorrectionResultFilePath, out List<double> originalRt, out List<double> rtDiff, out List<double> predictedRt);
                    if (originalRt == null) {
                        return null;
                    }
                    this.originalRt = originalRt;
                    this.rtDiff = rtDiff;
                    this.predictedRt = predictedRt;
                }
                return rtDiff;
            } 
        }
        private List<double> rtDiff;
        [Key(2)]
        //public List<double> PredictedRt { get; set; }
        public List<double> PredictedRt {
            get {
                if (predictedRt.IsEmptyOrNull()) {
                    RetentionTimeCorrectionMethod.LoadRetentionCorrectionResult(this.RetentionTimeCorrectionResultFilePath, out List<double> originalRt, out List<double> rtDiff, out List<double> predictedRt);
                    if (originalRt == null) {
                        return null;
                    }
                    this.originalRt = originalRt;
                    this.rtDiff = rtDiff;
                    this.predictedRt = predictedRt;
                }
                return predictedRt;
            }
        }
        private List<double> predictedRt;
        [Key(3)]
        public List<StandardPair> StandardList { get; set; } = new List<StandardPair>();
        [Key(4)]
        public bool isTarget { get; set; }
        [Key(5)]
        public string RetentionTimeCorrectionResultFilePath { get; set; } = string.Empty; // *.rtc

        public RetentionTimeCorrectionBean() { }
        public RetentionTimeCorrectionBean(string retentionTimeCorrectionResultFilePath) {
            RetentionTimeCorrectionResultFilePath = retentionTimeCorrectionResultFilePath;
        }

        public RetentionTimeCorrectionBean(string retentionTimeCorrectionResultFilePath, List<double> originalRt) {
            RetentionTimeCorrectionResultFilePath = retentionTimeCorrectionResultFilePath;
            this.originalRt = originalRt;
        }

        [SerializationConstructor]
        public RetentionTimeCorrectionBean(List<double> OriginalRt, List<double> RtDiff, List<double> PredictedRt) {
            this.originalRt = OriginalRt;
            this.rtDiff = RtDiff;
            this.predictedRt = PredictedRt;
        }

        public void ClearCache() {
            this.originalRt = null;
            this.rtDiff = null;
            this.predictedRt = null;
        }
    }

    [MessagePackObject]
    public class StandardPair
    {
        [Key(0)]
        public ChromatogramPeakFeature SamplePeakAreaBean { get; set; }
        [Key(1)]
        public MoleculeMsReference Reference { get; set; }
        [Key(2)]
        public List<ChromatogramPeak> Chromatogram { get; set; }

        [IgnoreMember]
        public double RtDiff { get { return (SamplePeakAreaBean.ChromXsTop.Value - Reference.ChromXs.Value); } }
        public void Write() {
            Console.WriteLine("Name: " + Reference.Name + ", mass diff: " + (Math.Abs(SamplePeakAreaBean.PrecursorMz - Reference.PrecursorMz)) +
                " Da (ref: " + Reference.PrecursorMz + ", act: " + SamplePeakAreaBean.PrecursorMz + "), RT diff: " +
                RtDiff + " min (ref: " + Reference.ChromXs.Value + ", act: " + SamplePeakAreaBean.ChromXsTop.Value + ")");
        }
    }

    [MessagePackObject]
    public class RetentionTimeCorrectionParam {

        [Key(0)]
        public bool ExcuteRtCorrection { get; set; } = false;
        [Key(1)]
        public InterpolationMethod InterpolationMethod { get; set; }
        [Key(2)]
        public ExtrapolationMethodBegin ExtrapolationMethodBegin { get; set; }
        [Key(3)]
        public ExtrapolationMethodEnd ExtrapolationMethodEnd { get; set; }
        [Key(4)]
        public double UserSettingIntercept { get; set; } = 0.0;
        [Key(5)]
        public RtDiffCalcMethod RtDiffCalcMethod { get; set; }
        [Key(6)]
        public bool doSmoothing { get; set; }
    }

    [MessagePackObject]
    public class RetentionTimeCorrectionCommon{
        [Key(0)]
        public RetentionTimeCorrectionParam RetentionTimeCorrectionParam { get; set; } = new RetentionTimeCorrectionParam();
        [Key(1)]
        public List<MoleculeMsReference> StandardLibrary { get; set; } = new List<MoleculeMsReference>();

        // to keep manual modification results. foreach(var sample in SampleCellInfoListList){foreach(var std in sample){Console.Write("cell info: " + cell);}}
        [Key(2)]
        public List<List<SampleListCellInfo>> SampleCellInfoListList { get; set; } = new List<List<SampleListCellInfo>>();
    }


    public class CommonStdData
    {
        public MoleculeMsReference Reference { get; set; }
        public List<IReadOnlyList<IChromatogramPeak>> Chromatograms { get; set; } = new List<IReadOnlyList<IChromatogramPeak>>();
        public List<double> PeakHeightList { get; set; } = new List<double>();
        public List<double> PeakAreaList { get; set; } = new List<double>();
        public List<double> PeakWidthList { get; set; } = new List<double>();
        public List<double> MzList { get; set; } = new List<double>();
        public List<double> RetentionTimeList { get; set; } = new List<double>();
        public float AverageRetentionTime { get; set; }
        public int NumHit { get; set; } = 0;

        public CommonStdData(MoleculeMsReference comp) {
            Reference = comp;
        }
    

        public void SetStandard(StandardPair std) {
            this.Chromatograms.Add(std.Chromatogram);
            if (std.SamplePeakAreaBean.ChromXsTop.Value == 0) {
                this.PeakAreaList.Add(0);
                this.PeakHeightList.Add(0);
                this.PeakWidthList.Add(0);
                this.RetentionTimeList.Add(0);
                this.MzList.Add(0);
            }
            else {
                this.PeakAreaList.Add(std.SamplePeakAreaBean.PeakAreaAboveZero);
                this.PeakHeightList.Add(std.SamplePeakAreaBean.PeakHeightTop);
                if (std.SamplePeakAreaBean.ChromXsRight != null && std.SamplePeakAreaBean.ChromXsLeft != null)
                    this.PeakWidthList.Add(std.SamplePeakAreaBean.ChromXsRight.Value - std.SamplePeakAreaBean.ChromXsLeft.Value);
                this.RetentionTimeList.Add(std.SamplePeakAreaBean.ChromXsTop.Value);
                this.MzList.Add(std.SamplePeakAreaBean.PrecursorMz);

                this.NumHit++;
            }
        }

        public void CalcAverageRetentionTime() {
            double sum = 0;
            if(NumHit > 0) {
                foreach(var rt in RetentionTimeList.Where(x => x > 0)) { sum += rt; }
                AverageRetentionTime = (float)(sum / (float)NumHit);
            }
            else {
                AverageRetentionTime = 0;
            }
        }


    }

    public class RetentionTimeCorrectionMethod {
        public static void UpdateRtCorrectionBean(List<AnalysisFileBean> files, ParallelOptions parallelOptions, RetentionTimeCorrectionParam rtParam, List<CommonStdData> commonStdList) {
            if (rtParam.RtDiffCalcMethod == RtDiffCalcMethod.SampleMinusSampleAverage) {
                Parallel.ForEach(files, parallelOptions, f => {
                    if (f.RetentionTimeCorrectionBean.StandardList != null && f.RetentionTimeCorrectionBean.StandardList.Count > 0) {
                        var items = RetentionTimeCorrection.GetRetentionTimeCorrectionBean_SampleMinusAverage(
                            rtParam, f.RetentionTimeCorrectionBean.StandardList, f.RetentionTimeCorrectionBean.OriginalRt.ToArray(), commonStdList);
                        SaveRetentionCorrectionResult(f.RetentionTimeCorrectionBean.RetentionTimeCorrectionResultFilePath, items.originalRt, items.rtDiff, items.predictedRt);
                    }
                });
            }
            else {
                Parallel.ForEach(files, parallelOptions, f => {
                    if (f.RetentionTimeCorrectionBean.StandardList != null && f.RetentionTimeCorrectionBean.StandardList.Count > 0) {

                        var items = RetentionTimeCorrection.GetRetentionTimeCorrectionBean_SampleMinusReference(
                            rtParam, f.RetentionTimeCorrectionBean.StandardList, f.RetentionTimeCorrectionBean.OriginalRt.ToArray());

                        SaveRetentionCorrectionResult(f.RetentionTimeCorrectionBean.RetentionTimeCorrectionResultFilePath, items.originalRt, items.rtDiff, items.predictedRt);
                    }
                });
            }
        }

        public static void SaveRetentionCorrectionResult(string filepath, List<double> originalRt, List<double> rtDiff, List<double> predictedRt) {
            using (var fs = File.Open(filepath, FileMode.Create, FileAccess.ReadWrite)) {
                fs.Write(BitConverter.GetBytes(originalRt.Count), 0, ByteConvertion.ToByteCount(originalRt.Count));
                for (int i = 0; i < originalRt.Count; i++) {
                    fs.Write(BitConverter.GetBytes(originalRt[i]), 0, ByteConvertion.ToByteCount(originalRt[i]));
                    if (rtDiff.IsEmptyOrNull()) {
                        fs.Write(BitConverter.GetBytes(originalRt[i]), 0, ByteConvertion.ToByteCount(originalRt[i]));
                    }
                    else {
                        fs.Write(BitConverter.GetBytes(rtDiff[i]), 0, ByteConvertion.ToByteCount(rtDiff[i]));
                    }
                    if (predictedRt.IsEmptyOrNull()) {
                        fs.Write(BitConverter.GetBytes(originalRt[i]), 0, ByteConvertion.ToByteCount(originalRt[i]));
                    }
                    else {
                        fs.Write(BitConverter.GetBytes(predictedRt[i]), 0, ByteConvertion.ToByteCount(predictedRt[i]));
                    }
                }
            }
        }

        public static void LoadRetentionCorrectionResult(string filepath, out List<double> originalRt, out List<double> rtDiff, out List<double> predictedRt) {
            
            if (!File.Exists(filepath)) {
                originalRt = null;
                rtDiff = null;
                predictedRt = null;
                return;
            }

            originalRt = new List<double>();
            rtDiff = new List<double>();
            predictedRt = new List<double>();   

            using (var fs = File.Open(filepath, FileMode.Open, FileAccess.Read, FileShare.Read)) {
                var buffer = new byte[4];
                fs.Read(buffer, 0, 4);
                var count = BitConverter.ToInt32(buffer, 0);

                for (int i = 0; i < count; i++) {
                    buffer = new byte[24];
                    fs.Read(buffer, 0, 24);

                    originalRt.Add(BitConverter.ToDouble(buffer, 0));
                    rtDiff.Add(BitConverter.ToDouble(buffer, 8));
                    predictedRt.Add(BitConverter.ToDouble(buffer, 16));
                }
            }
        }

        public static List<CommonStdData> MakeCommonStdList(List<AnalysisFileBean> analysisFiles, List<MoleculeMsReference> iStdList) {
            var commonStdList = new List<CommonStdData>();
            var tmpStdList = iStdList.Where(x => x.IsTargetMolecule).OrderBy(x => x.ChromXs.RT.Value);
            foreach (var std in tmpStdList) {
                commonStdList.Add(new CommonStdData(std));
            }
            for (var i = 0; i < analysisFiles.Count; i++) {
                for (var j = 0; j < commonStdList.Count; j++) {
                    commonStdList[j].SetStandard(analysisFiles[i].RetentionTimeCorrectionBean.StandardList[j]);
                }
            }
            foreach (var d in commonStdList) {
                d.CalcAverageRetentionTime();
            }
            return commonStdList;
        }
    }
}
