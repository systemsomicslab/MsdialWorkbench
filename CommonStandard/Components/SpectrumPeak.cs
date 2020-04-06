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
    }
}
