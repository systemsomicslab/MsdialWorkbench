using System;
using System.Collections.Generic;
using System.Text;
using CompMs.Common.Enum;
using CompMs.Common.Interfaces;
using MessagePack;

namespace CompMs.Common.Components
{
    public enum SpectrumComment {
        precursor, b, y, b2, y2, b_h2o, y_h2o, b_nh3, y_nh3, b_h3po4, y_h3po4, tyrosinep   
    }


    [MessagePackObject]
    public class SpectrumPeak : ISpectrumPeak
    {
        [Key(0)]
        public double Mass { get; set; }
        [Key(1)]
        public double Intensity { get; set; }
        [Key(2)]
        public string Comment { get; set; }
        [Key(3)]
        public double Resolution { get; set; }
        [Key(4)]
        public int Charge { get; set; }
        [Key(5)]
        public bool IsotopeFrag { get; set; }
        [Key(6)]
        public PeakQuality PeakQuality { get; set; }
        [Key(7)]
        public int PeakID { get; set; }
        [Key(8)]
        public int IsotopeParentPeakID { get; set; } = -1;
        [Key(9)]
        public int IsotopeWeightNumber { get; set; } = -1;
        [Key(10)]
        public bool IsMatched { get; set; } = false;
        [Key(11)]
        public SpectrumComment SpectrumComment { get; set; }

        public SpectrumPeak() { }
        public SpectrumPeak(double mass, double intensity, string comment = null) {
            Mass = mass;
            Intensity = intensity;
            Comment = comment;
        }

        public SpectrumPeak Clone() {
            return (SpectrumPeak)MemberwiseClone();
        }
    }
}
