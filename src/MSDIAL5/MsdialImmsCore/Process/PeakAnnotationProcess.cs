using CompMs.Common.Components;
using CompMs.Common.DataObj.Result;
using CompMs.Common.Extension;
using CompMs.MsdialCore.Algorithm;
using CompMs.MsdialCore.Algorithm.Annotation;
using CompMs.MsdialCore.DataObj;
using CompMs.MsdialCore.MSDec;
using CompMs.MsdialImmsCore.Algorithm;
using CompMs.MsdialImmsCore.Parameter;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

namespace CompMs.MsdialImmsCore.Process
{
    internal sealed class PeakAnnotationProcess
    {
        private readonly IMsdialDataStorage<MsdialImmsParameter> _storage;
        private readonly IAnnotator<IAnnotationQuery<MsScanMatchResult>, MoleculeMsReference, MsScanMatchResult>? _mspAnnotator;
        private readonly IAnnotator<IAnnotationQuery<MsScanMatchResult>, MoleculeMsReference, MsScanMatchResult>? _textDBAnnotator;
        private readonly IMatchResultEvaluator<MsScanMatchResult> _evaluator;

        public PeakAnnotationProcess(
            IMsdialDataStorage<MsdialImmsParameter> storage,
            IMatchResultEvaluator<MsScanMatchResult> evaluator,
            IAnnotator<IAnnotationQuery<MsScanMatchResult>, MoleculeMsReference, MsScanMatchResult>? mspAnnotator,
            IAnnotator<IAnnotationQuery<MsScanMatchResult>, MoleculeMsReference, MsScanMatchResult>? textDBAnnotator) {
            _storage = storage ?? throw new ArgumentNullException(nameof(storage));
            _mspAnnotator = mspAnnotator;
            _textDBAnnotator = textDBAnnotator;
            _evaluator = evaluator ?? throw new ArgumentNullException(nameof(evaluator));
        }

        public void Annotate(
            AnalysisFileBean analysisFile,
            IDataProvider provider,
            IReadOnlyList<ChromatogramPeakFeature> chromPeakFeatures,
            IReadOnlyList<MSDecResultCollection> mSDecResultCollections,
            IProgress<int>? progress,
            CancellationToken token) {

            var parameter = _storage.Parameter;
            PeakAnnotation(mSDecResultCollections, provider, chromPeakFeatures, _storage.DataBases.CreateQueryFactories().MoleculeQueryFactories, _mspAnnotator, _textDBAnnotator, _evaluator, _storage.DataBaseMapper, parameter, progress, token);

            // characterizatin
            PeakCharacterization(analysisFile, mSDecResultCollections, provider, chromPeakFeatures, _evaluator, parameter, progress);
        }

        private static void PeakAnnotation(
            IReadOnlyList<MSDecResultCollection> mSDecResultCollections,
            IDataProvider provider,
            IReadOnlyList<ChromatogramPeakFeature> chromPeakFeatures,
            IReadOnlyList<IAnnotationQueryFactory<MsScanMatchResult>> queryFactories,
            IAnnotator<IAnnotationQuery<MsScanMatchResult>, MoleculeMsReference, MsScanMatchResult>? mspAnnotator,
            IAnnotator<IAnnotationQuery<MsScanMatchResult>, MoleculeMsReference, MsScanMatchResult>? textDBAnnotator,
            IMatchResultEvaluator<MsScanMatchResult> evaluator,
            IMatchResultRefer<MoleculeMsReference, MsScanMatchResult> refer,
            MsdialImmsParameter parameter,
            IProgress<int>? progress,
            CancellationToken token) {

            var initial_annotation = 60.0;
            var max_annotation = 30.0;
            AnnotationProcess annotationProcess = new AnnotationProcess();
            foreach (var (ce2msdecs, index) in mSDecResultCollections.WithIndex()) {
                var targetCE = ce2msdecs.CollisionEnergy;
                var msdecResults = ce2msdecs.MSDecResults;
                var max_annotation_local = max_annotation / mSDecResultCollections.Count;
                var initial_annotation_local = initial_annotation + max_annotation_local * index;
                var reporter = MsdialCore.Utility.ReportProgress.FromLength(progress, initial_annotation_local, max_annotation_local);
                annotationProcess.Run(
                    provider, chromPeakFeatures, msdecResults,
                    queryFactories, mspAnnotator, textDBAnnotator,
                    evaluator, refer,
                    parameter, parameter.NumThreads, reporter, token
                );
            }
        }

        private static void PeakCharacterization(
            AnalysisFileBean file,
            IReadOnlyList<MSDecResultCollection> mSDecResultCollections,
            IDataProvider provider,
            IReadOnlyList<ChromatogramPeakFeature> chromPeakFeatures,
            IMatchResultEvaluator<MsScanMatchResult> evaluator,
            MsdialImmsParameter parameter,
            IProgress<int>? progress) {

            new PeakCharacterEstimator(90, 10).Process(file, provider, chromPeakFeatures, mSDecResultCollections.Any() ? mSDecResultCollections.Argmin(kvp => kvp.CollisionEnergy).MSDecResults : null, evaluator, parameter, progress, true);
        }
    }
}
