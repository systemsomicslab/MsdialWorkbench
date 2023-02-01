using CompMs.Common.Components;
using CompMs.Common.DataObj.Result;
using CompMs.Common.Extension;
using CompMs.MsdialCore.Algorithm;
using CompMs.MsdialCore.Algorithm.Annotation;
using CompMs.MsdialCore.DataObj;
using CompMs.MsdialCore.MSDec;
using CompMs.MsdialCore.Utility;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace CompMs.MsdialLcImMsApi.Algorithm.Annotation
{
    public sealed class LcimmsStandardAnnotationProcess : IAnnotationProcess
    {
        public LcimmsStandardAnnotationProcess(IReadOnlyList<IAnnotationQueryFactory<MsScanMatchResult>> queryFactories, IMatchResultEvaluator<MsScanMatchResult> evaluator, IMatchResultRefer<MoleculeMsReference, MsScanMatchResult> refer) {
            _queryFacotries = queryFactories ?? throw new ArgumentNullException(nameof(queryFactories));
            _evaluator = evaluator ?? throw new ArgumentNullException(nameof(evaluator));
            _refer = refer ?? throw new ArgumentNullException(nameof(refer));
        }

        private readonly IReadOnlyList<IAnnotationQueryFactory<MsScanMatchResult>> _queryFacotries;
        private readonly IMatchResultEvaluator<MsScanMatchResult> _evaluator;
        private readonly IMatchResultRefer<MoleculeMsReference, MsScanMatchResult> _refer;

        public void RunAnnotation(IReadOnlyList<ChromatogramPeakFeature> chromPeakFeatures, IReadOnlyList<MSDecResult> msdecResult, IDataProvider provider, int numThread = 1, CancellationToken token = default, Action<double> reportAction = null) {
            var reporter = ReportProgress.FromRange(reportAction, 0, 1);
            foreach (var (peak, idx) in chromPeakFeatures.WithIndex()) {
                RunAnnotationCore(peak, msdecResult, provider);
                reporter.Show(idx + 1, chromPeakFeatures.Count);
            }
        }

        public Task RunAnnotationAsync(IReadOnlyList<ChromatogramPeakFeature> chromPeakFeatures, IReadOnlyList<MSDecResult> msdecResult, IDataProvider provider, int numThread = 1, CancellationToken token = default, Action<double> reportAction = null) {
            var reporter = ReportProgress.FromRange(reportAction, 0, 1);
            var queue = new ConcurrentQueue<ChromatogramPeakFeature>(chromPeakFeatures);
            var tasks = new Task[Math.Max(numThread, 1)];
            for (int i = 0; i < tasks.Length; i++) {
                tasks[i] = ConsumeTasksAsync(queue, msdecResult, provider, token, reporter, chromPeakFeatures.Count);
            }
            return Task.WhenAll(tasks);
        }

        private async Task ConsumeTasksAsync(ConcurrentQueue<ChromatogramPeakFeature> queue, IReadOnlyList<MSDecResult> msdecResults, IDataProvider provider, CancellationToken token, ReportProgress reporter, int totalNumber) {
            while (queue.TryDequeue(out var peak)) {
                token.ThrowIfCancellationRequested();
                await Task.Run(() => RunAnnotationCore(peak, msdecResults, provider), token).ConfigureAwait(false);
                reporter.Show(totalNumber - queue.Count, totalNumber);
            }
        }

        private void RunAnnotationCore(ChromatogramPeakFeature chromatogramPeakFeature, IReadOnlyList<MSDecResult> msdecResults, IDataProvider provider) {
            foreach (var dpeak in chromatogramPeakFeature.DriftChromFeatures) {
                var msdecResult = msdecResults[dpeak.MasterPeakID]; 
                if (!msdecResult.Spectrum.IsEmptyOrNull()) {
                    dpeak.MSDecResultIdUsed = dpeak.MasterPeakID;
                }
                RunInnerAnnotationCore(chromatogramPeakFeature, dpeak, msdecResult, provider);
            }
            chromatogramPeakFeature.SetMatchResultProperty(_refer, _evaluator);
        }

        private void RunInnerAnnotationCore(ChromatogramPeakFeature parentChromatogramPeakFeature, ChromatogramPeakFeature chromatogramPeakFeature, MSDecResult msdecResult, IDataProvider provider) {
            var spectrum = provider.LoadMsSpectrumFromIndex(chromatogramPeakFeature.MS1RawSpectrumIdTop).Spectrum;
            foreach (var factory in _queryFacotries) {
                var query = factory.Create(chromatogramPeakFeature, msdecResult, spectrum, chromatogramPeakFeature.PeakCharacter, factory.PrepareParameter());
                var candidates = query.FindCandidates();
                var results = _evaluator.FilterByThreshold(candidates);
                var matches = _evaluator.SelectReferenceMatchResults(results);
                if (matches.Count > 0) {
                    var best = _evaluator.SelectTopHit(matches);
                    best.IsReferenceMatched = true;
                    chromatogramPeakFeature.MatchResults.AddResult(best);
                    parentChromatogramPeakFeature.MatchResults.AddResult(best);
                }
                else if (results.Count > 0) {
                    var best = _evaluator.SelectTopHit(results);
                    best.IsAnnotationSuggested = true;
                    chromatogramPeakFeature.MatchResults.AddResult(best);
                    parentChromatogramPeakFeature.MatchResults.AddResult(best);
                }
            }
            chromatogramPeakFeature.SetMatchResultProperty(_refer, _evaluator);
        }
    }
}
