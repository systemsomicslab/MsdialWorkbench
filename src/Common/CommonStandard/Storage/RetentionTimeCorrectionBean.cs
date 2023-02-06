using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MessagePack;

namespace Rfx.Riken.OsakaUniv.RetentionTimeCorrection
{
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
        public PeakAreaBean SamplePeakAreaBean { get; set; }
        [Key(1)]
        public TextFormatCompoundInformationBean Reference { get; set; }
        [Key(2)]
        public List<double[]> Chromatogram { get; set; }

        [IgnoreMember]
        public double RtDiff { get { return (SamplePeakAreaBean.RtAtPeakTop - Reference.RetentionTime); } }
        public void Write() {
            Console.WriteLine("Name: " + Reference.MetaboliteName + ", mass diff: " + (Math.Abs(SamplePeakAreaBean.AccurateMass - Reference.AccurateMass)) +
                " Da (ref: " + Reference.AccurateMass + ", act: " + SamplePeakAreaBean.AccurateMass + "), RT diff: " +
                RtDiff + " min (ref: " + Reference.RetentionTime + ", act: " + SamplePeakAreaBean.RtAtPeakTop + ")");
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
        [Key(10)]
        public List<TextFormatCompoundInformationBean> StandardLibrary { get; set; }

        // to keep manual modification results. foreach(var sample in SampleCellInfoListList){foreach(var std in sample){Console.Write("cell info: " + cell);}}
        [Key(100)]
        public List<List<SampleListCellInfo>> SampleCellInfoListList { get; set; } = new List<List<SampleListCellInfo>>();
    }


    public class CommonStdData
    {
        public TextFormatCompoundInformationBean Reference { get; set; }
        public List<List<double[]>> Chromatograms { get; set; } = new List<List<double[]>>();
        public List<float> PeakHeightList { get; set; } = new List<float>();
        public List<float> PeakAreaList { get; set; } = new List<float>();
        public List<float> PeakWidthList { get; set; } = new List<float>();
        public List<float> MzList { get; set; } = new List<float>();
        public List<float> RetentionTimeList { get; set; } = new List<float>();
        public float AverageRetentionTime { get; set; }
        public int NumHit { get; set; } = 0;

        public CommonStdData(TextFormatCompoundInformationBean comp) {
            Reference = comp;
        }
    

        public void SetStandard(StandardPair std) {
            this.Chromatograms.Add(std.Chromatogram);
            if (std.SamplePeakAreaBean.RtAtPeakTop == 0) {
                this.PeakAreaList.Add(0);
                this.PeakHeightList.Add(0);
                this.PeakWidthList.Add(0);
                this.RetentionTimeList.Add(0);
                this.MzList.Add(0);
            }
            else {
                this.PeakAreaList.Add(std.SamplePeakAreaBean.AreaAboveZero);
                this.PeakHeightList.Add(std.SamplePeakAreaBean.IntensityAtPeakTop);
                this.PeakWidthList.Add(std.SamplePeakAreaBean.RtAtRightPeakEdge - std.SamplePeakAreaBean.RtAtLeftPeakEdge);
                this.RetentionTimeList.Add(std.SamplePeakAreaBean.RtAtPeakTop);
                this.MzList.Add(std.SamplePeakAreaBean.AccurateMass);

                this.NumHit++;
            }
        }

        public void CalcAverageRetentionTime() {
            float sum = 0;
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
