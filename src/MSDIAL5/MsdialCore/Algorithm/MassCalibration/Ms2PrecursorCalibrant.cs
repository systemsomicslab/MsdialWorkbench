namespace CompMs.MsdialCore.Algorithm.MassCalibration;

/// <summary>
/// One MS1 feature and the ion found at its m/z in the product-ion scan chosen for it: a candidate for the
/// unfragmented remainder of the precursor, whose m/z there, against the MS1 m/z, is the error of the
/// product-ion scans at that m/z.
/// </summary>
/// <remarks>
/// A candidate, not a calibrant: in a crowded product-ion scan another ion often lies within the search
/// window. <see cref="MassErrorEstimate"/> takes the error as the mode of many candidates.
/// </remarks>
public sealed class Ms2PrecursorCalibrant
{
    public Ms2PrecursorCalibrant(
        int peakId,
        double retentionTime,
        double ms1Mz,
        double ms1Intensity,
        double ms2Mz,
        double ms2Intensity,
        int ms2ScanIndex,
        int experimentId,
        double isolationLowerMz,
        double isolationUpperMz,
        double collisionEnergy,
        double ms2ScanTotalIntensity,
        bool isotopeConfirmed) {
        PeakId = peakId;
        RetentionTime = retentionTime;
        Ms1Mz = ms1Mz;
        Ms1Intensity = ms1Intensity;
        Ms2Mz = ms2Mz;
        Ms2Intensity = ms2Intensity;
        Ms2ScanIndex = ms2ScanIndex;
        ExperimentId = experimentId;
        IsolationLowerMz = isolationLowerMz;
        IsolationUpperMz = isolationUpperMz;
        CollisionEnergy = collisionEnergy;
        Ms2ScanTotalIntensity = ms2ScanTotalIntensity;
        IsotopeConfirmed = isotopeConfirmed;
    }

    /// <summary>The feature's <see cref="DataObj.ChromatogramPeakFeature.PeakID"/>.</summary>
    public int PeakId { get; }

    /// <summary>Scan start time of the product-ion scan, in minutes.</summary>
    public double RetentionTime { get; }

    /// <summary>The feature's m/z in its MS1 peak-top scan, the reference.</summary>
    public double Ms1Mz { get; }

    public double Ms1Intensity { get; }

    /// <summary>The most intense ion within the search window around <see cref="Ms1Mz"/> in the product-ion scan.</summary>
    public double Ms2Mz { get; }

    public double Ms2Intensity { get; }

    /// <summary>Index of the product-ion scan among all spectra of the file.</summary>
    public int Ms2ScanIndex { get; }

    /// <summary>The vendor experiment of the product-ion scan; a SWATH window keeps its own.</summary>
    public int ExperimentId { get; }

    /// <summary>Lower bound of the scan's isolation window, or NaN when the scan has no precursor record.</summary>
    public double IsolationLowerMz { get; }

    /// <summary>Upper bound of the scan's isolation window, or NaN when the scan has no precursor record.</summary>
    public double IsolationUpperMz { get; }

    public double CollisionEnergy { get; }

    /// <summary>Sum of the intensities of the product-ion scan, a measure of the ion load of that scan.</summary>
    public double Ms2ScanTotalIntensity { get; }

    /// <summary>
    /// Whether the M+1 isotope was found in the MS1 scan and, shifted by the same relative amount, in the
    /// product-ion scan: the pair is then unlikely to be an unrelated ion that happens to lie near the precursor.
    /// </summary>
    public bool IsotopeConfirmed { get; }

    /// <summary>(MS2 m/z − MS1 m/z) / MS1 m/z, in ppm.</summary>
    public double ErrorPpm => (Ms2Mz - Ms1Mz) / Ms1Mz * 1e6;

    public bool HasIsolationWindow => !double.IsNaN(IsolationLowerMz) && !double.IsNaN(IsolationUpperMz);
}
