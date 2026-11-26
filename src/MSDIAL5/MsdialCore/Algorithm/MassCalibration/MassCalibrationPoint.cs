using System;
using System.Collections.Generic;
using System.Linq;

namespace CompMs.MsdialCore.Algorithm.MassCalibration;

/// <summary>
/// One observed m/z and its error against a reference, as a mass error model takes it: where it was read (m/z, retention
/// time, isolation window), how strong it was, and which feature it belongs to.
/// </summary>
/// <remarks>
/// A candidate, not a certainty: a share of the points are unrelated ions or wrong matches, spread over the search
/// window, which the model sets aside as <see cref="MassErrorEstimate"/> does.
/// </remarks>
public sealed class MassCalibrationPoint
{
    /// <summary>The point was not read in a product-ion scan of a fixed isolation window (MS1, DDA, or unknown).</summary>
    public const int NoWindow = -1;

    /// <summary>
    /// Isolation windows narrower than this are a DDA precursor's own, not a fixed window of a DIA/SWATH method, and are
    /// given no window offset of their own.
    /// </summary>
    public const double MinimumWindowWidth = 5d;

    public MassCalibrationPoint(double mz, double errorPpm, double retentionTime, double intensity, int featureId, int window = NoWindow) {
        Mz = mz;
        ErrorPpm = errorPpm;
        RetentionTime = retentionTime;
        Intensity = intensity;
        FeatureId = featureId;
        Window = window;
    }

    /// <summary>The observed m/z: the m/z a correction would be applied to.</summary>
    public double Mz { get; }

    /// <summary>(observed − reference) / reference, in ppm.</summary>
    public double ErrorPpm { get; }

    /// <summary>In minutes.</summary>
    public double RetentionTime { get; }

    /// <summary>The intensity the m/z was read at; its scatter grows as the intensity falls.</summary>
    public double Intensity { get; }

    /// <summary>
    /// The feature the point comes from. The points of one feature (the product ions of one spectrum) are held out
    /// together when the model is cross-validated, so that none is checked against its own spectrum.
    /// </summary>
    public int FeatureId { get; }

    /// <summary>The index of the fixed isolation window the point was read in, or <see cref="NoWindow"/>.</summary>
    public int Window { get; }

    /// <summary>The precursors of the annotated features (<paramref name="msLevel"/> 1) or their product ions (2).</summary>
    public static List<MassCalibrationPoint> FromAnnotation(IEnumerable<AnnotationCalibrant> calibrants, int msLevel) {
        return FromAnnotation(calibrants, msLevel, out _);
    }

    /// <summary>
    /// The precursors of the annotated features (<paramref name="msLevel"/> 1) or their product ions (2), and the bounds
    /// of each fixed isolation window, rounded to 0.1 m/z, by the window index the points carry.
    /// </summary>
    public static List<MassCalibrationPoint> FromAnnotation(IEnumerable<AnnotationCalibrant> calibrants, int msLevel, out IReadOnlyList<(double Lower, double Upper)> windowBounds) {
        if (calibrants is null) {
            throw new ArgumentNullException(nameof(calibrants));
        }
        var selected = calibrants.Where(calibrant => calibrant.MsLevel == msLevel).ToList();
        var windows = new WindowIndex(selected.Select(calibrant => (calibrant.IsolationLowerMz, calibrant.IsolationUpperMz)));
        windowBounds = windows.Bounds;
        return selected
            .Select(calibrant => new MassCalibrationPoint(
                calibrant.ObservedMz, calibrant.ErrorPpm, calibrant.RetentionTime, calibrant.ObservedIntensity, calibrant.PeakId,
                windows.Of(calibrant.IsolationLowerMz, calibrant.IsolationUpperMz)))
            .ToList();
    }

    /// <summary>The precursors left unfragmented in the product-ion scans, against the MS1 survey.</summary>
    public static List<MassCalibrationPoint> FromPrecursorPairs(IEnumerable<Ms2PrecursorCalibrant> pairs) {
        if (pairs is null) {
            throw new ArgumentNullException(nameof(pairs));
        }
        var list = pairs.ToList();
        var windows = new WindowIndex(list.Select(pair => (pair.IsolationLowerMz, pair.IsolationUpperMz)));
        return list
            .Select(pair => new MassCalibrationPoint(
                pair.Ms2Mz, pair.ErrorPpm, pair.RetentionTime, pair.Ms2Intensity, pair.PeakId,
                windows.Of(pair.IsolationLowerMz, pair.IsolationUpperMz)))
            .ToList();
    }

    /// <summary>
    /// Numbers the fixed isolation windows by their bounds, rounded to 0.1 m/z, in m/z order; a window narrower than
    /// <see cref="MinimumWindowWidth"/>, or with a bound unknown, is <see cref="NoWindow"/>.
    /// </summary>
    internal sealed class WindowIndex
    {
        private readonly Dictionary<(double, double), int> _index;

        public WindowIndex(IEnumerable<(double Lower, double Upper)> windows) {
            _index = windows
                .Select(window => Key(window.Lower, window.Upper))
                .Where(key => key.HasValue)
                .Select(key => key!.Value)
                .Distinct()
                .OrderBy(key => key.Item1)
                .ThenBy(key => key.Item2)
                .Select((key, i) => (key, i))
                .ToDictionary(pair => pair.key, pair => pair.i);
        }

        public int Of(double lower, double upper) {
            var key = Key(lower, upper);
            return key.HasValue && _index.TryGetValue(key.Value, out var index) ? index : NoWindow;
        }

        /// <summary>The rounded bounds of each window, by index.</summary>
        public IReadOnlyList<(double Lower, double Upper)> Bounds => _index.OrderBy(pair => pair.Value).Select(pair => pair.Key).ToList();

        /// <summary>A window's bounds rounded as the index keys them, or null when it is no fixed window.</summary>
        internal static (double, double)? Key(double lower, double upper) {
            if (double.IsNaN(lower) || double.IsNaN(upper) || upper - lower < MinimumWindowWidth) {
                return null;
            }
            return (Math.Round(lower, 1), Math.Round(upper, 1));
        }
    }
}
