namespace CompMs.Common.Components
{
    public readonly struct ValuePeak
    {
        public ValuePeak(int id, double time, double mz, double intensity) {
            Id = id;
            Time = time;
            Mz = mz;
            Intensity = intensity;
        }

        public readonly int Id; // means RawSpectrum.Index
        public readonly double Mz;
        public readonly double Intensity;
        public readonly double Time;

        public ChromatogramPeak ConvertToChromatogramPeak(ChromXType type, ChromXUnit unit) {
            return new ChromatogramPeak(Id, Mz, Intensity, new ChromXs(Time, type, unit));
        }
    }
}
