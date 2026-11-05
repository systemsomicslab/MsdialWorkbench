using CompMs.Common.DataObj.Result;
using CompMs.Common.Enum;
using CompMs.Common.Utility;
using CompMs.MsdialCore.DataObj;
using System;
using System.Collections.Generic;
using System.Linq;

namespace CompMs.MsdialCore.Algorithm.Annotation.LipidRt
{
    /// <summary>
    /// Builds the check's input from MS-DIAL's own objects: the peaks of one analysis file (hook A) or the spots of one
    /// alignment (hook B). Reads only; writes nothing back.
    /// </summary>
    /// <remarks>
    /// Drift children of LC-IM-MS objects are not evaluated in v1: pass the top-level list and the parents are read.
    /// </remarks>
    public static class LipidRtInputBuilder
    {
        /// <summary>The peaks of one analysis file, with RT on that file's own axis.</summary>
        public static LipidRtRunInput FromPeaks(IReadOnlyList<ChromatogramPeakFeature> peaks, IonMode polarity) {
            if (peaks is null) throw new ArgumentNullException(nameof(peaks));
            return Build(peaks.Select(p => (p.MasterPeakID, p.PeakFeature.ChromXsTop?.RT?.Value ?? double.NaN, p.PeakFeature.Mass, p.PeakFeature.PeakHeightTop,
                p.AdductType?.AdductIonName, p.MatchResults?.Representative, p.Ontology, p.Name)), polarity);
        }

        /// <summary>The spots of one alignment, with RT on the aligned axis and the average height.</summary>
        public static LipidRtRunInput FromSpots(IReadOnlyList<AlignmentSpotProperty> spots, IonMode polarity) {
            if (spots is null) throw new ArgumentNullException(nameof(spots));
            return Build(spots.Select(s => (s.MasterAlignmentID, s.TimesCenter?.RT?.Value ?? double.NaN, s.MassCenter, (double)s.HeightAverage,
                s.AdductType?.AdductIonName, s.MatchResults?.Representative, s.Ontology, s.Name)), polarity);
        }

        private static LipidRtRunInput Build(IEnumerable<(int Id, double Rt, double Mz, double Height, string Adduct, MsScanMatchResult Representative, string Ontology, string Name)> objects, IonMode polarity) {
            var candidates = new List<LipidRtCandidate>();
            var context = new List<LipidRtContextPeak>();
            var rtMin = double.PositiveInfinity;
            var rtMax = double.NegativeInfinity;
            foreach (var o in objects) {
                if (!double.IsNaN(o.Rt) && !double.IsInfinity(o.Rt)) {
                    rtMin = Math.Min(rtMin, o.Rt);
                    rtMax = Math.Max(rtMax, o.Rt);
                }
                context.Add(new LipidRtContextPeak
                {
                    Id = o.Id, Rt = o.Rt, Mz = o.Mz, Height = o.Height, Adduct = o.Adduct ?? string.Empty,
                    IsMsmsAnnotated = IsAnnotated(o.Representative) && o.Representative.IsSpectrumMatch,
                });
                var candidate = CreateCandidate(o.Id, o.Rt, o.Mz, o.Height, o.Adduct, o.Representative, o.Ontology, o.Name);
                if (candidate != null) {
                    candidates.Add(candidate);
                }
            }
            if (double.IsInfinity(rtMin)) {
                rtMin = rtMax = 0d;
            }
            return new LipidRtRunInput(candidates, context, rtMin, rtMax, polarity);
        }

        /// <summary>
        /// True when the representative carries an annotation at all: a precursor m/z or a spectrum match (annotation
        /// tag other than 999).
        /// </summary>
        private static bool IsAnnotated(MsScanMatchResult r) {
            return r != null && !r.IsUnknown && (r.IsPrecursorMzMatch || r.IsSpectrumMatch);
        }

        /// <summary>
        /// The candidate for one object, or null when the object carries no annotation (it is then context only).
        /// Eligibility follows SPEC 2.1 with addendum A6.
        /// </summary>
        /// <param name="id">MasterPeakID or MasterAlignmentID.</param>
        /// <param name="representative">The representative match result, read directly: on a project restored without its
        /// library the evaluator would have no entry for the annotator and would call every match unmatched.</param>
        /// <param name="ontology">The object's Ontology (LbmClass name).</param>
        /// <param name="featureName">The object's own name, which carries the "Unsettled:" prefix a person may have set.</param>
        public static LipidRtCandidate CreateCandidate(int id, double rt, double mz, double height, string adduct,
            MsScanMatchResult representative, string ontology, string featureName) {
            var r = representative;
            if (!IsAnnotated(r)) {
                return null;
            }
            var rawName = r.Name ?? string.Empty;
            var name = AnnotationName.WithoutPrefix(rawName) ?? string.Empty;
            var trimmed = name.Trim();
            if (trimmed.Length == 0 || string.Equals(trimmed, "Unknown", StringComparison.OrdinalIgnoreCase) || string.Equals(trimmed, "null", StringComparison.OrdinalIgnoreCase)) {
                return null;
            }
            ontology = ontology ?? string.Empty;
            var parsed = LipidRtNameParser.Parse(name, ontology);
            var isLipid = LipidRtNameParser.IsLipidOntology(ontology, name);
            var hasComposition = isLipid && parsed.IsParsed && parsed.Carbon > 0;
            var tagLevel = r.IsSpectrumMatch
                ? (r.IsLipidPositionMatch || r.IsLipidChainsMatch ? LipidRtTagLevel.Chain : r.IsLipidClassMatch ? LipidRtTagLevel.Class : LipidRtTagLevel.Msms)
                : LipidRtTagLevel.None;

            var reasons = new List<string>();
            if (!r.IsReferenceMatched) {
                reasons.Add("not_ref_matched");
            }
            if (!r.IsSpectrumMatch) {
                reasons.Add("no_spectrum_match"); // m/z (+RT) only, e.g. tag 520: RT evidence about an RT-matched suggestion would be circular
            }
            if ((r.Source & SourceType.TextDB) != 0 || (r.Source & (SourceType.MspDB | SourceType.GeneratedLipid)) == 0) {
                reasons.Add("not_spectral_library");
            }
            if (!isLipid) {
                reasons.Add("nonlipid");
            }
            else if (parsed.IsNonChain || parsed.Carbon <= 0) {
                reasons.Add("nonchain");
            }
            if (parsed.IsLabelled) {
                reasons.Add("internal_standard");
            }
            if (r.IsManuallyModified) {
                reasons.Add("manual");
            }
            if (parsed.IsUnsettled || IsUnsettledName(featureName)) {
                reasons.Add("unsettled");
            }
            return new LipidRtCandidate
            {
                Id = id,
                Rt = rt,
                Mz = mz,
                Height = height,
                Adduct = adduct ?? string.Empty,
                Name = parsed.Species.Length > 0 ? parsed.Species : name,
                Molecular = parsed.Molecular,
                Ontology = ontology,
                ClassKey = parsed.ClassKey.Length > 0 ? parsed.ClassKey : ontology,
                Carbon = parsed.Carbon,
                DoubleBondEquivalent = parsed.DoubleBondEquivalent,
                Oxygen = parsed.Oxygen,
                IsLipid = hasComposition,
                IsLabelled = parsed.IsLabelled,
                IsMsmsAnnotated = r.IsSpectrumMatch,
                TagLevel = tagLevel,
                WeightedDot = r.WeightedDotProduct,
                Chains = parsed.Chains,
                IsEligible = reasons.Count == 0 && hasComposition,
                IneligibleReason = string.Join(",", reasons),
            };
        }

        private static bool IsUnsettledName(string name) {
            return !string.IsNullOrEmpty(name) && name.TrimStart().StartsWith("Unsettled:", StringComparison.OrdinalIgnoreCase);
        }
    }
}
