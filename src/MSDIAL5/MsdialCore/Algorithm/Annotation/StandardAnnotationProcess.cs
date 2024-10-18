using CompMs.Common.Components;
using CompMs.Common.DataObj.Result;
using CompMs.Common.Extension;
using CompMs.MsdialCore.DataObj;
using CompMs.MsdialCore.MSDec;
using CompMs.MsdialCore.Utility;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace CompMs.MsdialCore.Algorithm.Annotation;

public sealed class StandardAnnotationProcess : IAnnotationProcess
{
    private static readonly int NUMBER_OF_ANNOTATION_RESULTS = 3;

    public async Task RunAnnotationAsync(
        IReadOnlyList<ChromatogramPeakFeature> chromPeakFeatures,
        MSDecResultCollection msdecResults,
        IDataProvider provider,
        int numThreads = 1,
        Action<double>? reportAction = null,
        CancellationToken token = default) {
        var reporter = ReportProgress.FromRange(reportAction, 0d, 1d);
        if (numThreads <= 1) {
            await RunBySingleThreadAsync(chromPeakFeatures, msdecResults, provider, token, reporter).ConfigureAwait(false);
        }
        else {
            await RunByMultiThreadAsync(chromPeakFeatures, msdecResults, provider, numThreads, token, reporter).ConfigureAwait(false);
        }
    }

    public StandardAnnotationProcess(
        IAnnotationQueryFactory<MsScanMatchResult> queryFactory,
        IMatchResultEvaluator<MsScanMatchResult> evaluator,
        IMatchResultRefer<MoleculeMsReference, MsScanMatchResult> refer) {
        _queryFactories = [queryFactory];
        _evaluator = evaluator ?? throw new ArgumentNullException(nameof(evaluator));
        _refer = refer ?? throw new ArgumentNullException(nameof(refer));
    }

    public StandardAnnotationProcess(IReadOnlyList<IAnnotationQueryFactory<MsScanMatchResult>> queryFactories, IMatchResultEvaluator<MsScanMatchResult> evaluator, IMatchResultRefer<MoleculeMsReference?, MsScanMatchResult?> refer) {
        _queryFactories = queryFactories;
        _evaluator = evaluator ?? throw new ArgumentNullException(nameof(evaluator));
        _refer = refer ?? throw new ArgumentNullException(nameof(refer));
    }

    private readonly IReadOnlyList<IAnnotationQueryFactory<MsScanMatchResult>> _queryFactories;
    private readonly IMatchResultEvaluator<MsScanMatchResult> _evaluator;
    private readonly IMatchResultRefer<MoleculeMsReference?, MsScanMatchResult?> _refer;

    private async Task RunBySingleThreadAsync(
        IReadOnlyList<ChromatogramPeakFeature> chromPeakFeatures,
        MSDecResultCollection msdecResults,
        IDataProvider provider,
        CancellationToken token,
        ReportProgress reporter) {
        for (int i = 0; i < chromPeakFeatures.Count; i++) {
            var chromPeakFeature = chromPeakFeatures[i];
            var msdecResult = msdecResults.MSDecResults[i];
            await RunAnnotationCoreAsync(chromPeakFeature, msdecResult, provider, msdecResults.CollisionEnergy, token).ConfigureAwait(false);
            reporter.Report(i + 1, chromPeakFeatures.Count);
        };
    }

    private async Task RunByMultiThreadAsync(
        IReadOnlyList<ChromatogramPeakFeature> chromPeakFeatures,
        MSDecResultCollection msdecResults,
        IDataProvider provider,
        int numThreads,
        CancellationToken token,
        ReportProgress reporter) {
        using var sem = new SemaphoreSlim(numThreads);
        var annotationTasks = chromPeakFeatures.Zip(msdecResults.MSDecResults, Tuple.Create)
            .Select(async (pair, i) => {
                await sem.WaitAsync(token).ConfigureAwait(false);

                try {
                    var chromPeakFeature = pair.Item1;
                    var msdecResult = pair.Item2;
                    await RunAnnotationCoreAsync(chromPeakFeature, msdecResult, provider, msdecResults.CollisionEnergy, token);
                }
                finally {
                    sem.Release();
                    reporter.Report(i + 1, chromPeakFeatures.Count);
                }
            });
        await Task.WhenAll(annotationTasks);
    }

    private async Task RunAnnotationCoreAsync(
        ChromatogramPeakFeature chromPeakFeature,
        MSDecResult msdecResult,
        IDataProvider provider,
        double collisionEnergy,
        CancellationToken token = default) {

        if (!msdecResult.Spectrum.IsEmptyOrNull()) {
            chromPeakFeature.MSDecResultIdUsed = chromPeakFeature.MasterPeakID;
        }
        foreach (var factory in _queryFactories) {
            var query = factory.Create(
                chromPeakFeature,
                msdecResult,
                provider.LoadMsSpectrumFromIndex(chromPeakFeature.MS1RawSpectrumIdTop).Spectrum,
                chromPeakFeature.PeakCharacter,
                factory.PrepareParameter());
            token.ThrowIfCancellationRequested();
            await Task.Run(() => SetAnnotationResult(chromPeakFeature, query, msdecResult, collisionEnergy), token).ConfigureAwait(false);
        }
        token.ThrowIfCancellationRequested();
        SetRepresentativeProperty(chromPeakFeature);
    }

    private void SetAnnotationResult(ChromatogramPeakFeature chromPeakFeature, IAnnotationQuery<MsScanMatchResult> query, MSDecResult msdecResult, double collisionEnergy) {
        var candidates = query.FindCandidates();
        var results = _evaluator.FilterByThreshold(candidates);
        var topResults = _evaluator.SelectTopN(results, NUMBER_OF_ANNOTATION_RESULTS).Select(r => {
            r.CollisionEnergy = collisionEnergy;
            r.SpectrumID = msdecResult.RawSpectrumID;
            return r;
        });
        chromPeakFeature.MatchResults.AddResults(topResults);
    }

    private void SetRepresentativeProperty(ChromatogramPeakFeature chromPeakFeature) {
        var representative = chromPeakFeature.MatchResults.Representative;
        if (_evaluator is null) {
            return;
        }
        else if (_evaluator.IsReferenceMatched(representative)) {
            DataAccess.SetMoleculeMsProperty(chromPeakFeature, _refer.Refer(representative), representative);
        }
        else if (_evaluator.IsAnnotationSuggested(representative)) {
            DataAccess.SetMoleculeMsPropertyAsSuggested(chromPeakFeature, _refer.Refer(representative), representative);
        }
    }
}
