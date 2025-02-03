namespace CompMs.Raw.Abstractions;

[System.Flags]
public enum SpectraLoadingFlag {
    None = 0,
    MSLevel = 1,
    ExperimentID = 2,
    CollisionEnergy = 4,
    PrecursorMzRange = 8,
    ScanTimeRange = 16,
    DriftTimeRange = 32
}

public sealed class SpectraLoadingQuery
{
    public int? MSLevel { get; set; }
    public int? ExperimentID { get; set; }
    public double? CollisionEnergy { get; set; }

    public PrecursorMzRange? PrecursorMzRange { get; set; }
    public ScanTimeRange? ScanTimeRange { get; set; }
    public DriftTimeRange? DriftTimeRange { get; set; }

    public bool EnableQ1Deconvolution { get; set; } = false;

    public SpectraLoadingFlag Flags {
        get {
            var flag = SpectraLoadingFlag.None;
            if (MSLevel.HasValue) {
                flag |= SpectraLoadingFlag.MSLevel;
            }
            if (ExperimentID.HasValue) {
                flag |= SpectraLoadingFlag.ExperimentID;
            }
            if (CollisionEnergy.HasValue) {
                flag |= SpectraLoadingFlag.CollisionEnergy;
            }
            if (PrecursorMzRange is not null) {
                flag |= SpectraLoadingFlag.PrecursorMzRange;
            }
            if (ScanTimeRange is not null) {
                flag |= SpectraLoadingFlag.ScanTimeRange;
            }
            if (DriftTimeRange is not null) {
                flag |= SpectraLoadingFlag.DriftTimeRange;
            }
            return flag;
        }
    }
}
