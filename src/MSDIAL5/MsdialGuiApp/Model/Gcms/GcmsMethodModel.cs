using CompMs.App.Msdial.Model.Core;
using CompMs.App.Msdial.Model.DataObj;
using CompMs.Common.Enum;
using CompMs.Graphics.UI.ProgressBar;
using CompMs.MsdialCore.Algorithm;
using CompMs.MsdialCore.DataObj;
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
        private readonly IMessageBroker _broker;

        public GcmsMethodModel(AnalysisFileBeanModelCollection analysisFileBeanModelCollection, IMsdialDataStorage<MsdialGcmsParameter> storage, ProjectBaseParameterModel projectBaseParameter, IMessageBroker broker) : base(analysisFileBeanModelCollection, storage.AlignmentFiles, projectBaseParameter) {
            _storage = storage ?? throw new ArgumentNullException(nameof(storage));
            _broker = broker;
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
                    var processor = new FileProcess(new StandardDataProviderFactory(retry: 5, isGuiProcess: true), _storage);
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
                    var processor = new FileProcess(new StandardDataProviderFactory(retry: 5, isGuiProcess: true), _storage);
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
            throw new NotImplementedException();
        }

        protected override IAlignmentModel LoadAlignmentFileCore(AlignmentFileBean alignmentFile) {
            throw new NotImplementedException();
        }

        protected override IAnalysisModel LoadAnalysisFileCore(AnalysisFileBeanModel analysisFile) {
            if (SelectedAnalysisModel != null) {
                ((IDisposable)SelectedAnalysisModel).Dispose();
                Disposables.Remove(SelectedAnalysisModel);
            }
            return SelectedAnalysisModel = new GcmsAnalysisModel(analysisFile);
        }
    }
}
