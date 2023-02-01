using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MessagePack.Formatters;
using MessagePack;

namespace Rfx.Riken.OsakaUniv
{
    [MessagePackObject]
    public class AnalysisParamOfMsdialCorrDec
    {
        [Key(0)]
        public int MinMS2Intensity { get; set; } = 1000;
        [Key(1)]
        public float MS2Tolerance { get; set; } = 0.01f;
        [Key(2)]
        public float MinCorr_MS1 { get; set; } = 0.9f;
        [Key(3)]
        public float MinCorr_MS2 { get; set; } = 0.7f;
        [Key(4)]
        public float CorrDiff_MS1 { get; set; } = 0.2f;
        [Key(5)]
        public float CorrDiff_MS2 { get; set; } = 0.1f;
        [Key(6)]
        public float MinDetectedPercentToVisualize { get; set; } = 0.5f;
        [Key(7)]
        public bool RemoveAfterPrecursor { get; set; } = true;
        [Key(8)]
        public int MinNumberOfSample { get; set; } = 3;
        [Key(9)]
        public float MinMS2RelativeIntensity { get; set; } = 2;
        [Key(10)]
        public bool CanExcute { get; set; } = true;
        public AnalysisParamOfMsdialCorrDec() { }
    }
}
