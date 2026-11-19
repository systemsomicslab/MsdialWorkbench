using CompMs.Common.StructureFinder.DiagnosticRules.Model;
using System;
using System.Collections.Generic;

namespace CompMs.Common.StructureFinder.DiagnosticRules.Engine
{
    /// <summary>Why a spectrum is or is not evaluated (design 4.4), checked in this order (OQ17).</summary>
    internal enum SpectrumStatus
    {
        Evaluated,
        /// <summary>The precursor m/z is not finite or not positive.</summary>
        NoPrecursor,
        /// <summary>The ion mode is null.</summary>
        UnsupportedIonMode,
        /// <summary>No peak is usable.</summary>
        NoPeaks,
    }

    /// <summary>One peak that hits a member's target (design 4.4).</summary>
    internal readonly struct PeakHit
    {
        public PeakHit(int viewIndex, int inputIndex, double mz, double rel, double errorDa, bool present, bool inPrecursorRegion) {
            ViewIndex = viewIndex;
            InputIndex = inputIndex;
            Mz = mz;
            Rel = rel;
            ErrorDa = errorDa;
            Present = present;
            InPrecursorRegion = inPrecursorRegion;
        }

        /// <summary>The position in the view's (m/z, input index) order.</summary>
        public int ViewIndex { get; }
        /// <summary>The position in <see cref="DiagnosticSpectrum.Peaks"/>: results report this as <c>peak_index</c>.</summary>
        public int InputIndex { get; }
        public double Mz { get; }
        /// <summary>The relative intensity on the rule's convention scale (0 when p2 has no base peak).</summary>
        public double Rel { get; }
        /// <summary>peak m/z - target m/z.</summary>
        public double ErrorDa { get; }
        /// <summary>The relative intensity passes the term's floor.</summary>
        public bool Present { get; }
        /// <summary>|m - P_obs| &lt;= 1.0 + 1e-10: never a candidate of an item, but seen by I_raw and raw_present.</summary>
        public bool InPrecursorRegion { get; }

        /// <summary>A candidate of an item (design 4.6): present and outside the precursor region.</summary>
        public bool IsCandidate => Present && !InPrecursorRegion;
    }

    /// <summary>How one rule reads a spectrum: its convention, its product tolerance and the floor scale.</summary>
    internal readonly struct MatchSettings
    {
        public MatchSettings(RuleConvention convention, AppliedProductTolerance tolerance, double floorScale) {
            Convention = convention;
            Tolerance = tolerance;
            FloorScale = floorScale;
        }

        public RuleConvention Convention { get; }
        public AppliedProductTolerance Tolerance { get; }
        public double FloorScale { get; }
    }

    /// <summary>
    /// The engine's view of a <see cref="DiagnosticSpectrum"/> (design 4.4): its status, the usable peaks sorted by
    /// (m/z, input index), the reference intensities of both conventions, and the hit, presence and precursor-region
    /// predicates. Peaks with a non-finite m/z or intensity, m/z &lt;= 0 or intensity &lt;= 0 are dropped and counted.
    /// There is no centroiding, merging or noise filter.
    /// </summary>
    internal sealed class SpectrumView
    {
        /// <summary>The precursor region and the p2 base-peak exclusion: 1.0 Da around P_obs.</summary>
        public const double PrecursorRegionDa = 1.0;

        /// <summary>The inclusive slack of the engine's closed comparisons.</summary>
        public const double Slack = 1e-10;

        /// <summary>A loss hit needs reference - m &gt;= 1.0 (p2_diagnostics.py:153).</summary>
        public const double MinLossDa = 1.0;

        private readonly double[] _mz;
        private readonly double[] _intensity;
        private readonly int[] _input;

        private SpectrumView(DiagnosticSpectrum spectrum) {
            PrecursorMz = spectrum.PrecursorMz;
            IonMode = spectrum.IonMode;
            var peaks = spectrum.Peaks;
            NPeaksIn = peaks.Count;
            var kept = new List<int>(peaks.Count);
            for (var i = 0; i < peaks.Count; i++) {
                var p = peaks[i];
                if (IsFinite(p.Mz) && IsFinite(p.Intensity) && p.Mz > 0.0 && p.Intensity > 0.0) {
                    kept.Add(i);
                }
            }
            NPeaksDropped = peaks.Count - kept.Count;
            kept.Sort((a, b) => {
                var c = peaks[a].Mz.CompareTo(peaks[b].Mz);
                return c != 0 ? c : a.CompareTo(b);
            });
            _mz = new double[kept.Count];
            _intensity = new double[kept.Count];
            _input = new int[kept.Count];
            for (var k = 0; k < kept.Count; k++) {
                _mz[k] = peaks[kept[k]].Mz;
                _intensity[k] = peaks[kept[k]].Intensity;
                _input[k] = kept[k];
            }
            if (!IsFinite(PrecursorMz) || PrecursorMz <= 0.0) {
                Status = SpectrumStatus.NoPrecursor;
            }
            else if (!IonMode.HasValue) {
                Status = SpectrumStatus.UnsupportedIonMode;
            }
            else if (_mz.Length == 0) {
                Status = SpectrumStatus.NoPeaks;
            }
            else {
                Status = SpectrumStatus.Evaluated;
            }
            BaseIntensity = double.NaN;
            MaxIntensity = double.NaN;
            if (Status != SpectrumStatus.Evaluated) {
                return;
            }
            var max = 0.0;
            var baseI = 0.0;
            var hasBase = false;
            for (var k = 0; k < _mz.Length; k++) {
                var x = _intensity[k];
                if (x > max) {
                    max = x;
                }
                // The P2 base peak: |m - P_obs| > 1.0 exactly, without the 1e-10 slack of the candidate exclusion
                // (OQ23), so a peak 1.0 + 1e-11 Da from P_obs may set the base and still never be a candidate.
                if (Math.Abs(_mz[k] - PrecursorMz) > PrecursorRegionDa && (!hasBase || x > baseI)) {
                    baseI = x;
                    hasBase = true;
                }
            }
            MaxIntensity = max;
            HasBase = hasBase;
            BaseIntensity = hasBase ? baseI : double.NaN;
        }

        public static SpectrumView Create(DiagnosticSpectrum spectrum) => new SpectrumView(spectrum);

        public SpectrumStatus Status { get; }

        /// <summary>The status as results write it.</summary>
        public string StatusName {
            get {
                switch (Status) {
                    case SpectrumStatus.Evaluated: return "evaluated";
                    case SpectrumStatus.NoPrecursor: return "no_precursor";
                    case SpectrumStatus.UnsupportedIonMode: return "unsupported_ion_mode";
                    default: return "no_peaks";
                }
            }
        }

        public double PrecursorMz { get; }
        public DiagnosticIonMode? IonMode { get; }
        public int NPeaksIn { get; }
        public int NPeaksDropped { get; }

        /// <summary>The number of usable peaks.</summary>
        public int Count => _mz.Length;

        public double Mz(int k) => _mz[k];
        public double Intensity(int k) => _intensity[k];
        public int InputIndex(int k) => _input[k];

        /// <summary>Whether the p2 convention has a base peak: some usable peak with |m - P_obs| &gt; 1.0.</summary>
        public bool HasBase { get; }

        /// <summary>The p2 base intensity, NaN when there is none or the spectrum is not evaluated.</summary>
        public double BaseIntensity { get; }

        /// <summary>The msdial reference: the maximum intensity over every usable peak (NaN when not evaluated).</summary>
        public double MaxIntensity { get; }

        /// <summary>Whether <paramref name="convention"/> has its reference intensity. Without it (p2, no base peak)
        /// every relative intensity is 0 and nothing is present; the hypotheses still count and fail with
        /// <c>no_base_peak</c>.</summary>
        public bool HasReference(RuleConvention convention) =>
            Status == SpectrumStatus.Evaluated && (convention == RuleConvention.Msdial || HasBase);

        /// <summary>p2: I / base (0 to 1 outside the precursor region; a region peak, which the base excludes, can
        /// exceed 1, and the quotient is saturated at double.MaxValue so it stays finite, <see cref="Saturation"/>);
        /// msdial: (I / maxI) * 100.0 (0 to 100).</summary>
        public double Rel(int k, RuleConvention convention) {
            if (!HasReference(convention)) {
                return 0.0;
            }
            return convention == RuleConvention.P2 ? Saturation.Clamp(_intensity[k] / BaseIntensity) : (_intensity[k] / MaxIntensity) * 100.0;
        }

        /// <summary>p2: rel &gt;= floor * scale and rel &gt; 0; msdial: rel &gt; floor * scale.</summary>
        public static bool Present(double rel, double floor, double floorScale, RuleConvention convention) {
            var f = floor * floorScale;
            return convention == RuleConvention.P2 ? rel >= f && rel > 0.0 : rel > f;
        }

        /// <summary>|m - P_obs| &lt;= 1.0 + 1e-10 (design 4.4).</summary>
        public bool InPrecursorRegion(int k) => Math.Abs(_mz[k] - PrecursorMz) <= PrecursorRegionDa + Slack;

        /// <summary>
        /// The view indices of the peaks with <c>Math.Abs(m - target) &lt;= limit</c> (or <c>&lt; limit</c> when
        /// <paramref name="strict"/>), in view order. For a fixed target, m -> fl(m - target) is monotone, so the hits
        /// are one contiguous run of the sorted peaks: a binary search finds its start and the exact predicate its end.
        /// </summary>
        public void HitRange(double target, double limit, bool strict, out int start, out int end) {
            int lo = 0, hi = _mz.Length;
            while (lo < hi) {
                var mid = lo + (hi - lo) / 2;
                var d = _mz[mid] - target;
                if (strict ? d > -limit : d >= -limit) {
                    hi = mid;
                }
                else {
                    lo = mid + 1;
                }
            }
            start = lo;
            end = lo;
            while (end < _mz.Length && (strict ? Math.Abs(_mz[end] - target) < limit : Math.Abs(_mz[end] - target) <= limit)) {
                end++;
            }
        }

        /// <summary>
        /// Every peak that hits <paramref name="member"/> of <paramref name="term"/> (design 4.4), in view order:
        /// <list type="bullet">
        /// <item>a window: <c>Math.Abs(m - t) &lt;= half_width + 1e-10</c> in both conventions, with no product
        /// tolerance (OQ09);</item>
        /// <item>p2: <c>Math.Abs(m - t) &lt;= tol(t) + 1e-10</c>; msdial: <c>Math.Abs(m - t) &lt; tol(t)</c>;</item>
        /// <item>a loss, a loss window included, also needs <c>reference - m &gt;= 1.0</c>.</item>
        /// </list>
        /// Each hit carries its relative intensity, whether it passes the term's floor and whether it lies in the
        /// precursor region. Nothing is appended when the convention has no reference intensity.
        /// </summary>
        public void Hits(TermTargets term, TargetMember member, MatchSettings settings, List<PeakHit> output) {
            if (!HasReference(settings.Convention)) {
                return;
            }
            var t = member.TargetMz;
            double limit;
            bool strict;
            if (term.IsWindow) {
                limit = term.HalfWidthDa + Slack;
                strict = false;
            }
            else if (settings.Convention == RuleConvention.P2) {
                limit = settings.Tolerance.At(t) + Slack;
                strict = false;
            }
            else {
                limit = settings.Tolerance.At(t);
                strict = true;
            }
            HitRange(t, limit, strict, out var start, out var end);
            for (var k = start; k < end; k++) {
                if (member.IsLoss && !(member.LossReferenceMz - _mz[k] >= MinLossDa)) {
                    continue;
                }
                var rel = Rel(k, settings.Convention);
                output.Add(new PeakHit(k, _input[k], _mz[k], rel, _mz[k] - t,
                                       Present(rel, term.Floor, settings.FloorScale, settings.Convention), InPrecursorRegion(k)));
            }
        }

        /// <summary>Every hit of every member of <paramref name="term"/>, member by member.</summary>
        public List<PeakHit> TermHits(TermTargets term, MatchSettings settings) {
            var output = new List<PeakHit>();
            foreach (var m in term.Members) {
                Hits(term, m, settings, output);
            }
            return output;
        }

        /// <summary>I_raw: the maximum relative intensity over <paramref name="hits"/>, floors ignored; 0 when there
        /// is none. The precursor region is included.</summary>
        public static double IRaw(IEnumerable<PeakHit> hits) {
            var max = 0.0;
            foreach (var h in hits) {
                if (h.Rel > max) {
                    max = h.Rel;
                }
            }
            return max;
        }

        /// <summary>raw_present: some hit passes the floor. The precursor region is included.</summary>
        public static bool RawPresent(IEnumerable<PeakHit> hits) {
            foreach (var h in hits) {
                if (h.Present) {
                    return true;
                }
            }
            return false;
        }

        /// <summary>
        /// The candidates of an item (design 4.6): its hits that are present and outside the precursor region, ordered by
        /// rel descending, then |err| ascending, then input index ascending.
        /// </summary>
        public static List<PeakHit> Candidates(IEnumerable<PeakHit> hits) {
            var output = new List<PeakHit>();
            foreach (var h in hits) {
                if (h.IsCandidate) {
                    output.Add(h);
                }
            }
            output.Sort((a, b) => {
                var c = b.Rel.CompareTo(a.Rel);
                if (c != 0) {
                    return c;
                }
                c = Math.Abs(a.ErrorDa).CompareTo(Math.Abs(b.ErrorDa));
                return c != 0 ? c : a.InputIndex.CompareTo(b.InputIndex);
            });
            return output;
        }

        private static bool IsFinite(double x) => !double.IsNaN(x) && !double.IsInfinity(x);
    }
}
