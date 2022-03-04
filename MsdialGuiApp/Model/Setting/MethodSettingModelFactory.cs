using CompMs.App.Msdial.Model.Core;
using CompMs.App.Msdial.Model.Dims;
using CompMs.App.Msdial.Model.Imms;
using CompMs.App.Msdial.Model.Lcimms;
using CompMs.App.Msdial.Model.Lcms;
using CompMs.MsdialCore.Algorithm;
using CompMs.MsdialCore.DataObj;
using CompMs.MsdialDimsCore.Parameter;
using CompMs.MsdialImmsCore.Parameter;
using CompMs.MsdialLcImMsApi.Parameter;
using CompMs.MsdialLcmsApi.Parameter;
using System;
using System.Reactive.Linq;

namespace CompMs.App.Msdial.Model.Setting
{
    internal interface IMethodSettingModelFactory
    {
        DataCollectionSettingModel CreateDataCollectionSetting();
        PeakDetectionSettingModel CreatePeakDetectionSetting();
        DeconvolutionSettingModel CreateDeconvolutionSetting();
        IdentifySettingModel CreateIdentifySetting();
        AdductIonSettingModel CreateAdductIonSetting();
        AlignmentParameterSettingModel CreateAlignmentParameterSetting();
        MobilitySettingModel CreateMobilitySetting();
        IsotopeTrackSettingModel CreateIsotopeTrackSetting();
        MethodModelBase BuildMethod();
    }

    class DimsMethodSettingModelFactory : IMethodSettingModelFactory
    {
        private readonly IMsdialDataStorage<MsdialDimsParameter> storage;

        public DimsMethodSettingModelFactory(IMsdialDataStorage<MsdialDimsParameter> storage) {
            this.storage = storage;
        }

        public AdductIonSettingModel CreateAdductIonSetting() {
            return new AdductIonSettingModel(storage.Parameter);
        }

        public AlignmentParameterSettingModel CreateAlignmentParameterSetting() {
            return new AlignmentParameterSettingModel(storage.Parameter, DateTime.Now, storage.AnalysisFiles);
        }

        public DataCollectionSettingModel CreateDataCollectionSetting() {
            return new DataCollectionSettingModel(storage.Parameter);
        }

        public DeconvolutionSettingModel CreateDeconvolutionSetting() {
            return new DeconvolutionSettingModel(storage.Parameter.ChromDecBaseParam);
        }

        public IdentifySettingModel CreateIdentifySetting() {
            return new IdentifySettingModel(storage.Parameter, new DimsAnnotatorSettingModelFactory(), storage.DataBases);
        }

        public IsotopeTrackSettingModel CreateIsotopeTrackSetting() {
            return new IsotopeTrackSettingModel(storage.Parameter, storage.AnalysisFiles);
        }

        public MobilitySettingModel CreateMobilitySetting() {
            return null;
        }

        public PeakDetectionSettingModel CreatePeakDetectionSetting() {
            return new PeakDetectionSettingModel(storage.Parameter.PeakPickBaseParam);
        }

        public MethodModelBase BuildMethod() {
            return new DimsMethodModel(storage, storage.AnalysisFiles, storage.AlignmentFiles);
        }
    }

    class LcmsMethodSettingModelFactory : IMethodSettingModelFactory
    {
        private readonly IMsdialDataStorage<MsdialLcmsParameter> storage;

        public LcmsMethodSettingModelFactory(IMsdialDataStorage<MsdialLcmsParameter> storage) {
            this.storage = storage;
        }

        public AdductIonSettingModel CreateAdductIonSetting() {
            return new AdductIonSettingModel(storage.Parameter);
        }

        public AlignmentParameterSettingModel CreateAlignmentParameterSetting() {
            return new AlignmentParameterSettingModel(storage.Parameter, DateTime.Now, storage.AnalysisFiles);
        }

        public DataCollectionSettingModel CreateDataCollectionSetting() {
            return new DataCollectionSettingModel(storage.Parameter);
        }

        public DeconvolutionSettingModel CreateDeconvolutionSetting() {
            return new DeconvolutionSettingModel(storage.Parameter.ChromDecBaseParam);
        }

        public IdentifySettingModel CreateIdentifySetting() {
            return new IdentifySettingModel(storage.Parameter, new LcmsAnnotatorSettingFactory(), storage.DataBases);
        }

        public IsotopeTrackSettingModel CreateIsotopeTrackSetting() {
            return new IsotopeTrackSettingModel(storage.Parameter, storage.AnalysisFiles);
        }

        public MobilitySettingModel CreateMobilitySetting() {
            return null;
        }

        public PeakDetectionSettingModel CreatePeakDetectionSetting() {
            return new PeakDetectionSettingModel(storage.Parameter.PeakPickBaseParam);
        }

        public MethodModelBase BuildMethod() {
            return new LcmsMethodModel(storage, new StandardDataProviderFactory(retry: 5, isGuiProcess: true), Observable.Return<IBarItemsLoader>(null));
        }
    }

    class ImmsMethodSettingModelFactory : IMethodSettingModelFactory
    {
        private readonly IMsdialDataStorage<MsdialImmsParameter> storage;

        public ImmsMethodSettingModelFactory(IMsdialDataStorage<MsdialImmsParameter> storage) {
            this.storage = storage;
        }

        public AdductIonSettingModel CreateAdductIonSetting() {
            return new AdductIonSettingModel(storage.Parameter);
        }

        public AlignmentParameterSettingModel CreateAlignmentParameterSetting() {
            return new AlignmentParameterSettingModel(storage.Parameter, DateTime.Now, storage.AnalysisFiles);
        }

        public DataCollectionSettingModel CreateDataCollectionSetting() {
            return new DataCollectionSettingModel(storage.Parameter);
        }

        public DeconvolutionSettingModel CreateDeconvolutionSetting() {
            return new DeconvolutionSettingModel(storage.Parameter.ChromDecBaseParam);
        }

        public IdentifySettingModel CreateIdentifySetting() {
            throw new NotImplementedException("ImmsAnnotatorSettingModelFactory is not implemented!");
            return new IdentifySettingModel(storage.Parameter, null, storage.DataBases);
        }

        public IsotopeTrackSettingModel CreateIsotopeTrackSetting() {
            return new IsotopeTrackSettingModel(storage.Parameter, storage.AnalysisFiles);
        }

        public MobilitySettingModel CreateMobilitySetting() {
            return new MobilitySettingModel(storage.Parameter, storage.AnalysisFiles);
        }

        public PeakDetectionSettingModel CreatePeakDetectionSetting() {
            return new PeakDetectionSettingModel(storage.Parameter.PeakPickBaseParam);
        }

        public MethodModelBase BuildMethod() {
            return new ImmsMethodModel(storage);
        }
    }

    class LcimmsMethodSettingModelFactory : IMethodSettingModelFactory
    {
        private readonly IMsdialDataStorage<MsdialLcImMsParameter> storage;

        public LcimmsMethodSettingModelFactory(IMsdialDataStorage<MsdialLcImMsParameter> storage) {
            this.storage = storage;
        }

        public AdductIonSettingModel CreateAdductIonSetting() {
            return new AdductIonSettingModel(storage.Parameter);
        }

        public AlignmentParameterSettingModel CreateAlignmentParameterSetting() {
            return new AlignmentParameterSettingModel(storage.Parameter, DateTime.Now, storage.AnalysisFiles);
        }

        public DataCollectionSettingModel CreateDataCollectionSetting() {
            return new DataCollectionSettingModel(storage.Parameter);
        }

        public DeconvolutionSettingModel CreateDeconvolutionSetting() {
            return new DeconvolutionSettingModel(storage.Parameter.ChromDecBaseParam);
        }

        public IdentifySettingModel CreateIdentifySetting() {
            return new IdentifySettingModel(storage.Parameter, new LcimmsAnnotatorSettingFactory(), storage.DataBases);
        }

        public IsotopeTrackSettingModel CreateIsotopeTrackSetting() {
            return new IsotopeTrackSettingModel(storage.Parameter, storage.AnalysisFiles);
        }

        public MobilitySettingModel CreateMobilitySetting() {
            return new MobilitySettingModel(storage.Parameter, storage.AnalysisFiles);
        }

        public PeakDetectionSettingModel CreatePeakDetectionSetting() {
            return new PeakDetectionSettingModel(storage.Parameter.PeakPickBaseParam);
        }

        public MethodModelBase BuildMethod() {
            return new LcimmsMethodModel(storage);
        }
    }
}
