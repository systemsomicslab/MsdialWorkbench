using System;
using System.Collections.Generic;
using System.Linq;

namespace CompMs.MsdialCore.Algorithm.MassCalibration;

/// <summary>The terms a mass error model may hold.</summary>
[Flags]
public enum MassErrorTerms
{
    /// <summary>No error is modelled: the m/z are left as read.</summary>
    None = 0,

    /// <summary>A constant error in ppm: in a time-of-flight calibration m = A(t − t0)², a gain A that is off.</summary>
    Offset = 1,

    /// <summary>An error in ppm proportional to 1/√(m/z): a time offset t0 that is off.</summary>
    InverseSqrtMz = 2,

    /// <summary>An error that drifts linearly with retention time through the run.</summary>
    RetentionTime = 4,

    /// <summary>An offset for each fixed isolation window, drawn toward the overall offset.</summary>
    Window = 8,
}

/// <summary>
/// The m/z error of one file's MS1 survey or product-ion scans, in ppm, as a function of m/z, retention time and
/// isolation window:
/// <c>error = a + c·(1/√m − 1/√m₀) + d·(t − t₀) + w[window]</c>,
/// with only the terms in <see cref="Terms"/>. m and t are held within the range of the points the model was fitted
/// to, so the model is not carried beyond them.
/// </summary>
public sealed class MassErrorModel
{
    private readonly MassErrorDesign _design;
    private readonly double[] _coefficients;
    private readonly double[,] _covariance;

    internal MassErrorModel(MassErrorDesign design, double[] coefficients, double[,] covariance) {
        _design = design;
        _coefficients = coefficients;
        _covariance = covariance;
    }

    /// <summary>No error: every m/z is left as read.</summary>
    public static MassErrorModel None { get; } = new MassErrorModel(MassErrorDesign.Empty, Array.Empty<double>(), new double[0, 0]);

    public MassErrorTerms Terms => _design.Terms;

    /// <summary>m₀, the median m/z of the points.</summary>
    public double ReferenceMz => _design.ReferenceMz;

    /// <summary>t₀, the median retention time of the points, in minutes.</summary>
    public double ReferenceRetentionTime => _design.ReferenceRetentionTime;

    /// <summary>a: the error at <see cref="ReferenceMz"/> and <see cref="ReferenceRetentionTime"/>, in ppm.</summary>
    public double ErrorAtReferencePpm => Coefficient(_design.OffsetColumn);

    /// <summary>c, in ppm·√(m/z).</summary>
    public double InverseSqrtMzCoefficient => Coefficient(_design.InverseSqrtMzColumn);

    /// <summary>d, in ppm per minute.</summary>
    public double RetentionTimeSlope => Coefficient(_design.RetentionTimeColumn);

    /// <summary>
    /// w: each fixed isolation window's offset from the rest of the model, by window index, in ppm; empty without the
    /// window term. A window not among them is given none.
    /// </summary>
    public IReadOnlyDictionary<int, double> WindowOffsetsPpm {
        get {
            if (!_design.Has(MassErrorTerms.Window)) {
                return new Dictionary<int, double>();
            }
            return _design.Windows.Select((window, i) => (window, i)).ToDictionary(pair => pair.window, pair => _coefficients[_design.FixedCount + pair.i]);
        }
    }

    /// <summary>The modelled error at an observed m/z, in ppm; 0 for <see cref="None"/>.</summary>
    public double PredictPpm(double mz, double retentionTime, int window = MassCalibrationPoint.NoWindow) {
        if (_design.Terms == MassErrorTerms.None) {
            return 0d;
        }
        var row = new double[_design.FixedCount];
        var windowColumn = _design.Row(mz, retentionTime, window, row);
        var value = 0d;
        for (var j = 0; j < row.Length; j++) {
            value += row[j] * _coefficients[j];
        }
        return windowColumn >= 0 ? value + _coefficients[windowColumn] : value;
    }

    /// <summary>The standard error of <see cref="PredictPpm"/> at that point, in ppm.</summary>
    public double StandardErrorPpm(double mz, double retentionTime, int window = MassCalibrationPoint.NoWindow) {
        if (_design.Terms == MassErrorTerms.None) {
            return 0d;
        }
        var row = new double[_design.FixedCount];
        var windowColumn = _design.Row(mz, retentionTime, window, row);
        var variance = 0d;
        for (var j = 0; j < row.Length; j++) {
            for (var k = 0; k < row.Length; k++) {
                variance += row[j] * _covariance[j, k] * row[k];
            }
            if (windowColumn >= 0) {
                variance += 2d * row[j] * _covariance[j, windowColumn];
            }
        }
        if (windowColumn >= 0) {
            variance += _covariance[windowColumn, windowColumn];
        }
        return Math.Sqrt(Math.Max(0d, variance));
    }

    /// <summary>The m/z with the modelled error taken out: <c>mz / (1 + error × 10⁻⁶)</c>.</summary>
    public double CorrectMz(double mz, double retentionTime, int window = MassCalibrationPoint.NoWindow) {
        return mz / (1d + PredictPpm(mz, retentionTime, window) * 1e-6);
    }

    /// <summary>"Offset + 1/sqrt(m/z) + RT + window", or "None".</summary>
    public static string Describe(MassErrorTerms terms) {
        if (terms == MassErrorTerms.None) {
            return "None";
        }
        var names = new List<string>();
        if ((terms & MassErrorTerms.Offset) != 0) {
            names.Add("Offset");
        }
        if ((terms & MassErrorTerms.InverseSqrtMz) != 0) {
            names.Add("1/sqrt(m/z)");
        }
        if ((terms & MassErrorTerms.RetentionTime) != 0) {
            names.Add("RT");
        }
        if ((terms & MassErrorTerms.Window) != 0) {
            names.Add("window");
        }
        return string.Join(" + ", names);
    }

    private double Coefficient(int column) => column >= 0 ? _coefficients[column] : 0d;
}

/// <summary>
/// The columns of a mass error model: the fixed terms (offset, 1/√m, RT), each centred on the points' median, then one
/// column per fixed isolation window.
/// </summary>
internal sealed class MassErrorDesign
{
    private readonly Dictionary<int, int> _windowColumns;

    public MassErrorDesign(MassErrorTerms terms, double referenceMz, double referenceRetentionTime, double minimumMz, double maximumMz, double minimumRetentionTime, double maximumRetentionTime, IReadOnlyList<int> windows) {
        if (terms != MassErrorTerms.None && (terms & MassErrorTerms.Offset) == 0) {
            throw new ArgumentException("A mass error model holds an offset whenever it holds any term.", nameof(terms));
        }
        Terms = terms;
        ReferenceMz = referenceMz;
        ReferenceRetentionTime = referenceRetentionTime;
        MinimumMz = minimumMz;
        MaximumMz = maximumMz;
        MinimumRetentionTime = minimumRetentionTime;
        MaximumRetentionTime = maximumRetentionTime;
        Windows = windows;

        var column = 0;
        OffsetColumn = Has(MassErrorTerms.Offset) ? column++ : -1;
        InverseSqrtMzColumn = Has(MassErrorTerms.InverseSqrtMz) ? column++ : -1;
        RetentionTimeColumn = Has(MassErrorTerms.RetentionTime) ? column++ : -1;
        FixedCount = column;
        _windowColumns = Has(MassErrorTerms.Window)
            ? windows.Select((window, i) => (window, i)).ToDictionary(pair => pair.window, pair => FixedCount + pair.i)
            : new Dictionary<int, int>();
        Count = FixedCount + _windowColumns.Count;
    }

    public static MassErrorDesign Empty { get; } = new MassErrorDesign(MassErrorTerms.None, double.NaN, double.NaN, double.NaN, double.NaN, double.NaN, double.NaN, Array.Empty<int>());

    public MassErrorTerms Terms { get; }
    public double ReferenceMz { get; }
    public double ReferenceRetentionTime { get; }
    public double MinimumMz { get; }
    public double MaximumMz { get; }
    public double MinimumRetentionTime { get; }
    public double MaximumRetentionTime { get; }
    public IReadOnlyList<int> Windows { get; }

    public int OffsetColumn { get; }
    public int InverseSqrtMzColumn { get; }
    public int RetentionTimeColumn { get; }

    /// <summary>Columns of the fixed terms, first in every row.</summary>
    public int FixedCount { get; }

    /// <summary>All columns: the fixed terms, then the windows.</summary>
    public int Count { get; }

    public bool Has(MassErrorTerms term) => (Terms & term) != 0;

    /// <summary>The same references and windows with other terms.</summary>
    public MassErrorDesign With(MassErrorTerms terms) {
        return new MassErrorDesign(terms, ReferenceMz, ReferenceRetentionTime, MinimumMz, MaximumMz, MinimumRetentionTime, MaximumRetentionTime, Windows);
    }

    /// <summary>
    /// Writes the fixed columns of a point into <paramref name="row"/> and returns the column of its window, or -1 when
    /// the model has no window term or the window is not one of its own.
    /// </summary>
    public int Row(double mz, double retentionTime, int window, double[] row) {
        if (OffsetColumn >= 0) {
            row[OffsetColumn] = 1d;
        }
        if (InverseSqrtMzColumn >= 0) {
            row[InverseSqrtMzColumn] = 1d / Math.Sqrt(Within(mz, MinimumMz, MaximumMz)) - 1d / Math.Sqrt(ReferenceMz);
        }
        if (RetentionTimeColumn >= 0) {
            row[RetentionTimeColumn] = Within(retentionTime, MinimumRetentionTime, MaximumRetentionTime) - ReferenceRetentionTime;
        }
        return _windowColumns.TryGetValue(window, out var column) ? column : -1;
    }

    private static double Within(double value, double minimum, double maximum) {
        return value < minimum ? minimum : value > maximum ? maximum : value;
    }
}
