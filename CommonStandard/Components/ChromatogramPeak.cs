using System;
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
        public ChromatogramPeak(int id, double mass, double intensity, IChromX time) {
            ID = id;
            Mass = mass;
            Intensity = intensity;
            ChromXs = new ChromXs(time);
        }

        public ChromatogramPeak(int id, double mass, double intensity, ChromXs chromXs) {
            ID = id;
            Mass = mass;
            Intensity = intensity;
            ChromXs = chromXs;
        }

        public static ChromatogramPeak Create<T>(int id, double mass, double intensity, T time) where T: IChromX {
            return new ChromatogramPeak
            {
                ID = id,
                Mass = mass,
                Intensity = intensity,
                ChromXs = ChromXs.Create(time),
            };
        }
    }
}
