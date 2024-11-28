using CompMs.Common.Components;
using CompMs.Common.DataObj;
using CompMs.Common.DataObj.Property;
using CompMs.Common.DataObj.Result;
using CompMs.Common.Extension;
using CompMs.Common.Parameter;
using CompMs.MsdialCore.Algorithm;
using CompMs.MsdialCore.Algorithm.Annotation;
using CompMs.MsdialCore.DataObj;
using CompMs.MsdialCore.MSDec;
using CompMs.MsdialCore.Utility;
using CompMs.MsdialImmsCore.Parameter;
using System;
using System.Collections.Generic;
using System.Linq;

namespace CompMs.MsdialImmsCore.Algorithm;

public sealed class AnnotationProcess
{
    public void Run(
        IDataProvider provider,
        IReadOnlyList<ChromatogramPeakFeature> chromPeakFeatures,
        IReadOnlyList<MSDecResult> msdecResults,
        IReadOnlyList<IAnnotationQueryFactory<MsScanMatchResult>> queryFactories,
        IAnnotator<IAnnotationQuery<MsScanMatchResult>, MoleculeMsReference, MsScanMatchResult>? mspAnnotator,
        IAnnotator<IAnnotationQuery<MsScanMatchResult>, MoleculeMsReference, MsScanMatchResult>? textDBAnnotator,
        IMatchResultEvaluator<MsScanMatchResult> evaluator,
        IMatchResultRefer<MoleculeMsReference, MsScanMatchResult> refer,
        MsdialImmsParameter parameter,
        int numThreads,
        ReportProgress reporter,
        System.Threading.CancellationToken token) {

        if (chromPeakFeatures.Count != msdecResults.Count)
            throw new ArgumentException("Number of ChromatogramPeakFeature and MSDecResult are different.");
        if (mspAnnotator is null && textDBAnnotator is null && queryFactories.Count == 0) {
            reporter.Report(1d);
            return;
        }

        var spectrumList = provider.LoadMsSpectrums();
        Enumerable.Range(0, chromPeakFeatures.Count)
            .AsParallel()
            .WithCancellation(token)
            .WithDegreeOfParallelism(numThreads)
            .ForAll(i => {
                var chromPeakFeature = chromPeakFeatures[i];
                var msdecResult = msdecResults[i];
                //Console.WriteLine("mass {0}, isotope {1}", chromPeakFeature.Mass, chromPeakFeature.PeakCharacter.IsotopeWeightNumber);
                ImmsMatchMethod(chromPeakFeature, msdecResult, provider.LoadMsSpectrumFromIndex(chromPeakFeature.MS1RawSpectrumIdTop).Spectrum, queryFactories, mspAnnotator, textDBAnnotator, evaluator, refer, parameter);
                reporter.Report(i + 1, chromPeakFeatures.Count);
            });
    }

    private static void ImmsMatchMethod(
        ChromatogramPeakFeature chromPeakFeature, MSDecResult msdecResult,
        IReadOnlyList<RawPeakElement> spectrum,
        IReadOnlyList<IAnnotationQueryFactory<MsScanMatchResult>> queryFactories,
        IAnnotator<IAnnotationQuery<MsScanMatchResult>, MoleculeMsReference, MsScanMatchResult>? mspAnnotator,
        IAnnotator<IAnnotationQuery<MsScanMatchResult>, MoleculeMsReference, MsScanMatchResult>? textDBAnnotator,
        IMatchResultEvaluator<MsScanMatchResult> evaluator,
        IMatchResultRefer<MoleculeMsReference, MsScanMatchResult> refer,
        MsdialImmsParameter parameter) {
        var isotopes = DataAccess.GetIsotopicPeaks(spectrum, (float)chromPeakFeature.Mass, parameter.CentroidMs1Tolerance, parameter.PeakPickBaseParam.MaxIsotopesDetectedInMs1Spectrum);

        SetMspAnnotationResult(chromPeakFeature, msdecResult, isotopes, mspAnnotator, parameter.MspSearchParam);
        SetTextDBAnnotationResult(chromPeakFeature, msdecResult, isotopes, textDBAnnotator, parameter.TextDbSearchParam);

        foreach (var queryFactory in queryFactories) {
            SetAnnotationResult(chromPeakFeature, msdecResult, spectrum, queryFactory, evaluator);
        }
        var representative = chromPeakFeature.MatchResults.Representative;
        if (evaluator.IsReferenceMatched(representative)) {
            DataAccess.SetMoleculeMsProperty(chromPeakFeature, refer.Refer(representative), representative);
        }
        else if (evaluator.IsAnnotationSuggested(representative)) {
            DataAccess.SetMoleculeMsPropertyAsSuggested(chromPeakFeature, refer.Refer(representative), representative);
        }
    }

    private static void SetMspAnnotationResult(
        ChromatogramPeakFeature chromPeakFeature, MSDecResult msdecResult, List<IsotopicPeak> isotopes,
        IAnnotator<IAnnotationQuery<MsScanMatchResult>, MoleculeMsReference, MsScanMatchResult>? mspAnnotator, MsRefSearchParameterBase mspSearchParameter) {

        if (mspAnnotator is null)
            return;

        var candidates = new AnnotationQuery(chromPeakFeature, msdecResult, isotopes, chromPeakFeature.PeakCharacter, mspSearchParameter, mspAnnotator, ignoreIsotopicPeak: true).FindCandidates();
        var results = mspAnnotator.FilterByThreshold(candidates);
        chromPeakFeature.MSRawID2MspIDs[msdecResult.RawSpectrumID] = results.Select(result => result.LibraryIDWhenOrdered).ToList();
        var matches = mspAnnotator.SelectReferenceMatchResults(results);
        if (matches.Count > 0) {
            var best = matches.Argmax(result => result.TotalScore);
            chromPeakFeature.MSRawID2MspBasedMatchResult[msdecResult.RawSpectrumID] = best;
            chromPeakFeature.MatchResults.AddMspResult(msdecResult.RawSpectrumID, best);
            DataAccess.SetMoleculeMsProperty(chromPeakFeature, mspAnnotator.Refer(best), best);
        }
        else if (results.Count > 0) {
            var best = results.Argmax(result => result.TotalScore);
            chromPeakFeature.MSRawID2MspBasedMatchResult[msdecResult.RawSpectrumID] = best;
            chromPeakFeature.MatchResults.AddMspResult(msdecResult.RawSpectrumID, best);
            DataAccess.SetMoleculeMsPropertyAsSuggested(chromPeakFeature, mspAnnotator.Refer(best), best);
        }
    }

    private static void SetTextDBAnnotationResult(
        ChromatogramPeakFeature chromPeakFeature, MSDecResult msdecResult, List<IsotopicPeak> isotopes,
        IAnnotator<IAnnotationQuery<MsScanMatchResult>, MoleculeMsReference, MsScanMatchResult>? textDBAnnotator, MsRefSearchParameterBase textDBSearchParameter) {

        if (textDBAnnotator is null)
            return;
        var candidates = new AnnotationQuery(chromPeakFeature, msdecResult, isotopes, chromPeakFeature.PeakCharacter, textDBSearchParameter, textDBAnnotator, ignoreIsotopicPeak: false).FindCandidates();
        var results = textDBAnnotator.FilterByThreshold(candidates);
        var matches = textDBAnnotator.SelectReferenceMatchResults(results);
        chromPeakFeature.TextDbIDs.AddRange(matches.Select(result => result.LibraryIDWhenOrdered));
        chromPeakFeature.MatchResults.AddTextDbResults(matches);
        if (matches.Count > 0) {
            var best = results.Argmax(result => result.TotalScore);
            if (chromPeakFeature.TextDbBasedMatchResult == null || chromPeakFeature.TextDbBasedMatchResult.TotalScore < best.TotalScore) {
                chromPeakFeature.TextDbBasedMatchResult = best;
                DataAccess.SetTextDBMoleculeMsProperty(chromPeakFeature, textDBAnnotator.Refer(best), best);
            }
        }
    }

    private static void SetAnnotationResult(ChromatogramPeakFeature chromPeakFeature, MSDecResult msdecResult, IReadOnlyList<RawPeakElement> spectrum, IAnnotationQueryFactory<MsScanMatchResult> queryFactory, IMatchResultEvaluator<MsScanMatchResult> evaluator) {
        var candidates = queryFactory.Create(chromPeakFeature, msdecResult, spectrum, chromPeakFeature.PeakCharacter, queryFactory.PrepareParameter()).FindCandidates();
        var results = evaluator.FilterByThreshold(candidates);
        var matches = evaluator.SelectReferenceMatchResults(results);
        if (matches.Count > 0) {
            var best = evaluator.SelectTopHit(matches);
            chromPeakFeature.MatchResults.AddResult(best);
        }
        else if (results.Count > 0) {
            var best = evaluator.SelectTopHit(results);
            chromPeakFeature.MatchResults.AddResult(best);
        }
    }
}
