using CompMs.Common.DataObj.Result;
using CompMs.Common.Extension;
using CompMs.MsdialCore.Algorithm;
using CompMs.MsdialCore.Algorithm.Annotation;
using CompMs.MsdialCore.DataObj;
using CompMs.MsdialCore.MSDec;
using CompMs.MsdialLcmsApi.Parameter;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace CompMs.MsdialLcMsApi.Process
{
    internal sealed class PeakAnnotationProcess
    {
        private readonly IAnnotationProcess _annotationProcess;
        private readonly IMsdialDataStorage<MsdialLcmsParameter> _storage;
        private readonly IMatchResultEvaluator<MsScanMatchResult> _evaluator;

        public PeakAnnotationProcess(IAnnotationProcess annotationProcess, IMsdialDataStorage<MsdialLcmsParameter> storage, IMatchResultEvaluator<MsScanMatchResult> evaluator) {
            _annotationProcess = annotationProcess ?? throw new ArgumentNullException(nameof(annotationProcess));
            _storage = storage ?? throw new ArgumentNullException(nameof(storage));
            _evaluator = evaluator ?? throw new ArgumentNullException(nameof(evaluator));
        }

        public async Task AnnotateAsync(AnalysisFileBean file, IReadOnlyList<MSDecResultCollection> mSDecResultCollections, IReadOnlyList<ChromatogramPeakFeature> chromPeakFeatures, IDataProvider provider, CancellationToken token, Action<int> reportAction) {
            var initial_annotation = 60.0;
            var max_annotation = 30.0;
            foreach (var (ce2msdecs, index) in mSDecResultCollections.WithIndex()) {
                var targetCE = ce2msdecs.CollisionEnergy;
                var msdecResults = ce2msdecs.MSDecResults;
                var max_annotation_local = max_annotation / mSDecResultCollections.Count;
                var initial_annotation_local = initial_annotation + max_annotation_local * index;
                await _annotationProcess.RunAnnotationAsync(
                        chromPeakFeatures,
                        msdecResults,
                        provider,
                        _storage.Parameter.NumThreads == 1 ? 1 : 2,
                        v => reportAction?.Invoke((int)(initial_annotation_local + v * max_annotation_local)),
                        token).ConfigureAwait(false);
            }

            // characterizatin
            new PeakCharacterEstimator(90, 10).Process(file, provider, chromPeakFeatures, mSDecResultCollections.Any() ? mSDecResultCollections.Argmin(kvp => kvp.CollisionEnergy).MSDecResults : null, _evaluator, _storage.Parameter, reportAction);
        }
    }
}
