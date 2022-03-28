using CompMs.App.Msdial.Model.Core;
using CompMs.Common.Enum;
using CompMs.CommonMVVM;
using CompMs.MsdialCore.DataObj;
using CompMs.MsdialCore.Parameter;
using System;
using System.Reactive;

namespace CompMs.App.Msdial.Model.Setting
{
    public class MethodSettingModel : BindableBase
    {
        public MethodSettingModel(ProcessOption option, IMsdialDataStorage<ParameterBase> storage, Action<MethodSettingModel, MethodModelBase> handler, IObservable<Unit> observeParameterChanged) {
            Storage = storage ?? throw new ArgumentNullException(nameof(storage));
            this.handler = handler;

            settingModelFactory = new MethodSettingModelFactory(Storage, observeParameterChanged, option);
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

        private readonly Action<MethodSettingModel, MethodModelBase> handler;
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

        public bool Run() {
            if (Option.HasFlag(ProcessOption.PeakSpotting)) {
                if (!DataCollectionSettingModel.Commit()) {
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
                AdductIonSettingModel.Commit();
            }
            if (Option.HasFlag(ProcessOption.Alignment)) {
                AlignmentParameterSettingModel.Commit();
                if (AlignmentParameterSettingModel.ShouldRunAlignment) {
                    MobilitySettingModel?.Commit();
                    IsotopeTrackSettingModel.Commit();
                }
                else {
                    Option &= ~ProcessOption.Alignment;
                }
            }
            var method = settingModelFactory.BuildMethod();
            handler?.Invoke(this, method);
            return true;
        }
    }
}
