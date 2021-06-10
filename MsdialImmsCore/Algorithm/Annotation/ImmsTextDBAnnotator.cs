using CompMs.Common.Algorithm.Scoring;
using CompMs.Common.Components;
using CompMs.Common.DataObj.Property;
using CompMs.Common.DataObj.Result;
using CompMs.Common.FormulaGenerator.Function;
using CompMs.Common.Interfaces;
using CompMs.Common.Parameter;
using CompMs.Common.Utility;
using CompMs.MsdialCore.Algorithm;
using CompMs.MsdialCore.Algorithm.Annotation;
using CompMs.MsdialCore.MSDec;
using System;
using System.Collections.Generic;
using System.Linq;

namespace CompMs.MsdialImmsCore.Algorithm.Annotation
{
    public class ImmsTextDBAnnotator<T> : IAnnotator<T, MSDecResult>
        where T : IMSProperty, IIonProperty
    {
        private static readonly IComparer<IMSScanProperty> comparer = CompositeComparer.Build(MassComparer.Comparer, ChromXsComparer.DriftComparer);

        private readonly List<MoleculeMsReference> textDB;

        public string Key { get; }

        public MsRefSearchParameterBase Parameter { get; }

        public ImmsTextDBAnnotator(IEnumerable<MoleculeMsReference> textDB, MsRefSearchParameterBase parameter, string sourceKey) {
            this.textDB = textDB.ToList();
            this.textDB.Sort(comparer);
            this.Parameter = parameter;
            this.Key = sourceKey;
            this.ReferObject = new DataBaseRefer(this.textDB);
        }

        public MsScanMatchResult Annotate(
            T property, MSDecResult scan, IReadOnlyList<IsotopicPeak> isotopes,
            MsRefSearchParameterBase parameter = null) {

            if (parameter == null)
                parameter = Parameter;
            return FindCandidatesCore(property, isotopes, parameter, textDB, Key).FirstOrDefault();
        }

        public List<MsScanMatchResult> FindCandidates(
            T property, MSDecResult scan, IReadOnlyList<IsotopicPeak> isotopes,
            MsRefSearchParameterBase parameter = null) {

            if (parameter == null)
                parameter = Parameter;

            return FindCandidatesCore(property, isotopes, parameter, textDB, Key);
        }

        private static List<MsScanMatchResult> FindCandidatesCore(
            T property, IReadOnlyList<IsotopicPeak> isotopes,
            MsRefSearchParameterBase parameter, IReadOnlyList<MoleculeMsReference> textDB, string sourceKey) {

            (var lo, var hi) = SearchBoundIndex(property, textDB, parameter.Ms1Tolerance);
            var results = new List<MsScanMatchResult>(hi - lo);
            for (var i = lo; i < hi; i++) {
                var candidate = textDB[i];
                if (candidate.ChromXs.Drift.Value < property.ChromXs.Drift.Value - parameter.CcsTolerance
                    || property.ChromXs.Drift.Value + parameter.CcsTolerance < candidate.ChromXs.Drift.Value)
                    continue;
                var result = CalculateScoreCore(property, isotopes, candidate, candidate.IsotopicPeaks, parameter, sourceKey);
                result.LibraryIDWhenOrdered = i;
                ValidateCore(result, property, candidate, parameter);
                results.Add(result);
            }
            return results.OrderByDescending(result => result.TotalScore).ToList();
        }

        public MsScanMatchResult CalculateScore(
            T property, MSDecResult scan, IReadOnlyList<IsotopicPeak> isotopes,
            MoleculeMsReference reference, MsRefSearchParameterBase parameter = null) {

            if (parameter == null)
                parameter = Parameter;
            return CalculateScoreCore(property, isotopes, reference, reference.IsotopicPeaks, parameter, Key);
        }

        private static MsScanMatchResult CalculateScoreCore(
            T property, IReadOnlyList<IsotopicPeak> scanIsotopes,
            MoleculeMsReference reference, IReadOnlyList<IsotopicPeak> referenceIsotopes,
            MsRefSearchParameterBase parameter, string sourceKey) {

            var ms1Tol = CalculateMassTolerance(parameter.Ms1Tolerance, property.PrecursorMz);
            var ms1Similarity = MsScanMatching.GetGaussianSimilarity(property.PrecursorMz, reference.PrecursorMz, ms1Tol);

            var ccsSimilarity = MsScanMatching.GetGaussianSimilarity(property.CollisionCrossSection, reference.CollisionCrossSection, parameter.CcsTolerance);

            var isotopeSimilarity = MsScanMatching.GetIsotopeRatioSimilarity(scanIsotopes, referenceIsotopes, property.PrecursorMz, ms1Tol);

            var result = new MsScanMatchResult
            {
                Name = reference.Name, LibraryID = reference.ScanID, InChIKey = reference.InChIKey,
                AcurateMassSimilarity = (float)ms1Similarity, CcsSimilarity = (float)ccsSimilarity, IsotopeSimilarity = (float)isotopeSimilarity,
                Source = SourceType.TextDB, SourceKey = sourceKey
            };

            var scores = new List<float> { };
            if (result.AcurateMassSimilarity >= 0)
                scores.Add(result.AcurateMassSimilarity);
            if (result.CcsSimilarity >= 0)
                scores.Add(result.CcsSimilarity);
            if (result.IsotopeSimilarity >= 0)
                scores.Add(result.IsotopeSimilarity);
            result.TotalScore = scores.DefaultIfEmpty().Average();

            return result;
        }

        public IMatchResultRefer ReferObject { get; }
        public MoleculeMsReference Refer(MsScanMatchResult result) {
            return ReferObject.Refer(result);
        }

        public List<MoleculeMsReference> Search(T property, MsRefSearchParameterBase parameter = null) {
            if (parameter == null)
                parameter = Parameter;

            (var lo, var hi) = SearchBoundIndex(property, textDB, parameter.Ms1Tolerance);
            return textDB.GetRange(lo, hi - lo);
        }

        private static (int lo, int hi) SearchBoundIndex(T property, IReadOnlyList<MoleculeMsReference> textDB, double ms1Tolerance) {
            ms1Tolerance = CalculateMassTolerance(ms1Tolerance, property.PrecursorMz);
            var dummy = new MSScanProperty { PrecursorMz = property.PrecursorMz - ms1Tolerance };
            var lo = SearchCollection.LowerBound(textDB, dummy, comparer);
            dummy.PrecursorMz = property.PrecursorMz + ms1Tolerance;
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
            T property, MSDecResult scan, IReadOnlyList<IsotopicPeak> isotopes,
            MoleculeMsReference reference,
            MsRefSearchParameterBase parameter = null) {

            if (parameter == null)
                parameter = Parameter;
            ValidateCore(result, property, reference, parameter);
        }

        private static void ValidateCore(MsScanMatchResult result, T property, MoleculeMsReference reference, MsRefSearchParameterBase parameter) {
            ValidateBase(result, property, reference, parameter);
        }

        private static void ValidateBase(MsScanMatchResult result, T property, MoleculeMsReference reference, MsRefSearchParameterBase parameter) {
            var ms1Tol = CalculateMassTolerance(parameter.Ms1Tolerance, property.PrecursorMz);
            result.IsPrecursorMzMatch = Math.Abs(property.PrecursorMz - reference.PrecursorMz) <= ms1Tol;

            result.IsCcsMatch = Math.Abs(property.CollisionCrossSection - reference.CollisionCrossSection) <= parameter.CcsTolerance;
        }
    }
}
