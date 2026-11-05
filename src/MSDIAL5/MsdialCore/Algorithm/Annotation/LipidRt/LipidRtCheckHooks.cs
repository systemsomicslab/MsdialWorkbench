using CompMs.MsdialCore.DataObj;
using CompMs.MsdialCore.Parameter;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace CompMs.MsdialCore.Algorithm.Annotation.LipidRt
{
    /// <summary>What the check did on one unit: its result, what happened to the tags, or why it did not run.</summary>
    public sealed class LipidRtUnitRun
    {
        internal LipidRtUnitRun(LipidRtRunResult result, LipidRtTagSummary tags, string parameterHash, Exception error) {
            Result = result;
            Tags = tags;
            ParameterHash = parameterHash ?? string.Empty;
            Error = error;
        }

        /// <summary>The unit's result; null when the check did not run (switched off, or it failed).</summary>
        public LipidRtRunResult Result { get; }

        /// <summary>What the protocol did to the tags, including the withdrawal when the check did not run.</summary>
        public LipidRtTagSummary Tags { get; }

        public string ParameterHash { get; }

        /// <summary>The exception the check stopped with; its verdicts and tags were then withdrawn, not half-written.</summary>
        public Exception Error { get; }

        public bool Ran => Result != null;

        /// <summary>One line for a log: the outcome counts over eligible objects, the tags, and the gate when it failed.</summary>
        public string Describe() {
            if (Error != null) {
                return $"failed ({Error.GetType().Name}: {Error.Message}); no verdicts or Confirmed tags were written by it, and its earlier ones were withdrawn ({Tags.Withdrawn}).";
            }
            if (Result is null) {
                return Tags.Withdrawn > 0
                    ? $"not run; {Tags.Withdrawn} Confirmed tags it had set earlier were withdrawn."
                    : "not run.";
            }
            var eligible = Result.Results.Where(r => r.Reason != LipidRtReason.Ineligible).ToList();
            var text = string.Format(CultureInfo.InvariantCulture,
                "{0} annotated, {1} evaluable: {2} Confirmed, {3} NotEvaluable, {4} Inconsistent; Confirmed tags set {5} (already tagged {6}, Misannotation {7}, suppressed {8}, earlier ones withdrawn {9}); rules {10}, parameter {11}.",
                Result.Results.Count, eligible.Count,
                eligible.Count(r => r.Outcome == LipidRtOutcome.Confirmed),
                eligible.Count(r => r.Outcome == LipidRtOutcome.NotEvaluable),
                eligible.Count(r => r.Outcome == LipidRtOutcome.Inconsistent),
                Tags.Set, Tags.AlreadySelected, Tags.Misannotation, Tags.Suppressed, Tags.Withdrawn,
                LipidRtCheckParameter.RuleVersion, ParameterHash);
            if (Result.Gate != null && !Result.Gate.Passed) {
                text += " Reversed-phase gate not passed: " + Result.Gate + ".";
            }
            return text;
        }
    }

    /// <summary>
    /// Where processing runs the lipid RT check (SPEC section 11, addendum B3): hook A on the peaks of one analysis file
    /// between annotation and saving, and hook B on the spots of an alignment once they are packed and their annotations
    /// are final (after isotope tracking, which clears some), before they are saved. Both are shared by the Console and
    /// the GUI, and both apply the verdicts through <see cref="LipidRtTagProtocol"/>.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The check runs only when <see cref="LipidRtCheckParameter.RunsFor"/> says so: a lipidomics project with the
    /// check enabled (the default). Whether the chromatography is reversed phase is decided inside the check by its
    /// run-level gate. v1 runs in LC-MS processing only; the callers decide that.
    /// </para>
    /// <para>
    /// When the check does not run, the unit still goes through the protocol with no result, so a Confirmed tag the
    /// check set on an earlier run, and its verdicts, do not outlive a re-annotation they no longer describe. Tags a
    /// person or another tool set are never touched.
    /// </para>
    /// <para>
    /// Each call reads only its own unit and the parameter, writes only that unit's objects, and keeps no state, so
    /// hook A can run inside the per-file parallel loop.
    /// </para>
    /// </remarks>
    public static class LipidRtCheckHooks
    {
        /// <summary>Hook A: the peaks of one analysis file, after annotation and before they are saved.</summary>
        public static LipidRtUnitRun RunOnPeaks(IReadOnlyList<ChromatogramPeakFeature> peaks, ParameterBase parameter) {
            if (peaks is null) throw new ArgumentNullException(nameof(peaks));
            if (parameter is null) throw new ArgumentNullException(nameof(parameter));
            return Run(parameter,
                () => LipidRtInputBuilder.FromPeaks(peaks, parameter.IonMode),
                (result, hash) => LipidRtTagProtocol.ApplyToPeaks(peaks, result, hash));
        }

        /// <summary>
        /// Hook B: the spots of one alignment, after they are packed and every step that changes their annotations has run,
        /// and before the result is saved or exported.
        /// </summary>
        public static LipidRtUnitRun RunOnSpots(IReadOnlyList<AlignmentSpotProperty> spots, ParameterBase parameter) {
            if (spots is null) throw new ArgumentNullException(nameof(spots));
            if (parameter is null) throw new ArgumentNullException(nameof(parameter));
            return Run(parameter,
                () => LipidRtInputBuilder.FromSpots(spots, parameter.IonMode),
                (result, hash) => LipidRtTagProtocol.ApplyToSpots(spots, result, hash));
        }

        private static LipidRtUnitRun Run(ParameterBase parameter, Func<LipidRtRunInput> input, Func<LipidRtRunResult, string, LipidRtTagSummary> apply) {
            var checkParameter = parameter.LipidRtCheckParam;
            var hash = checkParameter.ComputeHash();
            if (!checkParameter.RunsFor(parameter.TargetOmics)) {
                return new LipidRtUnitRun(null, apply(null, hash), hash, null);
            }
            LipidRtRunResult result;
            try {
                result = new LipidRtCheck(checkParameter).Evaluate(input());
            }
            catch (Exception ex) when (!(ex is OperationCanceledException)) {
                // An optional check must not take the processing down with it, and must not leave half a result behind:
                // the unit is withdrawn as if the check were off, and the caller reports the error.
                return new LipidRtUnitRun(null, apply(null, hash), hash, ex);
            }
            return new LipidRtUnitRun(result, apply(result, hash), hash, null);
        }
    }
}
