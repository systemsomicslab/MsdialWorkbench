using CompMs.App.Msdial.Model.Core;
using CompMs.App.Msdial.Model.DataObj;
using CompMs.App.Msdial.Model.Service;
using CompMs.Common.Enum;
using CompMs.CommonMVVM;
using CompMs.MsdialCore.DataObj;
using CompMs.MsdialCore.Parameter;
using Reactive.Bindings.Notifiers;
using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace CompMs.App.Msdial.Model.Setting
{
    internal sealed class MethodSettingModel : BindableBase
    {
        public MethodSettingModel(ProcessOption option, AnalysisFileBeanModelCollection analysisFileBeanModelCollection, AlignmentFileBeanModelCollection alignmentFileModelCollection, IMsdialDataStorage<ParameterBase> storage, Func<MethodSettingModel, IMethodModel, CancellationToken, Task> asyncHandler, FilePropertiesModel fileProperties, StudyContextModel studyContext, IMessageBroker broker) {
            Storage = storage ?? throw new ArgumentNullException(nameof(storage));
            _asyncHandler = asyncHandler;
            _broker = broker;
            settingModelFactory = new MethodSettingModelFactory(analysisFileBeanModelCollection, alignmentFileModelCollection, storage, fileProperties, studyContext, option, broker);
            DataCollectionSettingModel = settingModelFactory.CreateDataCollectionSetting();
            PeakDetectionSettingModel = settingModelFactory.CreatePeakDetectionSetting();
            DeconvolutionSettingModel = settingModelFactory.CreateDeconvolutionSetting();
            IdentifySettingModel = settingModelFactory.CreateIdentifySetting();
            AdductIonSettingModel = settingModelFactory.CreateAdductIonSetting();
            AlignmentParameterSettingModel = settingModelFactory.CreateAlignmentParameterSetting();
            MobilitySettingModel = settingModelFactory.CreateMobilitySetting();
            IsotopeTrackSettingModel = settingModelFactory.CreateIsotopeTrackSetting();

            Option = option;
            IsReadOnlyPeakPickParameter = !option.HasFlag(ProcessOption.PeakSpotting);
            IsReadOnlyAnnotationParameter = !option.HasFlag(ProcessOption.Identification);
            IsReadOnlyAlignmentParameter = !option.HasFlag(ProcessOption.Alignment);
        }

        public IMsdialDataStorage<ParameterBase> Storage { get; }

        private readonly Func<MethodSettingModel, IMethodModel, CancellationToken, Task> _asyncHandler;
        private readonly IMessageBroker _broker;
        private readonly IMethodSettingModelFactory settingModelFactory;

        public ProcessOption Option {
            get => option;
            set => SetProperty(ref option, value);
        }
        private ProcessOption option;

        public IDataCollectionSettingModel? DataCollectionSettingModel { get; }

        public IPeakDetectionSettingModel? PeakDetectionSettingModel { get; }

        public DeconvolutionSettingModel? DeconvolutionSettingModel { get; }

        public IIdentificationSettingModel? IdentifySettingModel { get; }

        public AdductIonSettingModel? AdductIonSettingModel { get; }

        public IAlignmentParameterSettingModel? AlignmentParameterSettingModel { get; }

        public MobilitySettingModel? MobilitySettingModel { get; }

        public IsotopeTrackSettingModel? IsotopeTrackSettingModel { get; }

        public bool IsReadOnlyPeakPickParameter { get; }

        public bool IsReadOnlyAnnotationParameter { get; }

        public bool IsReadOnlyAlignmentParameter { get; }

        public async Task<bool> TryRunAsync(CancellationToken token) {
            if (Option.HasFlag(ProcessOption.PeakSpotting)) {
                if (DataCollectionSettingModel is null || !DataCollectionSettingModel.TryCommit()) {
                    return false;
                }

                PeakDetectionSettingModel?.Commit();
                DeconvolutionSettingModel?.Commit();
            }
            if (Option.HasFlag(ProcessOption.Identification)) {
                if (IdentifySettingModel != null) {
                    if (!IdentifySettingModel.IsReadOnly ) {
                        Storage.DataBases = IdentifySettingModel.Create(Storage.DataBaseMapper);
                        Storage.DataBaseMapper = Storage.DataBases.CreateDataBaseMapper();
                    }
                }
                if (!(AdductIonSettingModel is null || AdductIonSettingModel.TryCommit())) {
                    return false;
                }
            }
            if (Option.HasFlag(ProcessOption.Alignment)) {
                if (AlignmentParameterSettingModel is null || !AlignmentParameterSettingModel.TryCommit()) {
                    return false;
                }

                if (!AlignmentParameterSettingModel.ShouldRunAlignment) {
                    Option &= ~ProcessOption.Alignment;
                }
            }
            IsotopeTrackSettingModel?.Commit();
            if (MobilitySettingModel != null && !MobilitySettingModel.TryCommit()) {
                return false;
            }
            var method = settingModelFactory.BuildMethod();
            if (!(_asyncHandler is null)) {
                await _asyncHandler(this, method, token).ConfigureAwait(false);
            }
            return true;
        }

        public void LoadParameter() {
            var request = new OpenFileRequest(
                file =>
                {
                    using (var stream = File.Open(file, FileMode.Open)) {
                        var parameter = Storage.LoadParameter(stream);
                        DataCollectionSettingModel?.LoadParameter(parameter);
                        PeakDetectionSettingModel?.LoadParameter(parameter.PeakPickBaseParam);
                        DeconvolutionSettingModel?.LoadParameter(parameter.ChromDecBaseParam);
                        IdentifySettingModel?.LoadParameter(parameter);
                        AdductIonSettingModel?.LoadParameter(parameter);
                        AlignmentParameterSettingModel?.LoadParameter(parameter);
                        // MobilitySettingModel?.LoadParameter();
                        // IsotopeTrackSettingModel?.LoadParameter();
                    }
                })
            {
                Title = "Load parameter file",
                Filter = "Msdial parameter file(*.mdparameter)|*.mdparameter",
            };
            _broker.Publish(request);           
        }
    }
}
