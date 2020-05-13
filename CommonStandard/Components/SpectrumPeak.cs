using System;
using System.Collections.Generic;
using System.Text;
using CompMs.Common.Enum;
using CompMs.Common.Interfaces;
using MessagePack;

namespace CompMs.Common.Components
{
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

        public SpectrumPeak() { }
        public SpectrumPeak(double mass, double intensity, string comment = null) {
            Mass = mass;
            Intensity = intensity;
            Comment = comment;
        }
    }
}
