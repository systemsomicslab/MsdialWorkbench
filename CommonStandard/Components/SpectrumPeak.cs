using System;
using System.Collections.Generic;
using System.Text;
using CompMs.Common.Interfaces;

namespace CompMs.Common.Components
{
    public class SpectrumPeak : ISpectrumPeak
    {
        public double Mass { get; set; }
        public double Intensity { get; set; }
        public string Comment { get; set; }

        public double Resolution { get; set; }

        public SpectrumPeak() { }
        public SpectrumPeak(double mass, double intensity, string comment = null) {
            Mass = mass;
            Intensity = intensity;
            Comment = comment;
        }
    }
}
