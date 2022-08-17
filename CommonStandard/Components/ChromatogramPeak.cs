using System;
using CompMs.Common.Interfaces;
using MessagePack;

namespace CompMs.Common.Components
{
    [MessagePackObject]
    public class ChromatogramPeak: IChromatogramPeak
    {
        [Key(0)]
        public int Id { get; } // RawSpectrum.Index
        int IChromatogramPeak.ID => Id;
        [IgnoreMember]
        public int Index { get; } // Index of chromatogram

        [Key(1)]
        public double Mass { get; set; }
        [Key(2)]
        public double Intensity { get; set; }
        [Key(3)]
        public ChromXs ChromXs { get; set; }

        [SerializationConstructor]
        public ChromatogramPeak(int id, double mass, double intensity, ChromXs chromXs) {
            Id = id;
            Index = id;
            Mass = mass;
            Intensity = intensity;
            ChromXs = chromXs;
        }

        public ChromatogramPeak(int id, int index, double mass, double intensity, ChromXs chromXs) {
            Id = id;
            Index = index;
            Mass = mass;
            Intensity = intensity;
            ChromXs = chromXs;
        }
        public ChromatogramPeak(int id, int index, double mass, double intensity, IChromX time) {
            Id = id;
            Index = index;
            Mass = mass;
            Intensity = intensity;
            ChromXs = new ChromXs(time);
        }

        public static ChromatogramPeak Create<T>(int id, int index, double mass, double intensity, T time) where T: IChromX {
            return new ChromatogramPeak(id, index, mass, intensity, ChromXs.Create(time));
        }
    }
}
