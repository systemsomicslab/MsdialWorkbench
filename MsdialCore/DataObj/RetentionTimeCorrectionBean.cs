using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CompMs.Common.Components;
using CompMs.Common.Interfaces;
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
        public List<double> OriginalRt { get; set; }
        [Key(1)]
        public List<double> RtDiff { get; set; }
        [Key(2)]
        public List<double> PredictedRt { get; set; }
        [Key(3)]
        public List<StandardPair> StandardList { get; set; } = new List<StandardPair>();
        [Key(4)]
        public bool isTarget { get; set; }

        public RetentionTimeCorrectionBean() { }


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
}
