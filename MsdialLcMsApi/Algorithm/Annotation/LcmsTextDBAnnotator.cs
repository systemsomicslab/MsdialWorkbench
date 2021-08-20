using CompMs.Common.Algorithm.Scoring;
using CompMs.Common.Components;
using CompMs.Common.DataObj.Property;
using CompMs.Common.DataObj.Result;
using CompMs.Common.Extension;
using CompMs.Common.FormulaGenerator.Function;
using CompMs.Common.Interfaces;
using CompMs.Common.Parameter;
using CompMs.Common.Utility;
using CompMs.MsdialCore.Algorithm;
using CompMs.MsdialCore.Algorithm.Annotation;
using CompMs.MsdialCore.DataObj;
using System;
using System.Collections.Generic;
using System.Linq;

namespace CompMs.MsdialLcMsApi.Algorithm.Annotation
{
    public class LcmsTextDBAnnotator : StandardRestorableBase, ISerializableAnnotator<IMSIonProperty, IMSScanProperty, MoleculeDataBase>
    {
        private static readonly IComparer<IMSScanProperty> comparer = CompositeComparer.Build(MassComparer.Comparer, ChromXsComparer.RTComparer);

        public LcmsTextDBAnnotator(MoleculeDataBase textDB, MsRefSearchParameterBase parameter, string annotatorID)
            : base(textDB.Database, parameter, annotatorID, SourceType.TextDB) {
            this.db.Sort(comparer);
            this.ReferObject = textDB;
        }

        public MsScanMatchResult Annotate(
            IMSIonProperty property, IMSScanProperty scan, IReadOnlyList<IsotopicPeak> isotopes,
            MsRefSearchParameterBase parameter = null) {

            if (parameter is null) {
                parameter = Parameter;
            }

            return FindCandidatesCore(property, isotopes, parameter, db, Key).FirstOrDefault();
        }

        public List<MsScanMatchResult> FindCandidates(
            IMSIonProperty property, IMSScanProperty scan, IReadOnlyList<IsotopicPeak> isotopes,
            MsRefSearchParameterBase parameter = null) {

            if (parameter is null) {
                parameter = Parameter;
            }

            return FindCandidatesCore(property, isotopes, parameter, db, Key);
        }

        private static List<MsScanMatchResult> FindCandidatesCore(
            IMSIonProperty property, IReadOnlyList<IsotopicPeak> isotopes,
            MsRefSearchParameterBase parameter, IReadOnlyList<MoleculeMsReference> textDB, string annotatorID) {

            (var lo, var hi) = SearchBoundIndex(property, textDB, parameter.Ms1Tolerance, parameter.RtTolerance);
            var results = new List<MsScanMatchResult>(hi - lo);
            for (var i = lo; i < hi; i++) {
                var candidate = textDB[i];
                if (parameter.IsUseTimeForAnnotationFiltering
                    && Math.Abs(property.ChromXs.RT.Value - candidate.ChromXs.RT.Value) > parameter.RtTolerance) {
                    continue;
                }

                var result = CalculateScoreCore(property, isotopes, candidate, candidate.IsotopicPeaks, parameter, annotatorID);
                result.LibraryIDWhenOrdered = i;
                ValidateCore(result, property, candidate, parameter);
                results.Add(result);
            }
            return results.OrderByDescending(result => result.TotalScore).ToList();
        }

        public MsScanMatchResult CalculateScore(
            IMSIonProperty property, IMSScanProperty scan, IReadOnlyList<IsotopicPeak> isotopes,
            MoleculeMsReference reference, MsRefSearchParameterBase parameter = null) {

            if (parameter is null) {
                parameter = Parameter;
            }

            return CalculateScoreCore(property, isotopes, reference, reference.IsotopicPeaks, parameter, Key);
        }

        private static MsScanMatchResult CalculateScoreCore(
            IMSIonProperty property, IReadOnlyList<IsotopicPeak> scanIsotopes,
            MoleculeMsReference reference, IReadOnlyList<IsotopicPeak> referenceIsotopes,
            MsRefSearchParameterBase parameter, string sourceKey) {

            var ms1Tol = CalculateMassTolerance(parameter.Ms1Tolerance, property.PrecursorMz);
            var ms1Similarity = MsScanMatching.GetGaussianSimilarity(property.PrecursorMz, reference.PrecursorMz, ms1Tol);

            var isotopeSimilarity = MsScanMatching.GetIsotopeRatioSimilarity(scanIsotopes, referenceIsotopes, property.PrecursorMz, ms1Tol);

            var result = new MsScanMatchResult
            {
                Name = reference.Name, LibraryID = reference.ScanID, InChIKey = reference.InChIKey,
                AcurateMassSimilarity = (float)ms1Similarity, IsotopeSimilarity = (float)isotopeSimilarity,
                Source = SourceType.TextDB, SourceKey = sourceKey
            };
            if (parameter.IsUseTimeForAnnotationScoring) {
                var rtSimilarity = MsScanMatching.GetGaussianSimilarity(property.ChromXs.RT.Value, reference.ChromXs.RT.Value, parameter.RtTolerance);
                result.RtSimilarity = (float)rtSimilarity;

            }
            result.TotalScore = (float)CalculateTotalScoreCore(result, parameter);

            return result;
        }

        public double CalculateAnnotatedScore(MsScanMatchResult result, MsRefSearchParameterBase parameter = null) {
            if (parameter is null) {
                parameter = Parameter;
            }
            return CalculateTotalScoreCore(result, parameter);
        }

        public double CalculateSuggestedScore(MsScanMatchResult result, MsRefSearchParameterBase parameter = null) {
            if (parameter is null) {
                parameter = Parameter;
            }
            return CalculateTotalScoreCore(result, parameter);
        }

        private static double CalculateTotalScoreCore(MsScanMatchResult result, MsRefSearchParameterBase parameter) {
            var scores = new List<double> { };
            if (result.AcurateMassSimilarity >= 0)
                scores.Add(result.AcurateMassSimilarity);
            if (parameter.IsUseTimeForAnnotationScoring && result.RtSimilarity >= 0)
                scores.Add(result.RtSimilarity);
            if (result.IsotopeSimilarity >= 0)
                scores.Add(result.IsotopeSimilarity);
            return scores.DefaultIfEmpty().Average();
        }

        public IMatchResultRefer ReferObject { get; }
        public override MoleculeMsReference Refer(MsScanMatchResult result) {
            return ReferObject.Refer(result);
        }

        public List<MoleculeMsReference> Search(IMSIonProperty property, MsRefSearchParameterBase parameter = null) {
            if (parameter is null) {
                parameter = Parameter;
            }

            (var lo, var hi) = SearchBoundIndex(property, db, parameter.Ms1Tolerance, parameter.RtTolerance);
            var candidates =  db.GetRange(lo, hi - lo);
            if (!parameter.IsUseTimeForAnnotationFiltering) {
                return candidates;
            }
            return candidates.Where(candidate => Math.Abs(candidate.ChromXs.RT.Value - property.ChromXs.RT.Value) <= parameter.RtTolerance).ToList();
        }

        private static (int lo, int hi) SearchBoundIndex(IMSIonProperty property, IReadOnlyList<MoleculeMsReference> textDB, double ms1Tolerance, double rtTolerance) {
            ms1Tolerance = CalculateMassTolerance(ms1Tolerance, property.PrecursorMz);
            var rt = property.ChromXs.RT;
            var dummy = new MSScanProperty { PrecursorMz = property.PrecursorMz - ms1Tolerance, ChromXs = new ChromXs(rt.Value - rtTolerance, rt.Type, rt.Unit) };
            var lo = SearchCollection.LowerBound(textDB, dummy, comparer);
            dummy.PrecursorMz = property.PrecursorMz + ms1Tolerance;
            dummy.ChromXs.RT.Value = rt.Value + rtTolerance;
            var hi = SearchCollection.UpperBound(textDB, dummy, lo, textDB.Count, comparer);
            return (lo, hi);
        }

        private static double CalculateMassTolerance(double tolerance, double mass) {
            if (mass <= 500)
                return tolerance;
            var ppm = Math.Abs(MolecularFormulaUtility.PpmCalculator(500.00, 500.00 + tolerance));
            return MolecularFormulaUtility.ConvertPpmToMassAccuracy(mass, ppm);
        }

        public void Validate(
            MsScanMatchResult result,
            IMSIonProperty property, IMSScanProperty scan, IReadOnlyList<IsotopicPeak> isotopes,
            MoleculeMsReference reference,
            MsRefSearchParameterBase parameter = null) {

            if (parameter is null) {
                parameter = Parameter;
            }

            ValidateCore(result, property, reference, parameter);
        }

        private static readonly double MsdialRtMatchThreshold = 0.5;
        private static void ValidateCore(MsScanMatchResult result, IMSIonProperty property, MoleculeMsReference reference, MsRefSearchParameterBase parameter) {
            var ms1Tol = CalculateMassTolerance(parameter.Ms1Tolerance, property.PrecursorMz);
            result.IsPrecursorMzMatch = Math.Abs(property.PrecursorMz - reference.PrecursorMz) <= ms1Tol;

            if (parameter.IsUseTimeForAnnotationScoring) {
                var diff = Math.Abs(property.ChromXs.RT.Value - reference.ChromXs.RT.Value);
                result.IsRtMatch =  diff <= MsdialRtMatchThreshold && diff <= parameter.RtTolerance;
            }
        }

        public MsScanMatchResult SelectTopHit(IEnumerable<MsScanMatchResult> results, MsRefSearchParameterBase parameter = null) {
            return results.Argmax(result => result.TotalScore);
        }

        public List<MsScanMatchResult> FilterByThreshold(IEnumerable<MsScanMatchResult> results, MsRefSearchParameterBase parameter = null) {
            if (parameter is null) {
                parameter = Parameter;
            }
            var filtered = new List<MsScanMatchResult>();
            foreach (var result in results) {
                if (!result.IsPrecursorMzMatch) {
                    continue;
                }
                if (CalculateTotalScoreCore(result, parameter) < parameter.TotalScoreCutoff) {
                    continue;
                }
                filtered.Add(result);
            }
            return filtered;
        }

        public List<MsScanMatchResult> SelectReferenceMatchResults(IEnumerable<MsScanMatchResult> results, MsRefSearchParameterBase parameter = null) {
            return FilterByThreshold(results, parameter)
                .Where(result => result.IsPrecursorMzMatch)
                .ToList();
        }
    }
}
