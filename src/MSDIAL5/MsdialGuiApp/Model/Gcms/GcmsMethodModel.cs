using CompMs.App.Msdial.Model.Core;
using CompMs.App.Msdial.Model.DataObj;
using CompMs.Common.Components;
using CompMs.Common.Enum;
using CompMs.Graphics.UI.ProgressBar;
using CompMs.MsdialCore.Algorithm;
using CompMs.MsdialCore.Algorithm.Annotation;
using CompMs.MsdialCore.DataObj;
using CompMs.MsdialCore.Parser;
using CompMs.MsdialGcMsApi.Algorithm.Alignment;
using CompMs.MsdialGcMsApi.Parameter;
using CompMs.MsdialGcMsApi.Process;
using Reactive.Bindings.Notifiers;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace CompMs.App.Msdial.Model.Gcms
{
    internal sealed class GcmsMethodModel : MethodModelBase
    {
        private readonly IMsdialDataStorage<MsdialGcmsParameter> _storage;
        private readonly ProjectBaseParameterModel _projectBaseParameter;
        private readonly FacadeMatchResultEvaluator _evaluator;
        private readonly IMessageBroker _broker;
        private readonly StandardDataProviderFactory _providerFactory;
        private static readonly ChromatogramSerializer<ChromatogramSpotInfo> CHROMATOGRAM_SPOT_SERIALIZER;

        static GcmsMethodModel() {
            CHROMATOGRAM_SPOT_SERIALIZER = ChromatogramSerializerFactory.CreateSpotSerializer("CSS1", ChromXType.RT);
            //CHROMATOGRAM_SPOT_SERIALIZER = ChromatogramSerializerFactory.CreateSpotSerializer("CSS1", ChromXType.RI);
        }
        public GcmsMethodModel(AnalysisFileBeanModelCollection analysisFileBeanModelCollection, AlignmentFileBeanModelCollection alignmentFiles, IMsdialDataStorage<MsdialGcmsParameter> storage, ProjectBaseParameterModel projectBaseParameter, IMessageBroker broker) : base(analysisFileBeanModelCollection, alignmentFiles, projectBaseParameter) {
            _storage = storage ?? throw new ArgumentNullException(nameof(storage));
            _projectBaseParameter = projectBaseParameter;
            _evaluator = FacadeMatchResultEvaluator.FromDataBases(storage.DataBases);
            _broker = broker;
            _providerFactory = new StandardDataProviderFactory(retry: 5, isGuiProcess: true);
        }

        public GcmsAnalysisModel SelectedAnalysisModel {
            get => _selectedAnalysisModel;
            set => SetProperty(ref _selectedAnalysisModel, value);
        }
        private GcmsAnalysisModel _selectedAnalysisModel;

        public override Task RunAsync(ProcessOption option, CancellationToken token) {
            if (option.HasFlag(ProcessOption.PeakSpotting | ProcessOption.Identification)) {
                if (!RunFromPeakSpotting()) {
                    return Task.CompletedTask;
                }
            }
            else if (option.HasFlag(ProcessOption.Identification)) {
                if (!RunFromIdentification()) {
                    return Task.CompletedTask;
                }
            }

            if (option.HasFlag(ProcessOption.Alignment)) {
                if (!RunAlignment()) {
                    return Task.CompletedTask;
                }
            }
            return Task.CompletedTask;
        }

        private bool RunFromPeakSpotting() {
            var request = new ProgressBarMultiContainerRequest(
                vm_ =>
                {
                    var processor = new FileProcess(_providerFactory, _storage);
                    var runner = new ProcessRunner(processor);
                    return runner.RunAllAsync(
                        _storage.AnalysisFiles,
                        Enumerable.Repeat((Action<int>)null, _storage.AnalysisFiles.Count),
                        Math.Max(1, _storage.Parameter.ProcessBaseParam.UsableNumThreads / 2),
                        null,
                        default);
                },
                _storage.AnalysisFiles.Select(file => file.AnalysisFileName).ToArray());
            _broker.Publish(request);
            return request.Result ?? false;
        }

        private bool RunFromIdentification() {
            var request = new ProgressBarMultiContainerRequest(
                vm_ =>
                {
                    var processor = new FileProcess(_providerFactory, _storage);
                    var runner = new ProcessRunner(processor);
                    return runner.AnnotateAllAsync(
                        _storage.AnalysisFiles,
                        Enumerable.Repeat((Action<int>)null, _storage.AnalysisFiles.Count),
                        Math.Max(1, _storage.Parameter.ProcessBaseParam.UsableNumThreads / 2),
                        null,
                        default);
                },
                _storage.AnalysisFiles.Select(file => file.AnalysisFileName).ToArray());
            _broker.Publish(request);
            return request.Result ?? false;
        }

        private bool RunAlignment() {
            var request = new ProgressBarRequest("Process alignment..", isIndeterminate: false,
                async vm =>
                {
                    var factory = new GcmsAlignmentProcessFactory(_storage.AnalysisFiles, _storage, _evaluator);
                    var aligner = factory.CreatePeakAligner();
                    aligner.ProviderFactory = _providerFactory; // TODO: I'll remove this later.

                    var alignmentFileModel = AlignmentFiles.Files.Last();
                    var result = await Task.Run(() => alignmentFileModel.RunAlignment(aligner, CHROMATOGRAM_SPOT_SERIALIZER)).ConfigureAwait(false);

                    var tasks = new[]
                    {
                        alignmentFileModel.SaveAlignmentResultAsync(result),
                        alignmentFileModel.SaveMSDecResultsAsync(alignmentFileModel.LoadMSDecResultsFromEachFiles(result.AlignmentSpotProperties)),
                    };
                    await Task.WhenAll(tasks).ConfigureAwait(false);
                });
            _broker.Publish(request);
            return request.Result ?? false;
        }

        protected override IAlignmentModel LoadAlignmentFileCore(AlignmentFileBeanModel alignmentFileModel) {
            throw new NotImplementedException();
        }

        protected override IAnalysisModel LoadAnalysisFileCore(AnalysisFileBeanModel analysisFile) {
            if (SelectedAnalysisModel != null) {
                ((IDisposable)SelectedAnalysisModel).Dispose();
                Disposables.Remove(SelectedAnalysisModel);
            }
            var providerFactory = _providerFactory.ContraMap((AnalysisFileBeanModel fileModel) => fileModel.File);
            return SelectedAnalysisModel = new GcmsAnalysisModel(analysisFile, providerFactory, _storage.Parameter.ProjectParam, _storage.Parameter.PeakPickBaseParam, _storage.Parameter.ChromDecBaseParam, _storage.DataBaseMapper, _storage.DataBases, _projectBaseParameter, _broker);
        }
    }
}
