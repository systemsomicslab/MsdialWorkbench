namespace CompMs.Common.Components
{
    public struct ValuePeak
    {
        public ValuePeak(int id, double time, double mz, double intensity) {
            Id = id;
            Time = time;
            Mz = mz;
            Intensity = intensity;
        }

        public int Id;
        public double Mz;
        public double Intensity;
        public double Time;
    }
}
