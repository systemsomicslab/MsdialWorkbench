using CompMs.Common.DataObj.Result;

namespace CompMs.MsdialCore.Algorithm.MassCalibration;

/// <summary>
/// One observed m/z of an annotated feature and the theoretical m/z it should have read: the precursor in the MS1
/// survey, or a product ion of the deconvoluted MS/MS spectrum against a fragment of a library whose fragment m/z are
/// computed rather than measured.
/// </summary>
public sealed class AnnotationCalibrant
{
    /// <summary>The reference m/z is the formula's, through the adduct.</summary>
    public const string FromFormula = "Formula and adduct";

    /// <summary>The reference m/z is the library record's precursor m/z: the record has no usable formula or adduct.</summary>
    public const string FromLibraryPrecursor = "Library precursor m/z";

    /// <summary>The reference m/z is a fragment of a library whose fragment m/z are computed (LBM, in silico MSP).</summary>
    public const string FromLibraryFragment = "Library fragment m/z";

    public AnnotationCalibrant(
        int peakId,
        double retentionTime,
        int msLevel,
        double observedMz,
        double referenceMz,
        string referenceMzSource,
        double observedIntensity,
        double precursorMz,
        bool isPrecursorIon,
        string referenceName,
        string annotatorId,
        DataBaseSource dataBaseSource,
        double score,
        int scanIndex = -1) {
        PeakId = peakId;
        RetentionTime = retentionTime;
        MsLevel = msLevel;
        ObservedMz = observedMz;
        ReferenceMz = referenceMz;
        ReferenceMzSource = referenceMzSource;
        ObservedIntensity = observedIntensity;
        PrecursorMz = precursorMz;
        IsPrecursorIon = isPrecursorIon;
        ReferenceName = referenceName;
        AnnotatorId = annotatorId;
        DataBaseSource = dataBaseSource;
        Score = score;
        ScanIndex = scanIndex;
    }

    public int PeakId { get; }

    public double RetentionTime { get; }

    /// <summary>1 for the precursor in the MS1 survey, 2 for a product ion.</summary>
    public int MsLevel { get; }

    public double ObservedMz { get; }

    public double ReferenceMz { get; }

    /// <summary><see cref="FromFormula"/>, <see cref="FromLibraryPrecursor"/> or <see cref="FromLibraryFragment"/>.</summary>
    public string ReferenceMzSource { get; }

    /// <summary>The feature's peak height for an MS1 pair; the product ion's intensity in the deconvoluted spectrum for an MS2 pair.</summary>
    public double ObservedIntensity { get; }

    /// <summary>The feature's MS1 m/z, as observed.</summary>
    public double PrecursorMz { get; }

    /// <summary>Whether an MS2 pair is the precursor itself, left unfragmented in the product-ion scan.</summary>
    public bool IsPrecursorIon { get; }

    public string ReferenceName { get; }

    public string AnnotatorId { get; }

    public DataBaseSource DataBaseSource { get; }

    /// <summary>The score, without the precursor m/z term, by which the reference was chosen among the feature's candidates.</summary>
    public double Score { get; }

    /// <summary>
    /// Position in the file's raw spectra of the scan the m/z was read from: the MS1 scan at the feature's top, or the
    /// product-ion scan the spectrum was deconvoluted at; -1 when unknown.
    /// </summary>
    public int ScanIndex { get; }

    /// <summary>The observed m/z's error against the reference, in ppm, positive when the observed m/z is higher.</summary>
    public double ErrorPpm => (ObservedMz - ReferenceMz) / ReferenceMz * 1e6;
}
