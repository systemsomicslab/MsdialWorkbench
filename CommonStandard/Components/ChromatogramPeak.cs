using System;
using System.Collections.Generic;
using System.Text;
using CompMs.Common.Interfaces;
using MessagePack;

namespace CompMs.Common.Components
{
    [MessagePackObject]
    public class ChromatogramPeak: IChromatogramPeak
    {
        [Key(0)]
        public int ID { get; set; }
        [Key(1)]
        public double Mass { get; set; }
        [Key(2)]
        public double Intensity { get; set; }
        [Key(3)]
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
