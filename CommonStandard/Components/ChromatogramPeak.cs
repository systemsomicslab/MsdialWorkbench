using System;
using System.Collections.Generic;
using System.Text;
using CompMs.Common.Interfaces;

namespace CompMs.Common.Components
{
    public class ChromatogramPeak: IChromatogramPeak
    {
        public int ID { get; set; }
        public double Mass { get; set; }
        public double Intensity { get; set; }
        public ChromXs ChromXs { get; set; }

        public ChromatogramPeak() { }
        public ChromatogramPeak(int id, double mass, double intensity, ChromX time) {
            ID = id;
            Mass = mass;
            Intensity = intensity;
            ChromXs = new ChromXs(time);
        }
    }
}
