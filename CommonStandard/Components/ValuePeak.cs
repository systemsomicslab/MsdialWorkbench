namespace CompMs.Common.Components
{
    public readonly struct ValuePeak
    {
        public ValuePeak(int id, int index, double time, double mz, double intensity) {
            Id = id;
            Index = index;
            Time = time;
            Mz = mz;
            Intensity = intensity;
        }

        public readonly int Id; // means RawSpectrum.Index
        public readonly int Index; // means index of chromatogram
        public readonly double Mz;
        public readonly double Intensity;
        public readonly double Time;
    }
}
