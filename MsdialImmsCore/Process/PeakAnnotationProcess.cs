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
        private readonly IAnnotator<IAnnotationQuery<MsScanMatchResult>, MoleculeMsReference, MsScanMatchResult> _mspAnnotator;
        private readonly IAnnotator<IAnnotationQuery<MsScanMatchResult>, MoleculeMsReference, MsScanMatchResult> _textDBAnnotator;
        private readonly IMatchResultEvaluator<MsScanMatchResult> _evaluator;

        public PeakAnnotationProcess(
            IMsdialDataStorage<MsdialImmsParameter> storage,
            IMatchResultEvaluator<MsScanMatchResult> evaluator,
            IAnnotator<IAnnotationQuery<MsScanMatchResult>, MoleculeMsReference, MsScanMatchResult> mspAnnotator,
            IAnnotator<IAnnotationQuery<MsScanMatchResult>, MoleculeMsReference, MsScanMatchResult> textDBAnnotator) {
            _storage = storage ?? throw new ArgumentNullException(nameof(storage));
            _mspAnnotator = mspAnnotator;
            _textDBAnnotator = textDBAnnotator;
            _evaluator = evaluator ?? throw new ArgumentNullException(nameof(evaluator));
        }

        public void Annotate(
            IDataProvider provider,
            IReadOnlyList<ChromatogramPeakFeature> chromPeakFeatures,
            IReadOnlyList<MSDecResultCollection> mSDecResultCollections,
            Action<int> reportAction,
            CancellationToken token) {

            var parameter = _storage.Parameter;
            var annotatorContainers = _storage.DataBases.MetabolomicsDataBases.SelectMany(Item => Item.Pairs.Select(pair => pair.ConvertToAnnotatorContainer())).ToArray();
            PeakAnnotation(mSDecResultCollections, provider, chromPeakFeatures, annotatorContainers, _mspAnnotator, _textDBAnnotator, parameter, reportAction, token);

            // characterizatin
            PeakCharacterization(mSDecResultCollections, provider, chromPeakFeatures, _evaluator, parameter, reportAction);
        }

        public void Annotate(
            IDataProvider provider,
            IReadOnlyList<ChromatogramPeakFeature> chromPeakFeatures,
            Dictionary<double, List<MSDecResult>> targetCE2MSDecResults,
            Action<int> reportAction,
            CancellationToken token) {

            var parameter = _storage.Parameter;
            var annotatorContainers = _storage.DataBases.MetabolomicsDataBases.SelectMany(Item => Item.Pairs.Select(pair => pair.ConvertToAnnotatorContainer())).ToArray();
            PeakAnnotation(targetCE2MSDecResults, provider, chromPeakFeatures, annotatorContainers, _mspAnnotator, _textDBAnnotator, parameter, reportAction, token);

            // characterizatin
            PeakCharacterization(targetCE2MSDecResults, provider, chromPeakFeatures, _evaluator, parameter, reportAction);
        }

        private static void PeakAnnotation(
            IReadOnlyList<MSDecResultCollection> mSDecResultCollections,
            IDataProvider provider,
            IReadOnlyList<ChromatogramPeakFeature> chromPeakFeatures,
            IReadOnlyCollection<IAnnotatorContainer<IAnnotationQuery<MsScanMatchResult>, MoleculeMsReference, MsScanMatchResult>> annotatorContainers,
            IAnnotator<IAnnotationQuery<MsScanMatchResult>, MoleculeMsReference, MsScanMatchResult> mspAnnotator,
            IAnnotator<IAnnotationQuery<MsScanMatchResult>, MoleculeMsReference, MsScanMatchResult> textDBAnnotator,
            MsdialImmsParameter parameter,
            Action<int> reportAction, CancellationToken token) {

            var initial_annotation = 60.0;
            var max_annotation = 30.0;
            foreach (var (ce2msdecs, index) in mSDecResultCollections.WithIndex()) {
                var targetCE = ce2msdecs.CollisionEnergy;
                var msdecResults = ce2msdecs.MSDecResults;
                var max_annotation_local = max_annotation / mSDecResultCollections.Count;
                var initial_annotation_local = initial_annotation + max_annotation_local * index;
                new AnnotationProcess(initial_annotation_local, max_annotation_local).Run(
                    provider, chromPeakFeatures, msdecResults,
                    annotatorContainers, mspAnnotator, textDBAnnotator, parameter,
                    reportAction, parameter.NumThreads, token
                );
            }
        }

        private static void PeakAnnotation(
            Dictionary<double, List<MSDecResult>> targetCE2MSDecResults,
            IDataProvider provider,
            IReadOnlyList<ChromatogramPeakFeature> chromPeakFeatures,
            IReadOnlyCollection<IAnnotatorContainer<IAnnotationQuery<MsScanMatchResult>, MoleculeMsReference, MsScanMatchResult>> annotatorContainers,
            IAnnotator<IAnnotationQuery<MsScanMatchResult>, MoleculeMsReference, MsScanMatchResult> mspAnnotator,
            IAnnotator<IAnnotationQuery<MsScanMatchResult>, MoleculeMsReference, MsScanMatchResult> textDBAnnotator,
            MsdialImmsParameter parameter,
            Action<int> reportAction, CancellationToken token) {

            var initial_annotation = 60.0;
            var max_annotation = 30.0;
            foreach (var (ce2msdecs, index) in targetCE2MSDecResults.WithIndex()) {
                var targetCE = ce2msdecs.Key;
                var msdecResults = ce2msdecs.Value;
                var max_annotation_local = max_annotation / targetCE2MSDecResults.Count;
                var initial_annotation_local = initial_annotation + max_annotation_local * index;
                new AnnotationProcess(initial_annotation_local, max_annotation_local).Run(
                    provider, chromPeakFeatures, msdecResults,
                    annotatorContainers, mspAnnotator, textDBAnnotator, parameter,
                    reportAction, parameter.NumThreads, token
                );
            }
        }

        private static void PeakCharacterization(
            IReadOnlyList<MSDecResultCollection> mSDecResultCollections,
            IDataProvider provider,
            IReadOnlyList<ChromatogramPeakFeature> chromPeakFeatures,
            IMatchResultEvaluator<MsScanMatchResult> evaluator,
            MsdialImmsParameter parameter,
            Action<int> reportAction) {

            new PeakCharacterEstimator(90, 10).Process(provider, chromPeakFeatures, mSDecResultCollections.Any() ? mSDecResultCollections.Argmin(kvp => kvp.CollisionEnergy).MSDecResults : null,
                evaluator,
                parameter, reportAction);
        }

        private static void PeakCharacterization(
            Dictionary<double, List<MSDecResult>> targetCE2MSDecResults,
            IDataProvider provider,
            IReadOnlyList<ChromatogramPeakFeature> chromPeakFeatures,
            IMatchResultEvaluator<MsScanMatchResult> evaluator,
            MsdialImmsParameter parameter,
            Action<int> reportAction) {

            new PeakCharacterEstimator(90, 10).Process(provider, chromPeakFeatures, targetCE2MSDecResults.Any() ? targetCE2MSDecResults.Argmin(kvp => kvp.Key).Value : null,
                evaluator,
                parameter, reportAction);
        }
    }
}
