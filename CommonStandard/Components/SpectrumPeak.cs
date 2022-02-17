using System;
using System.Collections.Generic;
using System.Text;
using CompMs.Common.Enum;
using CompMs.Common.Interfaces;
using MessagePack;

namespace CompMs.Common.Components
{
    [Flags]
    public enum SpectrumComment {
        none = 0, 
        experiment = 1, 
        reference = 2, 
        precursor = 4, 
        b = 8, 
        y = 16, 
        b2 = 32, 
        y2 = 64, 
        b_h2o = 128, 
        y_h2o = 256,
        b_nh3 = 512, 
        y_nh3 = 1024, 
        b_h3po4 = 2048, 
        y_h3po4 = 4096, 
        tyrosinep = 8192,
        metaboliteclass = 16384, 
        acylchain = 32768,
        doublebond = 65536,
        snposition = 131072
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
        public SpectrumPeak(double mass, double intensity, string comment = null, SpectrumComment spectrumcomment = SpectrumComment.none) {
            Mass = mass;
            Intensity = intensity;
            Comment = comment;
            SpectrumComment = spectrumcomment;
        }
        
        public SpectrumPeak Clone() {
            return (SpectrumPeak)MemberwiseClone();
        }
    }
}
