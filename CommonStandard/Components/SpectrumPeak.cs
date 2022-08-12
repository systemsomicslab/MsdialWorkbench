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
        y = 0x10, 
        b2 = 0x20, 
        y2 = 0x40, 
        b_h2o = 0x80, 
        y_h2o = 0x100,
        b_nh3 = 0x200, 
        y_nh3 = 0x400, 
        b_h3po4 = 0x800, 
        y_h3po4 = 0x1000, 
        tyrosinep = 0x2000,
        metaboliteclass = 0x4000, 
        acylchain = 0x8000,
        doublebond = 0x10000,
        snposition = 0x20000,
        doublebond_high = 0x40000,
        doublebond_low = 0x80000,
        c = 0x100000,
        z = 0x200000,
        c2 = 0x400000,
        z2 = 0x800000,
    }


    [MessagePackObject]
    public class SpectrumPeak : ISpectrumPeak
    {
        [Key(0)]
        public float Mass { get; set; }
        [Key(1)]
        public float Intensity { get; set; }
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
        [Key(12)]
        public bool IsAbsolutelyRequiredFragmentForAnnotation { get; set; }

        public SpectrumPeak() { }
        public SpectrumPeak(float mass, float intensity, 
            string comment = null, SpectrumComment spectrumcomment = SpectrumComment.none, bool isMust = false) {
            Mass = mass;
            Intensity = intensity;
            Comment = comment;
            SpectrumComment = spectrumcomment;
            IsAbsolutelyRequiredFragmentForAnnotation = isMust;
        }
        
        public SpectrumPeak Clone() {
            return (SpectrumPeak)MemberwiseClone();
        }

        // ISpectrumPeak interface
        double ISpectrumPeak.Mass {
            get => (double)Mass;
            set => Mass = (float)value;
        }

        double ISpectrumPeak.Intensity {
            get => (double)Intensity;
            set => Intensity = (float)value;
        }
    }
}
