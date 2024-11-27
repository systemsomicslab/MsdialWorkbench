using CompMs.App.Msdial.Common;
using CompMs.App.Msdial.Model.Core;
using CompMs.App.Msdial.Model.DataObj;
using CompMs.App.Msdial.Model.Dims;
using CompMs.App.Msdial.Model.Gcms;
using CompMs.App.Msdial.Model.ImagingImms;
using CompMs.App.Msdial.Model.Imms;
using CompMs.App.Msdial.Model.Lcimms;
using CompMs.App.Msdial.Model.Lcms;
using CompMs.Common.DataObj.Result;
using CompMs.Common.Enum;
using CompMs.MsdialCore.Algorithm;
using CompMs.MsdialCore.DataObj;
using CompMs.MsdialCore.Parameter;
using CompMs.MsdialDimsCore.Parameter;
using CompMs.MsdialGcMsApi.Parameter;
using CompMs.MsdialImmsCore.Parameter;
using CompMs.MsdialLcImMsApi.Parameter;
using CompMs.MsdialLcmsApi.Parameter;
using Reactive.Bindings.Notifiers;
using System;
using System.IO;
using System.Linq;
using System.Reactive;
using System.Reactive.Linq;
using System.Reflection;

namespace CompMs.App.Msdial.Model.Setting
{
    internal interface IMethodSettingModelFactory
    {
        IDataCollectionSettingModel? CreateDataCollectionSetting();
        IPeakDetectionSettingModel? CreatePeakDetectionSetting();
        DeconvolutionSettingModel? CreateDeconvolutionSetting();
        IIdentificationSettingModel? CreateIdentifySetting();
        AdductIonSettingModel? CreateAdductIonSetting();
        IAlignmentParameterSettingModel? CreateAlignmentParameterSetting();
        MobilitySettingModel? CreateMobilitySetting();
        IsotopeTrackSettingModel? CreateIsotopeTrackSetting();
        IMethodModel BuildMethod();
    }

    internal sealed class MethodSettingModelFactory : IMethodSettingModelFactory
    {
        public MethodSettingModelFactory(AnalysisFileBeanModelCollection analysisFileBeanModelCollection, AlignmentFileBeanModelCollection alignmentFileModelCollection, IMsdialDataStorage<ParameterBase> storage, FilePropertiesModel fileProperties, StudyContextModel studyContext, ProcessOption process, IMessageBroker messageBroker) {
            switch (storage) {
                case IMsdialDataStorage<MsdialLcImMsParameter> lcimmsStorage:
                    factoryImpl = new LcimmsMethodSettingModelFactory(analysisFileBeanModelCollection, alignmentFileModelCollection, lcimmsStorage, fileProperties, studyContext, process, messageBroker);
                    break;
                case IMsdialDataStorage<MsdialLcmsParameter> lcmsStorage:
                    factoryImpl = new LcmsMethodSettingModelFactory(analysisFileBeanModelCollection, alignmentFileModelCollection, lcmsStorage, fileProperties, studyContext, process, messageBroker);
                    break;
                case IMsdialDataStorage<MsdialImmsParameter> immsStorage:
                    if (immsStorage.Parameter.MachineCategory == MachineCategory.IIMMS) {
                        factoryImpl = new ImagingImmsMethodSettingModelFactory(analysisFileBeanModelCollection, alignmentFileModelCollection, immsStorage, fileProperties, studyContext, process, messageBroker);
                    }
                    else {
                        factoryImpl = new ImmsMethodSettingModelFactory(analysisFileBeanModelCollection, alignmentFileModelCollection, immsStorage, fileProperties, studyContext, process, messageBroker);
                    }
                    break;
                case IMsdialDataStorage<MsdialDimsParameter> dimsStorage:
                    factoryImpl = new DimsMethodSettingModelFactory(analysisFileBeanModelCollection, alignmentFileModelCollection, dimsStorage, fileProperties, studyContext, process, messageBroker);
                    break;
                case IMsdialDataStorage<MsdialGcmsParameter> gcmsStorage:
                    factoryImpl = new GcmsMethodSettingModelFactory(analysisFileBeanModelCollection, alignmentFileModelCollection, gcmsStorage, fileProperties, studyContext, process, messageBroker);
                    break;
                default:
                    throw new ArgumentException(nameof(storage));
            }
        }

        private readonly IMethodSettingModelFactory factoryImpl;

        public IMethodModel BuildMethod() => factoryImpl.BuildMethod();
        public AdductIonSettingModel? CreateAdductIonSetting() => factoryImpl.CreateAdductIonSetting();
        public IAlignmentParameterSettingModel? CreateAlignmentParameterSetting() => factoryImpl.CreateAlignmentParameterSetting();
        public IDataCollectionSettingModel? CreateDataCollectionSetting() => factoryImpl.CreateDataCollectionSetting();
        public DeconvolutionSettingModel? CreateDeconvolutionSetting() => factoryImpl.CreateDeconvolutionSetting();
        public IIdentificationSettingModel? CreateIdentifySetting() => factoryImpl.CreateIdentifySetting();
        public IsotopeTrackSettingModel? CreateIsotopeTrackSetting() => factoryImpl.CreateIsotopeTrackSetting();
        public MobilitySettingModel? CreateMobilitySetting() => factoryImpl.CreateMobilitySetting();
        public IPeakDetectionSettingModel? CreatePeakDetectionSetting() => factoryImpl.CreatePeakDetectionSetting();
    }


    sealed class DimsMethodSettingModelFactory : IMethodSettingModelFactory
    {
        private readonly AnalysisFileBeanModelCollection _analysisFileBeanModelCollection;
        private readonly AlignmentFileBeanModelCollection _alignmentFileBeanModelCollection;
        private readonly IMsdialDataStorage<MsdialDimsParameter> storage;
        private readonly FilePropertiesModel _projectBaseParameter;
        private readonly StudyContextModel _studyContext;
        private readonly ProcessOption process;
        private readonly IMessageBroker _messageBroker;
        private readonly PeakPickBaseParameterModel _peakPickBaseParameter;

        public DimsMethodSettingModelFactory(AnalysisFileBeanModelCollection analysisFileBeanModelCollection, AlignmentFileBeanModelCollection alignmentFileBeanModelCollection, IMsdialDataStorage<MsdialDimsParameter> storage, FilePropertiesModel projectBaseParameter, StudyContextModel studyContext, ProcessOption process, IMessageBroker messageBroker) {
            _analysisFileBeanModelCollection = analysisFileBeanModelCollection;
            _alignmentFileBeanModelCollection = alignmentFileBeanModelCollection;
            this.storage = storage;
            _projectBaseParameter = projectBaseParameter ?? throw new ArgumentNullException(nameof(projectBaseParameter));
            _studyContext = studyContext;
            this.process = process;
            _messageBroker = messageBroker;
            _peakPickBaseParameter = new PeakPickBaseParameterModel(storage.Parameter.PeakPickBaseParam);
        }

        public AdductIonSettingModel CreateAdductIonSetting() {
            return new AdductIonSettingModel(storage.Parameter, process);
        }

        public IAlignmentParameterSettingModel CreateAlignmentParameterSetting() {
            return new AlignmentParameterSettingModel(storage.Parameter, DateTime.Now, storage.AnalysisFiles, _alignmentFileBeanModelCollection, process);
        }

        public IDataCollectionSettingModel CreateDataCollectionSetting() {
            return new DataCollectionSettingModel(storage.Parameter, _peakPickBaseParameter, storage.AnalysisFiles, process);
        }

        public DeconvolutionSettingModel CreateDeconvolutionSetting() {
            return new DeconvolutionSettingModel(storage.Parameter.ChromDecBaseParam, process);
        }

        public IIdentificationSettingModel CreateIdentifySetting() {
            var parameter = storage.Parameter;
            var model = new IdentifySettingModel(parameter, new DimsAnnotatorSettingModelFactory(parameter), process, _messageBroker, storage.DataBases);

            if (parameter.TargetOmics == TargetOmics.Lipidomics) {
                if (model.DataBaseModels.Count == 0) {
                    if (parameter.CollistionType == CollisionType.EIEIO
                        && model.DataBaseModels.All(m => m.DBSource != DataBaseSource.EieioLipid)) {
                        var databaseModel = model.AddDataBase();
                        databaseModel.DBSource = DataBaseSource.EieioLipid;
                    }

                    if (parameter.CollistionType == CollisionType.OAD
                        && model.DataBaseModels.All(m => m.DBSource != DataBaseSource.OadLipid)) {
                        var databaseModel = model.AddDataBase();
                        databaseModel.DBSource = DataBaseSource.OadLipid;
                    }

                    if (parameter.CollistionType == CollisionType.EID
                        && model.DataBaseModels.All(m => m.DBSource != DataBaseSource.EidLipid)) {
                        var databaseModel = model.AddDataBase();
                        databaseModel.DBSource = DataBaseSource.EidLipid;
                    }

                    string mainDirectory = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
                    var lbmFiles = Directory.GetFiles(mainDirectory, "*." + SaveFileFormat.lbm + "?", SearchOption.TopDirectoryOnly);
                    var lbmFile = lbmFiles.FirstOrDefault();
                    if (!(lbmFile is null)
                        && model.DataBaseModels.All(m => m.DBSource != DataBaseSource.Msp)) {
                        var databaseModel = model.AddDataBase();
                        databaseModel.DataBasePath = lbmFile;
                    }
                }
            }
            else if (parameter.TargetOmics == TargetOmics.Proteomics) {
                parameter.MaxChargeNumber = 6;
                parameter.MinimumAmplitude = 100000;
                parameter.AmplitudeCutoff = 1000;
            }
            return model;
        }

        public IsotopeTrackSettingModel CreateIsotopeTrackSetting() {
            return new IsotopeTrackSettingModel(storage.Parameter, storage.AnalysisFiles, process);
        }

        public MobilitySettingModel? CreateMobilitySetting() {
            return null;
        }

        public IPeakDetectionSettingModel CreatePeakDetectionSetting() {
            return new PeakDetectionSettingModel(_peakPickBaseParameter, process);
        }

        public IMethodModel BuildMethod() {
            return new DimsMethodModel(storage, _analysisFileBeanModelCollection, _alignmentFileBeanModelCollection, _projectBaseParameter, _studyContext, _messageBroker);
        }
    }

    sealed class LcmsMethodSettingModelFactory : IMethodSettingModelFactory
    {
        private readonly AnalysisFileBeanModelCollection _analysisFileBeanModelCollection;
        private readonly AlignmentFileBeanModelCollection _alignmentFileBeanModelCollection;
        private readonly IMsdialDataStorage<MsdialLcmsParameter> storage;
        private readonly FilePropertiesModel _fileProperties;
        private readonly StudyContextModel _studyContext;
        private readonly ProcessOption process;
        private readonly IMessageBroker _broker;
        private readonly PeakPickBaseParameterModel _peakPickBaseParameter;

        public LcmsMethodSettingModelFactory(AnalysisFileBeanModelCollection analysisFileBeanModelCollection, AlignmentFileBeanModelCollection alignmentFileBeanModelCollection, IMsdialDataStorage<MsdialLcmsParameter> storage, FilePropertiesModel fileProperties, StudyContextModel studyContext, ProcessOption process, IMessageBroker broker) {
            _analysisFileBeanModelCollection = analysisFileBeanModelCollection;
            _alignmentFileBeanModelCollection = alignmentFileBeanModelCollection;
            this.storage = storage;
            _fileProperties = fileProperties ?? throw new ArgumentNullException(nameof(fileProperties));
            _studyContext = studyContext;
            this.process = process;
            _broker = broker;
            if (this.storage.Parameter.TargetOmics == TargetOmics.Proteomics) {
                this.storage.Parameter.MaxChargeNumber = 8;
                this.storage.Parameter.RemoveAfterPrecursor = false;
            }
            _peakPickBaseParameter = new PeakPickBaseParameterModel(storage.Parameter.PeakPickBaseParam);
        }

        public AdductIonSettingModel CreateAdductIonSetting() {
            return new AdductIonSettingModel(storage.Parameter, process);
        }

        public IAlignmentParameterSettingModel CreateAlignmentParameterSetting() {
            return new AlignmentParameterSettingModel(storage.Parameter, DateTime.Now, storage.AnalysisFiles, _alignmentFileBeanModelCollection, process);
        }

        public IDataCollectionSettingModel CreateDataCollectionSetting() {
            return new DataCollectionSettingModel(storage.Parameter, _peakPickBaseParameter, storage.AnalysisFiles, process);
        }

        public DeconvolutionSettingModel CreateDeconvolutionSetting() {
            return new DeconvolutionSettingModel(storage.Parameter.ChromDecBaseParam, process);
        }

        public IIdentificationSettingModel CreateIdentifySetting() {
            var parameter = storage.Parameter;
            var model = new IdentifySettingModel(storage.Parameter, new LcmsAnnotatorSettingFactory(parameter), process, _broker, storage.DataBases);

            if (parameter.TargetOmics == TargetOmics.Lipidomics) {
                if (model.DataBaseModels.Count == 0) {
                    if (parameter.CollistionType == CollisionType.EIEIO && model.DataBaseModels.All(m => m.DBSource != DataBaseSource.EieioLipid)) {
                        var databaseModel = model.AddDataBase();
                        databaseModel.DBSource = DataBaseSource.EieioLipid;
                    }

                    if (parameter.CollistionType == CollisionType.OAD && model.DataBaseModels.All(m => m.DBSource != DataBaseSource.OadLipid)) {
                        var databaseModel = model.AddDataBase();
                        databaseModel.DBSource = DataBaseSource.OadLipid;
                    }

                    if (parameter.CollistionType == CollisionType.EID && model.DataBaseModels.All(m => m.DBSource != DataBaseSource.EidLipid)) {
                        var databaseModel = model.AddDataBase();
                        databaseModel.DBSource = DataBaseSource.EidLipid;
                    }

                    {
                        var databaseModel = model.AddDataBase();
                        databaseModel.TrySetLbmLibrary();
                    }
                }
            }
            
            return model;
        }

        public IsotopeTrackSettingModel CreateIsotopeTrackSetting() {
            return new IsotopeTrackSettingModel(storage.Parameter, storage.AnalysisFiles, process);
        }

        public MobilitySettingModel? CreateMobilitySetting() {
            return null;
        }

        public IPeakDetectionSettingModel CreatePeakDetectionSetting() {
            return new PeakDetectionSettingModel(_peakPickBaseParameter, process);
        }

        public IMethodModel BuildMethod() {
            return new LcmsMethodModel(_analysisFileBeanModelCollection, _alignmentFileBeanModelCollection, storage, new StandardDataProviderFactory(retry: 5, isGuiProcess: true), _fileProperties, _studyContext, _broker);
        }
    }

    sealed class ImmsMethodSettingModelFactory : IMethodSettingModelFactory
    {
        private readonly AnalysisFileBeanModelCollection _analysisFileBeanModelCollection;
        private readonly AlignmentFileBeanModelCollection _alignmentFileBeanModelCollection;
        private readonly IMsdialDataStorage<MsdialImmsParameter> storage;
        private readonly FilePropertiesModel _fileProperties;
        private readonly StudyContextModel _studyContext;
        private readonly ProcessOption process;
        private readonly IMessageBroker _broker;
        private readonly PeakPickBaseParameterModel _peakPickBaseParameter;

        public ImmsMethodSettingModelFactory(AnalysisFileBeanModelCollection analysisFileBeanModelCollection, AlignmentFileBeanModelCollection alignmentFileBeanModelCollection, IMsdialDataStorage<MsdialImmsParameter> storage, FilePropertiesModel fileProperties, StudyContextModel studyContext, ProcessOption process, IMessageBroker broker) {
            _analysisFileBeanModelCollection = analysisFileBeanModelCollection;
            _alignmentFileBeanModelCollection = alignmentFileBeanModelCollection;
            this.storage = storage;
            _fileProperties = fileProperties ?? throw new ArgumentNullException(nameof(fileProperties));
            _studyContext = studyContext;
            this.process = process;
            _broker = broker;
            _peakPickBaseParameter = new PeakPickBaseParameterModel(storage.Parameter.PeakPickBaseParam);
        }

        public AdductIonSettingModel CreateAdductIonSetting() {
            return new AdductIonSettingModel(storage.Parameter, process);
        }

        public IAlignmentParameterSettingModel CreateAlignmentParameterSetting() {
            return new AlignmentParameterSettingModel(storage.Parameter, DateTime.Now, storage.AnalysisFiles, _alignmentFileBeanModelCollection, process);
        }

        public IDataCollectionSettingModel CreateDataCollectionSetting() {
            return new DataCollectionSettingModel(storage.Parameter, _peakPickBaseParameter, storage.AnalysisFiles, process);
        }

        public DeconvolutionSettingModel CreateDeconvolutionSetting() {
            return new DeconvolutionSettingModel(storage.Parameter.ChromDecBaseParam, process);
        }

        public IIdentificationSettingModel CreateIdentifySetting() {
            var parameter = storage.Parameter;
            var model = new IdentifySettingModel(storage.Parameter, new ImmsAnnotatorSettingModelFactory(parameter), process, _broker, storage.DataBases);

            if (parameter.TargetOmics == TargetOmics.Lipidomics) {
                if (model.DataBaseModels.Count == 0) {
                    if (parameter.CollistionType == CollisionType.EIEIO
                        && model.DataBaseModels.All(m => m.DBSource != DataBaseSource.EieioLipid)) {
                        var databaseModel = model.AddDataBase();
                        databaseModel.DBSource = DataBaseSource.EieioLipid;
                    }

                    if (parameter.CollistionType == CollisionType.OAD
                        && model.DataBaseModels.All(m => m.DBSource != DataBaseSource.OadLipid)) {
                        var databaseModel = model.AddDataBase();
                        databaseModel.DBSource = DataBaseSource.OadLipid;
                    }

                    if (parameter.CollistionType == CollisionType.EID
                        && model.DataBaseModels.All(m => m.DBSource != DataBaseSource.EidLipid)) {
                        var databaseModel = model.AddDataBase();
                        databaseModel.DBSource = DataBaseSource.EidLipid;
                    }

                    string mainDirectory = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
                    var lbmFiles = Directory.GetFiles(mainDirectory, "*." + SaveFileFormat.lbm + "?", SearchOption.TopDirectoryOnly);
                    var lbmFile = lbmFiles.FirstOrDefault();
                    if (!(lbmFile is null)
                        && model.DataBaseModels.All(m => m.DBSource != DataBaseSource.Msp)) {
                        var databaseModel = model.AddDataBase();
                        databaseModel.DataBasePath = lbmFile;
                    }
                }
            }
            return model;
        }

        public IsotopeTrackSettingModel CreateIsotopeTrackSetting() {
            return new IsotopeTrackSettingModel(storage.Parameter, storage.AnalysisFiles, process);
        }

        public MobilitySettingModel CreateMobilitySetting() {
            return new MobilitySettingModel(storage.Parameter, storage.AnalysisFiles, process);
        }

        public IPeakDetectionSettingModel CreatePeakDetectionSetting() {
            return new PeakDetectionSettingModel(_peakPickBaseParameter, process);
        }

        public IMethodModel BuildMethod() {
            return new ImmsMethodModel(_analysisFileBeanModelCollection, _alignmentFileBeanModelCollection, storage, _fileProperties, _studyContext, _broker);
        }
    }

    sealed class LcimmsMethodSettingModelFactory : IMethodSettingModelFactory
    {
        private readonly AnalysisFileBeanModelCollection _analysisFileBeanModelCollection;
        private readonly AlignmentFileBeanModelCollection _alignmentFileBeanModelCollection;
        private readonly IMsdialDataStorage<MsdialLcImMsParameter> storage;
        private readonly FilePropertiesModel _projectBaseParameter;
        private readonly StudyContextModel _studyContext;
        private readonly ProcessOption process;
        private readonly IMessageBroker _broker;
        private readonly PeakPickBaseParameterModel _peakPickBaseParameter;

        public LcimmsMethodSettingModelFactory(AnalysisFileBeanModelCollection analysisFileBeanModelCollection, AlignmentFileBeanModelCollection alignmentFileBeanModelCollection, IMsdialDataStorage<MsdialLcImMsParameter> storage, FilePropertiesModel projectBaseParameter, StudyContextModel studyContext, ProcessOption process, IMessageBroker broker) {
            _analysisFileBeanModelCollection = analysisFileBeanModelCollection;
            _alignmentFileBeanModelCollection = alignmentFileBeanModelCollection;
            this.storage = storage;
            _projectBaseParameter = projectBaseParameter ?? throw new ArgumentNullException(nameof(projectBaseParameter));
            _studyContext = studyContext;
            this.process = process;
            _broker = broker;
            _peakPickBaseParameter = new PeakPickBaseParameterModel(storage.Parameter.PeakPickBaseParam);
        }

        public AdductIonSettingModel CreateAdductIonSetting() {
            return new AdductIonSettingModel(storage.Parameter, process);
        }

        public IAlignmentParameterSettingModel CreateAlignmentParameterSetting() {
            return new AlignmentParameterSettingModel(storage.Parameter, DateTime.Now, storage.AnalysisFiles, _alignmentFileBeanModelCollection, process);
        }

        public IDataCollectionSettingModel CreateDataCollectionSetting() {
            return new DataCollectionSettingModel(storage.Parameter, _peakPickBaseParameter, storage.AnalysisFiles, process);
        }

        public DeconvolutionSettingModel CreateDeconvolutionSetting() {
            return new DeconvolutionSettingModel(storage.Parameter.ChromDecBaseParam, process);
        }

        public IIdentificationSettingModel CreateIdentifySetting() {
            var parameter = storage.Parameter;
            var model = new IdentifySettingModel(storage.Parameter, new LcimmsAnnotatorSettingFactory(parameter), process, _broker, storage.DataBases);

            if (parameter.TargetOmics == TargetOmics.Lipidomics) {
                if (model.DataBaseModels.Count == 0) {
                    if (parameter.CollistionType == CollisionType.EIEIO
                        && model.DataBaseModels.All(m => m.DBSource != DataBaseSource.EieioLipid)) {
                        var databaseModel = model.AddDataBase();
                        databaseModel.DBSource = DataBaseSource.EieioLipid;
                    }

                    if (parameter.CollistionType == CollisionType.OAD
                        && model.DataBaseModels.All(m => m.DBSource != DataBaseSource.OadLipid)) {
                        var databaseModel = model.AddDataBase();
                        databaseModel.DBSource = DataBaseSource.OadLipid;
                    }

                    if (parameter.CollistionType == CollisionType.EID
                        && model.DataBaseModels.All(m => m.DBSource != DataBaseSource.EidLipid)) {
                        var databaseModel = model.AddDataBase();
                        databaseModel.DBSource = DataBaseSource.EidLipid;
                    }

                    string mainDirectory = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
                    var lbmFiles = Directory.GetFiles(mainDirectory, "*." + SaveFileFormat.lbm + "?", SearchOption.TopDirectoryOnly);
                    var lbmFile = lbmFiles.FirstOrDefault();
                    if (!(lbmFile is null)
                        && model.DataBaseModels.All(m => m.DBSource != DataBaseSource.Msp)) {
                        var databaseModel = model.AddDataBase();
                        databaseModel.DataBasePath = lbmFile;
                    }
                }
            }
            return model;
        }

        public IsotopeTrackSettingModel CreateIsotopeTrackSetting() {
            return new IsotopeTrackSettingModel(storage.Parameter, storage.AnalysisFiles, process);
        }

        public MobilitySettingModel CreateMobilitySetting() {
            return new MobilitySettingModel(storage.Parameter, storage.AnalysisFiles, process);
        }

        public IPeakDetectionSettingModel CreatePeakDetectionSetting() {
            return new PeakDetectionSettingModel(_peakPickBaseParameter, process);
        }

        public IMethodModel BuildMethod() {
            return new LcimmsMethodModel(_analysisFileBeanModelCollection, _alignmentFileBeanModelCollection, storage, _projectBaseParameter, _studyContext, _broker);
        }
    }

    sealed class ImagingImmsMethodSettingModelFactory : IMethodSettingModelFactory
    {
        private readonly AnalysisFileBeanModelCollection _analysisFileBeanModelCollection;
        private readonly AlignmentFileBeanModelCollection _alignmentFileBeanModelCollection;
        private readonly IMsdialDataStorage<MsdialImmsParameter> storage;
        private readonly FilePropertiesModel _fileProperties;
        private readonly StudyContextModel _studyContext;
        private readonly ProcessOption process;
        private readonly IMessageBroker _broker;
        private readonly PeakPickBaseParameterModel _peakPickBaseParameter;

        public ImagingImmsMethodSettingModelFactory(AnalysisFileBeanModelCollection analysisFileBeanModelCollection, AlignmentFileBeanModelCollection alignmentFileBeanModelCollection, IMsdialDataStorage<MsdialImmsParameter> storage, FilePropertiesModel fileProperties, StudyContextModel studyContext, ProcessOption process, IMessageBroker broker) {
            _analysisFileBeanModelCollection = analysisFileBeanModelCollection;
            _alignmentFileBeanModelCollection = alignmentFileBeanModelCollection;
            this.storage = storage;
            _fileProperties = fileProperties ?? throw new ArgumentNullException(nameof(fileProperties));
            _studyContext = studyContext;
            this.process = process;
            _broker = broker;
            _peakPickBaseParameter = new PeakPickBaseParameterModel(storage.Parameter.PeakPickBaseParam);
        }

        public AdductIonSettingModel CreateAdductIonSetting() {
            return new AdductIonSettingModel(storage.Parameter, process);
        }

        public IAlignmentParameterSettingModel CreateAlignmentParameterSetting() {
            return new AlignmentParameterSettingModel(storage.Parameter, DateTime.Now, storage.AnalysisFiles, _alignmentFileBeanModelCollection, process);
        }

        public IDataCollectionSettingModel CreateDataCollectionSetting() {
            return new DataCollectionSettingModel(storage.Parameter, _peakPickBaseParameter, storage.AnalysisFiles, process);
        }

        public DeconvolutionSettingModel CreateDeconvolutionSetting() {
            return new DeconvolutionSettingModel(storage.Parameter.ChromDecBaseParam, process);
        }

        public IIdentificationSettingModel CreateIdentifySetting() {
            var parameter = storage.Parameter;
            var model = new IdentifySettingModel(storage.Parameter, new ImmsAnnotatorSettingModelFactory(parameter), process, _broker, storage.DataBases);

            if (parameter.TargetOmics == TargetOmics.Lipidomics) {
                if (model.DataBaseModels.Count == 0) {
                    if (parameter.CollistionType == CollisionType.EIEIO
                        && model.DataBaseModels.All(m => m.DBSource != DataBaseSource.EieioLipid)) {
                        var databaseModel = model.AddDataBase();
                        databaseModel.DBSource = DataBaseSource.EieioLipid;
                    }

                    if (parameter.CollistionType == CollisionType.OAD
                        && model.DataBaseModels.All(m => m.DBSource != DataBaseSource.OadLipid)) {
                        var databaseModel = model.AddDataBase();
                        databaseModel.DBSource = DataBaseSource.OadLipid;
                    }

                    if (parameter.CollistionType == CollisionType.EID
                       && model.DataBaseModels.All(m => m.DBSource != DataBaseSource.EidLipid)) {
                        var databaseModel = model.AddDataBase();
                        databaseModel.DBSource = DataBaseSource.EidLipid;
                    }

                    string mainDirectory = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
                    var lbmFiles = Directory.GetFiles(mainDirectory, "*." + SaveFileFormat.lbm + "?", SearchOption.TopDirectoryOnly);
                    var lbmFile = lbmFiles.FirstOrDefault();
                    if (!(lbmFile is null)
                        && model.DataBaseModels.All(m => m.DBSource != DataBaseSource.Msp)) {
                        var databaseModel = model.AddDataBase();
                        databaseModel.DataBasePath = lbmFile;
                    }
                }
            }
            return model;
        }

        public IsotopeTrackSettingModel CreateIsotopeTrackSetting() {
            return new IsotopeTrackSettingModel(storage.Parameter, storage.AnalysisFiles, process);
        }

        public MobilitySettingModel CreateMobilitySetting() {
            return new MobilitySettingModel(storage.Parameter, storage.AnalysisFiles, process);
        }

        public IPeakDetectionSettingModel CreatePeakDetectionSetting() {
            return new PeakDetectionSettingModel(_peakPickBaseParameter, process);
        }

        public IMethodModel BuildMethod() {
            var method = new ImagingImmsMethodModel(_analysisFileBeanModelCollection, _alignmentFileBeanModelCollection, storage, _fileProperties, _studyContext, _broker);
            return method;
        }
    }

    internal sealed class GcmsMethodSettingModelFactory : IMethodSettingModelFactory
    {
        private readonly AnalysisFileBeanModelCollection _analysisFileBeanModelCollection;
        private readonly AlignmentFileBeanModelCollection _alignmentFileBeanModelCollection;
        private readonly IMsdialDataStorage<MsdialGcmsParameter> storage;
        private readonly FilePropertiesModel _projectBaseParameter;
        private readonly StudyContextModel _studyContext;
        private readonly ProcessOption process;
        private readonly IMessageBroker _broker;
        private readonly PeakPickBaseParameterModel _peakPickBaseParameter;

        public GcmsMethodSettingModelFactory(
            AnalysisFileBeanModelCollection analysisFileBeanModelCollection,
            AlignmentFileBeanModelCollection alignmentFileBeanModelCollection,
            IMsdialDataStorage<MsdialGcmsParameter> storage,
            FilePropertiesModel projectBaseParameter,
            StudyContextModel studyContext,
            ProcessOption process,
            IMessageBroker broker) {
            _analysisFileBeanModelCollection = analysisFileBeanModelCollection;
            _alignmentFileBeanModelCollection = alignmentFileBeanModelCollection;
            this.storage = storage;
            _projectBaseParameter = projectBaseParameter ?? throw new ArgumentNullException(nameof(projectBaseParameter));
            _studyContext = studyContext;
            this.process = process;
            _broker = broker;
            if (this.storage.Parameter.TargetOmics == TargetOmics.Proteomics) {
                this.storage.Parameter.MaxChargeNumber = 8;
                this.storage.Parameter.RemoveAfterPrecursor = false;
            }
            _peakPickBaseParameter = new PeakPickBaseParameterModel(storage.Parameter.PeakPickBaseParam);
        }

        public AdductIonSettingModel? CreateAdductIonSetting() {
            return null;
        }

        public IAlignmentParameterSettingModel CreateAlignmentParameterSetting() {
            return new GcmsAlignmentParameterSettingModel(storage.Parameter, DateTime.Now, _analysisFileBeanModelCollection, _alignmentFileBeanModelCollection, process, _broker);
        }

        public IDataCollectionSettingModel CreateDataCollectionSetting() {
            return new GcmsDataCollectionSettingModel(storage.Parameter, _peakPickBaseParameter, process);
        }

        public DeconvolutionSettingModel CreateDeconvolutionSetting() {
            return new DeconvolutionSettingModel(storage.Parameter.ChromDecBaseParam, process);
        }

        public IIdentificationSettingModel CreateIdentifySetting() {
            return new GcmsIdentificationSettingModel(storage.Parameter, _analysisFileBeanModelCollection, process, _broker);
        }

        public IsotopeTrackSettingModel? CreateIsotopeTrackSetting() {
            return null;
        }

        public MobilitySettingModel? CreateMobilitySetting() {
            return null;
        }

        public IPeakDetectionSettingModel CreatePeakDetectionSetting() {
            return new GcmsPeakDetectionSettingModel(_peakPickBaseParameter, storage.Parameter.ChromDecBaseParam, process);
        }

        public IMethodModel BuildMethod() {
            return new GcmsMethodModel(_analysisFileBeanModelCollection, _alignmentFileBeanModelCollection, storage, _projectBaseParameter, _studyContext, _broker);
        }
    }
}
