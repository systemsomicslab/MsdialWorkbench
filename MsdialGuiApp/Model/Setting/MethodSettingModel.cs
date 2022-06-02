using CompMs.App.Msdial.Model.Core;
using CompMs.Common.Enum;
using CompMs.CommonMVVM;
using CompMs.MsdialCore.DataObj;
using CompMs.MsdialCore.Parameter;
using Reactive.Bindings.Notifiers;
using System;
using System.Reactive;
using System.Threading;
using System.Threading.Tasks;

namespace CompMs.App.Msdial.Model.Setting
{
    public class MethodSettingModel : BindableBase
    {
        public MethodSettingModel(ProcessOption option, IMsdialDataStorage<ParameterBase> storage, Func<MethodSettingModel, IMethodModel, CancellationToken, Task> asyncHandler, IObservable<Unit> observeParameterChanged, IMessageBroker broker) {
            Storage = storage ?? throw new ArgumentNullException(nameof(storage));
            _asyncHandler = asyncHandler;

            settingModelFactory = new MethodSettingModelFactory(Storage, observeParameterChanged, option, broker);
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
        private readonly IMethodSettingModelFactory settingModelFactory;

        public ProcessOption Option {
            get => option;
            set => SetProperty(ref option, value);
        }
        private ProcessOption option;

        public DataCollectionSettingModel DataCollectionSettingModel { get; }

        public PeakDetectionSettingModel PeakDetectionSettingModel { get; }

        public DeconvolutionSettingModel DeconvolutionSettingModel { get; }

        public IdentifySettingModel IdentifySettingModel { get; }

        public AdductIonSettingModel AdductIonSettingModel { get; }

        public AlignmentParameterSettingModel AlignmentParameterSettingModel { get; }

        public MobilitySettingModel MobilitySettingModel { get; }

        public IsotopeTrackSettingModel IsotopeTrackSettingModel { get; }

        public bool IsReadOnlyPeakPickParameter { get; }

        public bool IsReadOnlyAnnotationParameter { get; }

        public bool IsReadOnlyAlignmentParameter { get; }

        public async Task<bool> TryRunAsync(CancellationToken token) {
            if (Option.HasFlag(ProcessOption.PeakSpotting)) {
                if (!DataCollectionSettingModel.TryCommit()) {
                    return false;
                }
                PeakDetectionSettingModel.Commit();
                DeconvolutionSettingModel.Commit();
            }
            if (Option.HasFlag(ProcessOption.Identification)) {
                if (!IdentifySettingModel.IsReadOnly) {
                    Storage.DataBases = IdentifySettingModel.Create();
                    Storage.DataBaseMapper = Storage.DataBases.CreateDataBaseMapper();
                }
                if (!AdductIonSettingModel.TryCommit()) {
                    return false;
                }
            }
            if (Option.HasFlag(ProcessOption.Alignment)) {
                if (!AlignmentParameterSettingModel.TryCommit()) {
                    return false;
                }
                if (!AlignmentParameterSettingModel.ShouldRunAlignment) {
                    Option &= ~ProcessOption.Alignment;
                }
            }
            IsotopeTrackSettingModel.Commit();
            if (MobilitySettingModel != null && !MobilitySettingModel.TryCommit()) {
                return false;
            }
            var method = settingModelFactory.BuildMethod();
            if (!(_asyncHandler is null)) {
                await _asyncHandler(this, method, token).ConfigureAwait(false);
            }
            return true;
        }
    }
}
