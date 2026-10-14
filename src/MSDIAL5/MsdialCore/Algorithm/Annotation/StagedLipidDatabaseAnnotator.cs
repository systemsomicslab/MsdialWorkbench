using CompMs.Common.Algorithm.Scoring;
using CompMs.Common.Components;
using CompMs.Common.DataObj.Result;
using CompMs.Common.FormulaGenerator.Function;
using CompMs.Common.Lipidomics;
using CompMs.Common.Parameter;
using CompMs.MsdialCore.DataObj;
using CompMs.MsdialCore.Parser;
using System;
using System.Collections.Generic;
using System.Linq;

namespace CompMs.MsdialCore.Algorithm.Annotation
{
    /// <summary>
    /// Runs the staged lipid annotation (<see cref="StagedLipidAnnotator"/>) against a
    /// <see cref="StagedLipidDatabase"/> and reports each matching seed as an <see cref="MsScanMatchResult"/>.
    /// The lipid level reached is expressed through the result's name and IsLipid*Match flags.
    /// </summary>
    public sealed class StagedLipidDatabaseAnnotator : ISerializableAnnotator<IAnnotationQuery<MsScanMatchResult>, MoleculeMsReference, MsScanMatchResult, StagedLipidDatabase>
    {
        private readonly StagedLipidDatabase _database;
        private readonly StagedLipidAnnotator _annotator;
        private readonly MsRefSearchParameterBase _parameter;
        private readonly IMatchResultEvaluator<MsScanMatchResult> _evaluator;

        public StagedLipidDatabaseAnnotator(StagedLipidDatabase database, string id, int priority, MsRefSearchParameterBase parameter) {
            _database = database ?? throw new ArgumentNullException(nameof(database));
            Id = id ?? throw new ArgumentNullException(nameof(id));
            Priority = priority;
            _parameter = parameter ?? throw new ArgumentNullException(nameof(parameter));
            _annotator = new StagedLipidAnnotator(database.Models, StagedLipidAnnotator.CreateDefaultGenerator(), new StagedLipidSpectrumEvaluator(parameter.Ms2Tolerance));
            _evaluator = new MsScanMatchResultEvaluator(parameter);
        }

        public string Id { get; }
        public int Priority { get; }
        public string Key => Id;

        public List<MsScanMatchResult> FindCandidates(IAnnotationQuery<MsScanMatchResult> query) {
            var parameter = query.Parameter ?? _parameter;
            var property = query.Property;
            var ms1Tolerance = MolecularFormulaUtility.FixMassTolerance(parameter.Ms1Tolerance, property.PrecursorMz);
            var rt = parameter.IsUseTimeForAnnotationFiltering ? property.ChromXs.RT.Value : (double?)null;
            var spectrum = (IReadOnlyList<SpectrumPeak>?)query.NormalizedScan?.Spectrum ?? Array.Empty<SpectrumPeak>();

            return _database.Library.Search(property.PrecursorMz, ms1Tolerance, rt, parameter.RtTolerance, property.IonMode)
                .Select(match => Score(query, parameter, ms1Tolerance, match, _annotator.Annotate(match.Seed.Lipid, match.Adduct, spectrum)))
                .OrderByDescending(result => result.TotalScore)
                .ToList();
        }

        private MsScanMatchResult Score(IAnnotationQuery<MsScanMatchResult> query, MsRefSearchParameterBase parameter, double ms1Tolerance, LipidSeedMatch match, StagedLipidAnnotationResult annotation) {
            var lipid = annotation.Lipid ?? match.Seed.Lipid;
            var reference = _database.GetOrAddReference(lipid, match.Adduct, match.PrecursorMz, match.Seed.RetentionTime, ReferenceSpectrum(annotation));
            var property = query.Property;
            var level = annotation.Level;

            var result = new MsScanMatchResult
            {
                Name = reference.Name,
                LibraryID = reference.ScanID,
                Source = SourceType.GeneratedLipid,
                AnnotatorID = Id,
                Priority = Priority,
                AcurateMassSimilarity = (float)MsScanMatching.GetGaussianSimilarity(property.PrecursorMz, match.PrecursorMz, ms1Tolerance),
                IsPrecursorMzMatch = Math.Abs(property.PrecursorMz - match.PrecursorMz) <= ms1Tolerance,
                IsSpectrumMatch = level >= StagedLipidLevel.Class,
                IsLipidClassMatch = level >= StagedLipidLevel.Class,
                IsLipidChainsMatch = level >= StagedLipidLevel.MolecularSpecies,
                IsLipidPositionMatch = level >= StagedLipidLevel.Position,
            };

            var scan = query.NormalizedScan;
            if (scan?.Spectrum is { Count: > 0 } && reference.Spectrum.Count > 0) {
                result.SquaredWeightedDotProduct = (float)MsScanMatching.GetWeightedDotProduct(scan, reference, parameter.Ms2Tolerance, parameter.MassRangeBegin, parameter.MassRangeEnd);
                result.SquaredSimpleDotProduct = (float)MsScanMatching.GetSimpleDotProduct(scan, reference, parameter.Ms2Tolerance, parameter.MassRangeBegin, parameter.MassRangeEnd);
                result.SquaredReverseDotProduct = (float)MsScanMatching.GetReverseDotProduct(scan, reference, parameter.Ms2Tolerance, parameter.MassRangeBegin, parameter.MassRangeEnd);
                var matched = MsScanMatching.GetMatchedPeaksScores(scan, reference, parameter.Ms2Tolerance, parameter.MassRangeBegin, parameter.MassRangeEnd);
                result.MatchedPeaksPercentage = (float)matched[0];
                result.MatchedPeaksCount = (float)matched[1];
            }

            var scores = new List<double> { result.AcurateMassSimilarity, annotation.Best?.Evaluation.Similarity ?? 0d };
            var isRtMatch = true;
            if (parameter.IsUseTimeForAnnotationScoring && match.Seed.RetentionTime is double seedRt) {
                result.RtSimilarity = (float)MsScanMatching.GetGaussianSimilarity(property.ChromXs.RT.Value, seedRt, parameter.RtTolerance);
                isRtMatch = Math.Abs(property.ChromXs.RT.Value - seedRt) <= parameter.RtTolerance;
                scores.Add(result.RtSimilarity);
            }
            result.IsRtMatch = isRtMatch;
            // Provisional: the mean of the m/z, RT (when used) and staged spectral similarities.
            result.TotalScore = (float)scores.Average();

            var isRtAccepted = !parameter.IsUseTimeForAnnotationScoring || result.IsRtMatch;
            result.IsReferenceMatched = result.IsPrecursorMzMatch && isRtAccepted && result.IsSpectrumMatch;
            result.IsAnnotationSuggested = result.IsPrecursorMzMatch && isRtAccepted && !result.IsReferenceMatched;
            return result;
        }

        // The ions the reported level stands on: everything for a structure with chains, and only the
        // chain-independent ions when just the class (or nothing) was confirmed.
        private static IReadOnlyList<SpectrumPeak> ReferenceSpectrum(StagedLipidAnnotationResult annotation) {
            if (annotation.Best is null) {
                return Array.Empty<SpectrumPeak>();
            }
            var includeChains = annotation.Level >= StagedLipidLevel.MolecularSpecies;
            return annotation.Best.Evaluation.Matches
                .Select(m => m.Fragment)
                .Where(f => f.Rule.IsVisible && (includeChains || f.Rule.Kind != LipidFragmentKind.ChainIon))
                .Select(f => new SpectrumPeak(f.Mz, f.Intensity, f.Rule.Comment))
                .OrderBy(p => p.Mass)
                .ToArray();
        }

        public MoleculeMsReference? Refer(MsScanMatchResult result) => _database.Refer(result);

        public IReferRestorationKey<IAnnotationQuery<MsScanMatchResult>, MoleculeMsReference, MsScanMatchResult, StagedLipidDatabase> Save() {
            return new StagedLipidRestorationKey(Id, Priority, _parameter);
        }

        public MsScanMatchResult SelectTopHit(IEnumerable<MsScanMatchResult> results) => _evaluator.SelectTopHit(results);
        public List<MsScanMatchResult> FilterByThreshold(IEnumerable<MsScanMatchResult> results) => _evaluator.FilterByThreshold(results);
        public List<MsScanMatchResult> SelectReferenceMatchResults(IEnumerable<MsScanMatchResult> results) => _evaluator.SelectReferenceMatchResults(results);
        public bool IsReferenceMatched(MsScanMatchResult result) => _evaluator.IsReferenceMatched(result);
        public bool IsAnnotationSuggested(MsScanMatchResult result) => _evaluator.IsAnnotationSuggested(result);
    }
}
