using CompMs.App.Msdial.Model.Core;
using CompMs.Common.Enum;
using CompMs.CommonMVVM;
using CompMs.MsdialCore.DataObj;
using CompMs.MsdialCore.Parameter;
using CompMs.MsdialDimsCore.Parameter;
using CompMs.MsdialImmsCore.Parameter;
using CompMs.MsdialLcImMsApi.Parameter;
using CompMs.MsdialLcmsApi.Parameter;
using System;

namespace CompMs.App.Msdial.Model.Setting
{
    public class MethodSettingModel : BindableBase
    {
        public MethodSettingModel(ProcessOption option, IMsdialDataStorage<ParameterBase> storage, Action<MethodSettingModel, MethodModelBase> handler) {
            Storage = storage ?? throw new ArgumentNullException(nameof(storage));
            this.handler = handler;

            settingModelFactory = PrepareFactory(Storage);
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

        public void Run() {
            if (Option.HasFlag(ProcessOption.PeakSpotting)) {
                DataCollectionSettingModel.Commit();
                PeakDetectionSettingModel.Commit();
                DeconvolutionSettingModel.Commit();
            }
            if (Option.HasFlag(ProcessOption.Identification)) {
                Storage.DataBases = IdentifySettingModel.Create();
                AdductIonSettingModel.Commit();
            }
            if (Option.HasFlag(ProcessOption.Alignment)) {
                AlignmentParameterSettingModel.Commit();
                MobilitySettingModel?.Commit();
                IsotopeTrackSettingModel.Commit();
            }
            var method = settingModelFactory.BuildMethod();
            handler?.Invoke(this, method);
        }

        private static IMethodSettingModelFactory PrepareFactory(IMsdialDataStorage<ParameterBase> storage) {
            switch (storage) {
                case IMsdialDataStorage<MsdialLcImMsParameter> lcimmsStorage:
                    return new LcimmsMethodSettingModelFactory(lcimmsStorage);
                case IMsdialDataStorage<MsdialLcmsParameter> lcmsStorage:
                    return new LcmsMethodSettingModelFactory(lcmsStorage);
                case IMsdialDataStorage<MsdialImmsParameter> immsStorage:
                    return new ImmsMethodSettingModelFactory(immsStorage);
                case IMsdialDataStorage<MsdialDimsParameter> dimsStorage:
                    return new DimsMethodSettingModelFactory(dimsStorage);
                default:
                    throw new ArgumentException(nameof(storage));
            }
        }
    }
}
