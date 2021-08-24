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
    public class ImmsTextDBAnnotator : StandardRestorableBase, ISerializableAnnotator<IAnnotationQuery, MoleculeMsReference, MsScanMatchResult, MoleculeDataBase>
    {
        private static readonly IComparer<IMSIonProperty> comparer = CompositeComparer.Build<IMSIonProperty>(MassComparer.Comparer, CollisionCrossSectionComparer.Comparer);

        public ImmsTextDBAnnotator(MoleculeDataBase textDB, MsRefSearchParameterBase parameter, string sourceKey)
            : base(textDB.Database, parameter, sourceKey, SourceType.TextDB) {
            this.db.Sort(comparer);
            this.ReferObject = textDB;
        }

        private readonly IMatchResultRefer<MoleculeMsReference, MsScanMatchResult> ReferObject;

        public MsScanMatchResult Annotate(IAnnotationQuery query) {
            var parameter = query.Parameter ?? Parameter;
            return FindCandidatesCore(query.Property, query.Isotopes, parameter, Key).FirstOrDefault();
        }

        public List<MsScanMatchResult> FindCandidates(IAnnotationQuery query) {
            var parameter = query.Parameter ?? Parameter;
            return FindCandidatesCore(query.Property, query.Isotopes, parameter, Key);
        }

        private List<MsScanMatchResult> FindCandidatesCore(
            IMSIonProperty property, IReadOnlyList<IsotopicPeak> isotopes,
            MsRefSearchParameterBase parameter, string sourceKey) {
            var candidates = parameter.IsUseCcsForAnnotationFiltering
                ? SearchWithCcsCore(property, parameter.Ms1Tolerance, parameter.CcsTolerance)
                : SearchCore(property, parameter.Ms1Tolerance);
            var results = new List<MsScanMatchResult>(candidates.Count);
            foreach (var candidate in candidates) {
                var result = CalculateScoreCore(property, isotopes, candidate, candidate.IsotopicPeaks, parameter, sourceKey);
                ValidateCore(result, property, candidate, parameter);
                results.Add(result);
            }
            return results.OrderByDescending(result => result.TotalScore).ToList();
        }

        public MsScanMatchResult CalculateScore(IAnnotationQuery query, MoleculeMsReference reference) {
            var parameter = query.Parameter ?? Parameter;
            return CalculateScoreCore(query.Property, query.Isotopes, reference, reference.IsotopicPeaks, parameter, Key);
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
            var scores = new List<float> { };
            if (result.AcurateMassSimilarity >= 0)
                scores.Add(result.AcurateMassSimilarity);
            if (parameter.IsUseCcsForAnnotationScoring && result.CcsSimilarity >= 0)
               scores.Add(result.CcsSimilarity);
            if (result.IsotopeSimilarity >= 0)
                scores.Add(result.IsotopeSimilarity);
            return scores.DefaultIfEmpty().Average();
        }

        public override MoleculeMsReference Refer(MsScanMatchResult result) {
            return ReferObject.Refer(result);
        }

        public List<MoleculeMsReference> Search(IAnnotationQuery query) {
            var parameter = query.Parameter ?? Parameter;
            return parameter.IsUseCcsForAnnotationFiltering
                ? SearchWithCcsCore(query.Property, parameter.Ms1Tolerance, parameter.CcsTolerance).ToList()
                : SearchCore(query.Property, parameter.Ms1Tolerance).ToList();
        }

        private MassReferenceSearcher<MoleculeMsReference> Searcher
            => searcher ?? (searcher = new MassReferenceSearcher<MoleculeMsReference>(db));
        private MassReferenceSearcher<MoleculeMsReference> searcher;
        private IReadOnlyList<MoleculeMsReference> SearchCore(IMSIonProperty property, double massTolerance) {
            return Searcher.Search(new MassSearchQuery(property.PrecursorMz, CalculateMassTolerance(massTolerance, property.PrecursorMz)));
        }

        private MassCcsReferenceSearcher<MoleculeMsReference> SearcherWithCcs
            => searcherWithCcs ?? (searcherWithCcs = new MassCcsReferenceSearcher<MoleculeMsReference>(db));
        private MassCcsReferenceSearcher<MoleculeMsReference> searcherWithCcs;
        private IReadOnlyList<MoleculeMsReference> SearchWithCcsCore(IMSIonProperty property, double massTolerance, double ccsTolerance) {
            return SearcherWithCcs.Search(MSIonSearchQuery.CreateMassCcsQuery(property.PrecursorMz, CalculateMassTolerance(massTolerance, property.PrecursorMz), property.CollisionCrossSection, ccsTolerance));
        }

        private static double CalculateMassTolerance(double tolerance, double mass) {
            if (mass <= 500)
                return tolerance;
            var ppm = Math.Abs(MolecularFormulaUtility.PpmCalculator(500.00, 500.00 + tolerance));
            return MolecularFormulaUtility.ConvertPpmToMassAccuracy(mass, ppm);
        }

        public void Validate(MsScanMatchResult result, IAnnotationQuery query, MoleculeMsReference reference) {
            var parameter = query.Parameter ?? Parameter;
            ValidateCore(result, query.Property, reference, parameter);
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
