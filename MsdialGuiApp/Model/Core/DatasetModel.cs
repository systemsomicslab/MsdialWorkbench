using CompMs.App.Msdial.Model.Dims;
using CompMs.App.Msdial.Model.Imms;
using CompMs.App.Msdial.Model.Lcimms;
using CompMs.App.Msdial.Model.Lcms;
using CompMs.App.Msdial.Model.Setting;
using CompMs.Common.Enum;
using CompMs.CommonMVVM;
using CompMs.MsdialCore.Algorithm;
using CompMs.MsdialCore.DataObj;
using CompMs.MsdialCore.Parameter;
using CompMs.MsdialDimsCore.DataObj;
using CompMs.MsdialDimsCore.Parameter;
using CompMs.MsdialImmsCore.DataObj;
using CompMs.MsdialImmsCore.Parameter;
using CompMs.MsdialLcImMsApi.DataObj;
using CompMs.MsdialLcImMsApi.Parameter;
using CompMs.MsdialLcmsApi.Parameter;
using CompMs.MsdialLcMsApi.DataObj;
using System;
using System.Reactive.Linq;

namespace CompMs.App.Msdial.Model.Core
{
    public class DatasetModel : BindableBase, IDatasetModel
    {
        public DatasetModel(IMsdialDataStorage<ParameterBase> storage) {
            Storage = storage;
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

        public virtual DataCollectionSettingModel DataCollectionSettingModel {
            get {
                if (dataCollectionSettingModel is null) {
                    switch (Storage) {
                        case IMsdialDataStorage<MsdialDimsParameter> storage:
                            dataCollectionSettingModel = new DataCollectionSettingModel(storage.Parameter);
                            break;
                        case IMsdialDataStorage<MsdialImmsParameter> storage:
                            dataCollectionSettingModel = new DataCollectionSettingModel(storage.Parameter);
                            break;
                        default:
                            dataCollectionSettingModel = new DataCollectionSettingModel(Storage.Parameter);
                            break;
                    }
                }
                return dataCollectionSettingModel;
            }
        }
        private DataCollectionSettingModel dataCollectionSettingModel;

        public PeakDetectionSettingModel PeakDetectionSettingModel {
            get {
                if (peakDetectionSettingModel is null) {
                    peakDetectionSettingModel = new PeakDetectionSettingModel(Storage.Parameter.PeakPickBaseParam);
                }
                return peakDetectionSettingModel;
            }
        }
        private PeakDetectionSettingModel peakDetectionSettingModel;

        public DeconvolutionSettingModel DeconvolutionSettingModel {
            get {
                if (deconvolutionSettingModel is null) {
                    deconvolutionSettingModel = new DeconvolutionSettingModel(Storage.Parameter.ChromDecBaseParam);
                }
                return deconvolutionSettingModel;
            }
        }
        private DeconvolutionSettingModel deconvolutionSettingModel;

        public IdentifySettingModel IdentifySettingModel {
            get {
                if (identifySettingModel is null || IdentifySettingModel.IsCompleted) {
                    switch (Storage) {
                        case IMsdialDataStorage<MsdialLcImMsParameter> storage:
                            identifySettingModel = new IdentifySettingModel(storage.Parameter, new LcimmsAnnotatorSettingFactory(), storage.DataBases);
                            break;
                        case IMsdialDataStorage<MsdialLcmsParameter> storage:
                            identifySettingModel = new IdentifySettingModel(storage.Parameter, new LcmsAnnotatorSettingFactory(), storage.DataBases);
                            break;
                        case IMsdialDataStorage<MsdialDimsParameter> storage:
                            identifySettingModel = new IdentifySettingModel(storage.Parameter, new DimsAnnotatorSettingModelFactory(), storage.DataBases);
                            break;
                        case IMsdialDataStorage<MsdialImmsParameter> storage:
                            throw new NotImplementedException("ImmsAnnotatorSettingModelFactory not implemented!"); // TODO: implement ImmsAnnotatorSettingModelFactory
                            identifySettingModel = new IdentifySettingModel(storage.Parameter, null, storage.DataBases);
                            break;
                    }
                    OnPropertyChanged(nameof(IdentifySettingModel));
                }
                return identifySettingModel;
            }
        }
        private IdentifySettingModel identifySettingModel;

        public AdductIonSettingModel AdductIonSettingModel {
            get {
                if (adductIonSettingModel is null) {
                    adductIonSettingModel = new AdductIonSettingModel(Storage.Parameter);
                }
                return adductIonSettingModel;
            }
        }
        private AdductIonSettingModel adductIonSettingModel;

        public AlignmentParameterSettingModel AlignmentParameterSettingModel {
            get {
                if (alignmentParameterSettingModel is null || alignmentParameterSettingModel.IsCompleted) {
                    alignmentParameterSettingModel = new AlignmentParameterSettingModel(Storage.Parameter, DateTime.UtcNow, Storage.AnalysisFiles);
                    OnPropertyChanged(nameof(AlignmentParameterSettingModel));
                }
                return alignmentParameterSettingModel;
            }
        }
        private AlignmentParameterSettingModel alignmentParameterSettingModel;

        public MobilitySettingModel MobilitySettingModel {
            get {
                if (mobilitySettingModel is null) {
                    switch (Storage) {
                        case IMsdialDataStorage<MsdialLcImMsParameter> storage:
                            mobilitySettingModel = new MobilitySettingModel(storage.Parameter, storage.AnalysisFiles);
                            break;
                        case IMsdialDataStorage<MsdialImmsParameter> storage:
                            mobilitySettingModel = new MobilitySettingModel(storage.Parameter, storage.AnalysisFiles);
                            break;
                    }
                }
                return mobilitySettingModel;
            }
        }
        private MobilitySettingModel mobilitySettingModel;

        public IsotopeTrackSettingModel IsotopeTrackSettingModel {
            get {
                if (isotopeTrackSettingModel is null) {
                    isotopeTrackSettingModel = new IsotopeTrackSettingModel(Storage.Parameter, Storage.AnalysisFiles);
                }
                return isotopeTrackSettingModel;
            }
        }
        private IsotopeTrackSettingModel isotopeTrackSettingModel;

        private void SetMethod() {
            switch (Storage) {
                case MsdialLcImMsDataStorage lcimmsStorage:
                    Method = new LcimmsMethodModel(lcimmsStorage);
                    return;
                case MsdialLcmsDataStorage lcmsStorage:
                    Method = new LcmsMethodModel(lcmsStorage, new StandardDataProviderFactory(retry: 5, isGuiProcess: true), Observable.Return<IBarItemsLoader>(null));
                    return;
                case MsdialImmsDataStorage immsStorage:
                    Method = new ImmsMethodModel(immsStorage);
                    return;
                case MsdialDimsDataStorage dimsStorage:
                    Method = new DimsMethodModel(dimsStorage, dimsStorage.AnalysisFiles, dimsStorage.AlignmentFiles);
                    return;
            }
            throw new NotImplementedException("This method is not implemented");
        }

        public void Run(ProcessOption option) {
            SetMethod();
            Method.Run(option);
        }
    }
}
