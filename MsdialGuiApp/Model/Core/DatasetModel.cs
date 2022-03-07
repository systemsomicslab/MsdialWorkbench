using CompMs.App.Msdial.Model.Setting;
using CompMs.Common.Enum;
using CompMs.CommonMVVM;
using CompMs.MsdialCore.DataObj;
using CompMs.MsdialCore.Parameter;
using System;

namespace CompMs.App.Msdial.Model.Core
{
    public class DatasetModel : BindableBase, IDatasetModel
    {
        public DatasetModel(IMsdialDataStorage<ParameterBase> storage) {
            Storage = storage ?? throw new ArgumentNullException(nameof(storage));
            AllProcessMethodSettingModel = new MethodSettingModel(ProcessOption.All, Storage, Handler);
            IdentificationProcessMethodSettingModel = new MethodSettingModel(ProcessOption.IdentificationPlusAlignment, Storage, Handler);
            AlignmentProcessMethodSettingModel = new MethodSettingModel(ProcessOption.Alignment, Storage, Handler);
        }

        public MethodModelBase Method {
            get => method;
            private set {
                var prev = method;
                if (SetProperty(ref method, value)) {
                    prev?.Dispose();
                }
            }
        }
        private MethodModelBase method;

        public IMsdialDataStorage<ParameterBase> Storage { get; }

        public MethodSettingModel AllProcessMethodSettingModel {
            get => allProcessMethodSettingModel;
            private set => SetProperty(ref allProcessMethodSettingModel, value);
        }
        private MethodSettingModel allProcessMethodSettingModel;

        public MethodSettingModel IdentificationProcessMethodSettingModel {
            get => identificationProcessMethodSettingModel;
            private set => SetProperty(ref identificationProcessMethodSettingModel, value);
        }
        public MethodSettingModel identificationProcessMethodSettingModel;

        public MethodSettingModel AlignmentProcessMethodSettingModel {
            get => alignmentProcessMethodSettingModel;
            private set => SetProperty(ref alignmentProcessMethodSettingModel, value);
        }
        public MethodSettingModel alignmentProcessMethodSettingModel;

        private void Handler(MethodSettingModel setting, MethodModelBase model) {
            Method = model;
            if (AllProcessMethodSettingModel == setting) {
                AllProcessMethodSettingModel = new MethodSettingModel(ProcessOption.All, Storage, Handler);
            }
            if (IdentificationProcessMethodSettingModel == setting) {
                IdentificationProcessMethodSettingModel = new MethodSettingModel(ProcessOption.IdentificationPlusAlignment, Storage, Handler);
            }
            if (AlignmentProcessMethodSettingModel == setting) {
                AlignmentProcessMethodSettingModel = new MethodSettingModel(ProcessOption.Alignment, Storage, Handler);
            }
            Method.Run(setting.Option);
        }
    }
}
