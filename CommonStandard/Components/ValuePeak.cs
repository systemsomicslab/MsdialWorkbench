namespace CompMs.Common.Components
{
    public readonly struct ValuePeak
    {
        public ValuePeak(int id, int index, int idOrIndex, double time, double mz, double intensity) {
            Id = id;
            Index = index;
            IdOrIndex = idOrIndex;
            Time = time;
            Mz = mz;
            Intensity = intensity;
        }

        public readonly int Id; // means RawSpectrum.Index
        public readonly int Index; // means index of chromatogram
        public readonly int IdOrIndex;
        public readonly double Mz;
        public readonly double Intensity;
        public readonly double Time;
    }
}
