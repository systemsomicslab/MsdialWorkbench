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

namespace CompMs.MsdialImmsCore.Algorithm.Annotation
{
    public class ImmsTextDBAnnotator : TextDbRestorableBase, ISerializableAnnotator<IMSIonProperty, IMSScanProperty, MoleculeDataBase>
    {
        private static readonly IComparer<IMSIonProperty> comparer = CompositeComparer.Build<IMSIonProperty>(MassComparer.Comparer, CollisionCrossSectionComparer.Comparer);

        public MsRefSearchParameterBase Parameter { get; }

        public ImmsTextDBAnnotator(IEnumerable<MoleculeMsReference> textDB, MsRefSearchParameterBase parameter, string sourceKey)
            : base(textDB, sourceKey) {
            this.db.Sort(comparer);
            this.Parameter = parameter;
            this.ReferObject = new DataBaseRefer(this.db);
        }

        public MsScanMatchResult Annotate(
            IMSIonProperty property, IMSScanProperty scan, IReadOnlyList<IsotopicPeak> isotopes,
            MsRefSearchParameterBase parameter = null) {

            if (parameter == null)
                parameter = Parameter;
            return FindCandidatesCore(property, isotopes, parameter, db, Key).FirstOrDefault();
        }

        public List<MsScanMatchResult> FindCandidates(
            IMSIonProperty property, IMSScanProperty scan, IReadOnlyList<IsotopicPeak> isotopes,
            MsRefSearchParameterBase parameter = null) {

            if (parameter == null)
                parameter = Parameter;

            return FindCandidatesCore(property, isotopes, parameter, db, Key);
        }

        private static List<MsScanMatchResult> FindCandidatesCore(
            IMSIonProperty property, IReadOnlyList<IsotopicPeak> isotopes,
            MsRefSearchParameterBase parameter, IReadOnlyList<MoleculeMsReference> textDB, string sourceKey) {
            (var lo, var hi) = SearchBoundIndex(property, textDB, parameter.Ms1Tolerance, parameter.IsUseCcsForAnnotationFiltering ? parameter.CcsTolerance : double.PositiveInfinity);
            var results = new List<MsScanMatchResult>(hi - lo);
            for (var i = lo; i < hi; i++) {
                var candidate = textDB[i];
				if (parameter.IsUseCcsForAnnotationFiltering
                    && Math.Abs(property.CollisionCrossSection - candidate.CollisionCrossSection) > parameter.CcsTolerance)
                    continue;
                var result = CalculateScoreCore(property, isotopes, candidate, candidate.IsotopicPeaks, parameter, sourceKey);
                result.LibraryIDWhenOrdered = i;
                ValidateCore(result, property, candidate, parameter);
                results.Add(result);
            }
            return results.OrderByDescending(result => result.TotalScore).ToList();
        }

        public MsScanMatchResult CalculateScore(
            IMSIonProperty property, IMSScanProperty scan, IReadOnlyList<IsotopicPeak> isotopes,
            MoleculeMsReference reference, MsRefSearchParameterBase parameter = null) {

            if (parameter == null)
                parameter = Parameter;
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

            if (parameter.IsUseCcsForAnnotationScoring) {
                var ccsSimilarity = MsScanMatching.GetGaussianSimilarity(property.CollisionCrossSection, reference.CollisionCrossSection, parameter.CcsTolerance);
                result.CcsSimilarity = (float)ccsSimilarity;
            }

            var scores = new List<float> { };
            if (result.AcurateMassSimilarity >= 0)
                scores.Add(result.AcurateMassSimilarity);
            if (result.CcsSimilarity >= 0)
                scores.Add(result.CcsSimilarity);
            //if (result.IsotopeSimilarity >= 0)
            //    scores.Add(result.IsotopeSimilarity);
            result.TotalScore = scores.DefaultIfEmpty().Average();

            return result;
        }

        public IMatchResultRefer ReferObject { get; }
        public override MoleculeMsReference Refer(MsScanMatchResult result) {
            return ReferObject.Refer(result);
        }

        public List<MoleculeMsReference> Search(IMSIonProperty property, MsRefSearchParameterBase parameter = null) {
            if (parameter == null)
                parameter = Parameter;

            (var lo, var hi) = SearchBoundIndex(property, db, parameter.Ms1Tolerance, parameter.IsUseCcsForAnnotationFiltering ? parameter.CcsTolerance : double.PositiveInfinity);
            var candidates = db.GetRange(lo, hi - lo);
            if (!parameter.IsUseCcsForAnnotationFiltering) {
                return candidates;
            }
            return candidates.Where(candidate => Math.Abs(candidate.CollisionCrossSection - property.CollisionCrossSection) <= parameter.CcsTolerance).ToList();
        }

        private static (int lo, int hi) SearchBoundIndex(IMSIonProperty property, IReadOnlyList<MoleculeMsReference> textDB, double ms1Tolerance, double ccsTolerance) {
            ms1Tolerance = CalculateMassTolerance(ms1Tolerance, property.PrecursorMz);
            var dummy = new MSIonProperty(property.PrecursorMz - ms1Tolerance, null, Common.Enum.IonMode.Negative, null, property.CollisionCrossSection - ccsTolerance);
            var lo = SearchCollection.LowerBound(textDB, dummy, comparer);
            dummy.PrecursorMz = property.PrecursorMz + ms1Tolerance;
            dummy.CollisionCrossSection = property.CollisionCrossSection + ccsTolerance;
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

            if (parameter == null)
                parameter = Parameter;
            ValidateCore(result, property, reference, parameter);
        }

        private static void ValidateCore(MsScanMatchResult result, IMSIonProperty property, MoleculeMsReference reference, MsRefSearchParameterBase parameter) {
            ValidateBase(result, property, reference, parameter);
        }

        private static readonly double MsdialCcsMatchThreshold = 10d;
        private static void ValidateBase(MsScanMatchResult result, IMSIonProperty property, MoleculeMsReference reference, MsRefSearchParameterBase parameter) {
            var ms1Tol = CalculateMassTolerance(parameter.Ms1Tolerance, property.PrecursorMz);
            result.IsPrecursorMzMatch = Math.Abs(property.PrecursorMz - reference.PrecursorMz) <= ms1Tol;

            if (parameter.IsUseCcsForAnnotationScoring) {
                var diff = Math.Abs(property.CollisionCrossSection - reference.CollisionCrossSection);
                result.IsCcsMatch = diff <= Math.Min(MsdialCcsMatchThreshold, parameter.CcsTolerance);
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
                if (result.TotalScore < parameter.TotalScoreCutoff) {
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
