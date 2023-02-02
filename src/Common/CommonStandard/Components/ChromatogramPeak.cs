using CompMs.Common.Interfaces;
using MessagePack;

namespace CompMs.Common.Components
{
    [MessagePackObject]
    public class ChromatogramPeak: IChromatogramPeak
    {
        [Key(0)]
        public int ID { get; } // RawSpectrum.Index

        [Key(1)]
        public double Mass { get; set; }
        [Key(2)]
        public double Intensity { get; set; }
        [Key(3)]
        public ChromXs ChromXs { get; set; }

        [SerializationConstructor]
        public ChromatogramPeak(int id, double mass, double intensity, ChromXs chromXs) {
            ID = id;
            Mass = mass;
            Intensity = intensity;
            ChromXs = chromXs;
        }

        public ChromatogramPeak(int id, double mass, double intensity, IChromX time) {
            ID = id;
            Mass = mass;
            Intensity = intensity;
            ChromXs = new ChromXs(time);
        }

        public static ChromatogramPeak Create<T>(int id, double mass, double intensity, T time) where T: IChromX {
            return new ChromatogramPeak(id, mass, intensity, ChromXs.Create(time));
        }
    }
}
