using CompMs.Common.DataObj;
using CompMs.MsdialCore.Algorithm.MassCalibration;
using MessagePack;
using System;
using System.Collections.Generic;
using System.Linq;

namespace CompMs.MsdialCore.DataObj;

/// <summary>
/// The m/z recalibration of one analysis file: the error model of its MS1 survey and that of its product-ion scans,
/// chosen when the file's peaks were picked, and taken out of its spectra whenever they are read, so that the peak
/// picking, deconvolution, annotation, alignment and gap filling, and the spectra shown, all read the recalibrated m/z.
/// </summary>
/// <remarks>
/// A level without a model is read as recorded. A file without a recalibration has no bean at all
/// (<see cref="AnalysisFileBean.MassRecalibration"/> is null), which is also how a project written before this class
/// existed reads.
/// </remarks>
[MessagePackObject]
public sealed class MassRecalibrationBean
{
    /// <summary>The model of the MS1 survey, or null when the MS1 m/z are read as recorded.</summary>
    [Key(0)]
    public MassErrorModelBean? Ms1 { get; set; }

    /// <summary>The model of the product-ion scans, or null when their m/z are read as recorded.</summary>
    [Key(1)]
    public MassErrorModelBean? Ms2 { get; set; }

    /// <summary><see cref="MassCalibrationDecision.AlgorithmVersion"/> of the rule that chose the models.</summary>
    [Key(2)]
    public string AlgorithmVersion { get; set; } = string.Empty;

    [IgnoreMember]
    public bool IsEmpty => Ms1 is null && Ms2 is null;

    /// <summary>
    /// Takes the modelled error out of the spectra's m/z, in place: every MS1 scan by the MS1 model, every product-ion
    /// scan by the MS2 model (with the offset of its isolation window), and the precursor m/z of a product-ion scan with
    /// a narrow isolation window (a DDA precursor, which the survey measured) by the MS1 model. The fixed windows of a
    /// DIA/SWATH method are settings, not measurements, and are left as they are.
    /// </summary>
    public void ApplyTo(IEnumerable<RawSpectrum> spectra) {
        if (spectra is null) {
            throw new ArgumentNullException(nameof(spectra));
        }
        if (IsEmpty) {
            return;
        }
        var ms2Windows = Ms2?.WindowOffsets();
        foreach (var spectrum in spectra) {
            if (spectrum is null) {
                continue;
            }
            var retentionTime = spectrum.ScanStartTime;
            if (spectrum.MsLevel <= 1) {
                if (Ms1 is not null) {
                    Correct(spectrum, Ms1, retentionTime, 0d);
                }
                continue;
            }
            var (lower, upper) = PrecursorCalibrantCollector.IsolationWindow(spectrum.Precursor);
            var window = MassCalibrationPoint.WindowIndex.Key(lower, upper);
            if (Ms2 is not null) {
                var offset = window.HasValue && ms2Windows!.TryGetValue(window.Value, out var windowOffset) ? windowOffset : 0d;
                Correct(spectrum, Ms2, retentionTime, offset);
            }
            if (Ms1 is not null && spectrum.Precursor is { } precursor && !window.HasValue) {
                if (precursor.SelectedIonMz > 0d) {
                    precursor.SelectedIonMz = Ms1.CorrectMz(precursor.SelectedIonMz, retentionTime, 0d);
                }
                if (precursor.IsolationTargetMz > 0d) {
                    precursor.IsolationTargetMz = Ms1.CorrectMz(precursor.IsolationTargetMz, retentionTime, 0d);
                }
            }
        }
    }

    private static void Correct(RawSpectrum spectrum, MassErrorModelBean model, double retentionTime, double windowOffset) {
        var peaks = spectrum.Spectrum;
        if (peaks is null || peaks.Length == 0) {
            return;
        }
        var sorted = true;
        for (var i = 0; i < peaks.Length; i++) {
            peaks[i].Mz = model.CorrectMz(peaks[i].Mz, retentionTime, windowOffset);
            if (i > 0 && peaks[i].Mz < peaks[i - 1].Mz) {
                sorted = false;
            }
        }
        if (!sorted) {
            Array.Sort(peaks, (a, b) => a.Mz.CompareTo(b.Mz));
        }
        if (spectrum.BasePeakMz > 0d) {
            spectrum.BasePeakMz = model.CorrectMz(spectrum.BasePeakMz, retentionTime, windowOffset);
        }
        if (spectrum.LowestObservedMz > 0d) {
            spectrum.LowestObservedMz = model.CorrectMz(spectrum.LowestObservedMz, retentionTime, windowOffset);
        }
        if (spectrum.HighestObservedMz > 0d) {
            spectrum.HighestObservedMz = model.CorrectMz(spectrum.HighestObservedMz, retentionTime, windowOffset);
        }
    }
}

/// <summary>
/// A mass error model as recorded with a file: <see cref="MassErrorModel"/>'s terms, coefficients and range, and the
/// offset of each fixed isolation window by its bounds, so that a product-ion scan finds its window from its own record.
/// </summary>
[MessagePackObject]
public sealed class MassErrorModelBean
{
    [Key(0)]
    public MassErrorTerms Terms { get; set; }

    [Key(1)]
    public double ErrorAtReferencePpm { get; set; }

    [Key(2)]
    public double InverseSqrtMzCoefficient { get; set; }

    [Key(3)]
    public double RetentionTimeSlope { get; set; }

    [Key(4)]
    public double ReferenceMz { get; set; }

    [Key(5)]
    public double ReferenceRetentionTime { get; set; }

    [Key(6)]
    public double MinimumMz { get; set; }

    [Key(7)]
    public double MaximumMz { get; set; }

    [Key(8)]
    public double MinimumRetentionTime { get; set; }

    [Key(9)]
    public double MaximumRetentionTime { get; set; }

    [Key(10)]
    public List<MassErrorWindowBean> Windows { get; set; } = new List<MassErrorWindowBean>();

    /// <summary>
    /// The record of <paramref name="model"/>, whose window indices are positions in <paramref name="windowBounds"/>
    /// (the rounded bounds of <see cref="MassCalibrationPoint.FromAnnotation(IEnumerable{AnnotationCalibrant}, int, out IReadOnlyList{ValueTuple{double, double}})"/>).
    /// </summary>
    public static MassErrorModelBean FromModel(MassErrorModel model, IReadOnlyList<(double Lower, double Upper)> windowBounds) {
        if (model is null) {
            throw new ArgumentNullException(nameof(model));
        }
        if (model.Terms == MassErrorTerms.None) {
            throw new ArgumentException("A model with no term is no recalibration; record none instead.", nameof(model));
        }
        return new MassErrorModelBean
        {
            Terms = model.Terms,
            ErrorAtReferencePpm = model.ErrorAtReferencePpm,
            InverseSqrtMzCoefficient = model.InverseSqrtMzCoefficient,
            RetentionTimeSlope = model.RetentionTimeSlope,
            ReferenceMz = model.ReferenceMz,
            ReferenceRetentionTime = model.ReferenceRetentionTime,
            MinimumMz = model.MinimumMz,
            MaximumMz = model.MaximumMz,
            MinimumRetentionTime = model.MinimumRetentionTime,
            MaximumRetentionTime = model.MaximumRetentionTime,
            Windows = model.WindowOffsetsPpm
                .Where(pair => pair.Key >= 0 && pair.Key < windowBounds.Count)
                .OrderBy(pair => pair.Key)
                .Select(pair => new MassErrorWindowBean { LowerMz = windowBounds[pair.Key].Lower, UpperMz = windowBounds[pair.Key].Upper, OffsetPpm = pair.Value })
                .ToList(),
        };
    }

    /// <summary>
    /// The modelled error at an observed m/z, in ppm, as <see cref="MassErrorModel.PredictPpm"/> gives it: m/z and
    /// retention time held within the model's range, plus the offset of the scan's window.
    /// </summary>
    public double PredictPpm(double mz, double retentionTime, double windowOffsetPpm) {
        var error = ErrorAtReferencePpm + windowOffsetPpm;
        if (InverseSqrtMzCoefficient != 0d) {
            error += InverseSqrtMzCoefficient * (1d / Math.Sqrt(Within(mz, MinimumMz, MaximumMz)) - 1d / Math.Sqrt(ReferenceMz));
        }
        if (RetentionTimeSlope != 0d) {
            error += RetentionTimeSlope * (Within(retentionTime, MinimumRetentionTime, MaximumRetentionTime) - ReferenceRetentionTime);
        }
        return error;
    }

    /// <summary><c>mz / (1 + error × 10⁻⁶)</c>.</summary>
    public double CorrectMz(double mz, double retentionTime, double windowOffsetPpm) {
        return mz / (1d + PredictPpm(mz, retentionTime, windowOffsetPpm) * 1e-6);
    }

    /// <summary>The offset of each window, by its bounds as <see cref="MassCalibrationPoint.WindowIndex"/> rounds them.</summary>
    internal Dictionary<(double, double), double> WindowOffsets() {
        var offsets = new Dictionary<(double, double), double>();
        foreach (var window in Windows ?? new List<MassErrorWindowBean>()) {
            var key = MassCalibrationPoint.WindowIndex.Key(window.LowerMz, window.UpperMz);
            if (key.HasValue) {
                offsets[key.Value] = window.OffsetPpm;
            }
        }
        return offsets;
    }

    private static double Within(double value, double minimum, double maximum) {
        return value < minimum ? minimum : value > maximum ? maximum : value;
    }
}

/// <summary>One fixed isolation window, by its bounds rounded to 0.1 m/z, and its offset from the rest of the model.</summary>
[MessagePackObject]
public sealed class MassErrorWindowBean
{
    [Key(0)]
    public double LowerMz { get; set; }

    [Key(1)]
    public double UpperMz { get; set; }

    [Key(2)]
    public double OffsetPpm { get; set; }
}
