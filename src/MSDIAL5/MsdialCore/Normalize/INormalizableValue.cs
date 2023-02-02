namespace CompMs.MsdialCore.Normalize
{
    public interface INormalizableValue {
        int FileID { get; }
        double PeakHeight { get; }
        double PeakAreaAboveZero { get; }
        double PeakAreaAboveBaseline { get; }
        double NormalizedPeakHeight { get; set; }
        double NormalizedPeakAreaAboveZero { get; set; }
        double NormalizedPeakAreaAboveBaseline { get; set; }
    }
}
