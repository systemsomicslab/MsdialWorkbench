using CompMs.App.Msdial.Common;
using CompMs.App.Msdial.Model.Core;
using CompMs.App.Msdial.Model.DataObj;
using CompMs.App.Msdial.Model.Dims;
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
using CompMs.MsdialImmsCore.Parameter;
using CompMs.MsdialLcImMsApi.Parameter;
using CompMs.MsdialLcmsApi.Parameter;
using Reactive.Bindings.Notifiers;
using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Reactive;
using System.Reactive.Linq;
using System.Reflection;

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
        IMethodModel BuildMethod();
    }

    internal sealed class MethodSettingModelFactory : IMethodSettingModelFactory
    {
        public MethodSettingModelFactory(AnalysisFileBeanModelCollection analysisFileBeanModelCollection, AlignmentFileBeanModelCollection alignmentFileModelCollection, IMsdialDataStorage<ParameterBase> storage, ProjectBaseParameterModel projectBaseParameter, ProcessOption process, IMessageBroker messageBroker) {
            switch (storage) {
                case IMsdialDataStorage<MsdialLcImMsParameter> lcimmsStorage:
                    factoryImpl = new LcimmsMethodSettingModelFactory(analysisFileBeanModelCollection, alignmentFileModelCollection, lcimmsStorage, projectBaseParameter, process, messageBroker);
                    break;
                case IMsdialDataStorage<MsdialLcmsParameter> lcmsStorage:
                    factoryImpl = new LcmsMethodSettingModelFactory(analysisFileBeanModelCollection, alignmentFileModelCollection, lcmsStorage, projectBaseParameter, process, messageBroker);
                    break;
                case IMsdialDataStorage<MsdialImmsParameter> immsStorage:
                    if (immsStorage.Parameter.MachineCategory == MachineCategory.IIMMS) {
                        factoryImpl = new ImagingImmsMethodSettingModelFactory(analysisFileBeanModelCollection, immsStorage, projectBaseParameter, process, messageBroker, alignmentFileModelCollection);
                    }
                    else {
                        factoryImpl = new ImmsMethodSettingModelFactory(analysisFileBeanModelCollection, alignmentFileModelCollection, immsStorage, projectBaseParameter, process, messageBroker);
                    }
                    break;
                case IMsdialDataStorage<MsdialDimsParameter> dimsStorage:
                    factoryImpl = new DimsMethodSettingModelFactory(analysisFileBeanModelCollection, alignmentFileModelCollection, dimsStorage, projectBaseParameter, process, messageBroker);
                    break;
                default:
                    throw new ArgumentException(nameof(storage));
            }
        }

        private readonly IMethodSettingModelFactory factoryImpl;

        public IMethodModel BuildMethod() => factoryImpl.BuildMethod();
        public AdductIonSettingModel CreateAdductIonSetting() => factoryImpl.CreateAdductIonSetting();
        public AlignmentParameterSettingModel CreateAlignmentParameterSetting() => factoryImpl.CreateAlignmentParameterSetting();
        public DataCollectionSettingModel CreateDataCollectionSetting() => factoryImpl.CreateDataCollectionSetting();
        public DeconvolutionSettingModel CreateDeconvolutionSetting() => factoryImpl.CreateDeconvolutionSetting();
        public IdentifySettingModel CreateIdentifySetting() => factoryImpl.CreateIdentifySetting();
        public IsotopeTrackSettingModel CreateIsotopeTrackSetting() => factoryImpl.CreateIsotopeTrackSetting();
        public MobilitySettingModel CreateMobilitySetting() => factoryImpl.CreateMobilitySetting();
        public PeakDetectionSettingModel CreatePeakDetectionSetting() => factoryImpl.CreatePeakDetectionSetting();
    }


    sealed class DimsMethodSettingModelFactory : IMethodSettingModelFactory
    {
        private readonly AnalysisFileBeanModelCollection _analysisFileBeanModelCollection;
        private readonly AlignmentFileBeanModelCollection _alignmentFileBeanModelCollection;
        private readonly IMsdialDataStorage<MsdialDimsParameter> storage;
        private readonly ProjectBaseParameterModel _projectBaseParameter;
        private readonly ProcessOption process;
        private readonly IMessageBroker _messageBroker;

        public DimsMethodSettingModelFactory(AnalysisFileBeanModelCollection analysisFileBeanModelCollection, AlignmentFileBeanModelCollection alignmentFileBeanModelCollection, IMsdialDataStorage<MsdialDimsParameter> storage, ProjectBaseParameterModel projectBaseParameter, ProcessOption process, IMessageBroker messageBroker) {
            _analysisFileBeanModelCollection = analysisFileBeanModelCollection;
            _alignmentFileBeanModelCollection = alignmentFileBeanModelCollection;
            this.storage = storage;
            _projectBaseParameter = projectBaseParameter ?? throw new ArgumentNullException(nameof(projectBaseParameter));
            this.process = process;
            _messageBroker = messageBroker;
        }

        public AdductIonSettingModel CreateAdductIonSetting() {
            return new AdductIonSettingModel(storage.Parameter, process);
        }

        public AlignmentParameterSettingModel CreateAlignmentParameterSetting() {
            return new AlignmentParameterSettingModel(storage.Parameter, DateTime.Now, storage.AnalysisFiles, _alignmentFileBeanModelCollection, process);
        }

        public DataCollectionSettingModel CreateDataCollectionSetting() {
            return new DataCollectionSettingModel(storage.Parameter, storage.AnalysisFiles, process);
        }

        public DeconvolutionSettingModel CreateDeconvolutionSetting() {
            return new DeconvolutionSettingModel(storage.Parameter.ChromDecBaseParam, process);
        }

        public IdentifySettingModel CreateIdentifySetting() {
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

        public MobilitySettingModel CreateMobilitySetting() {
            return null;
        }

        public PeakDetectionSettingModel CreatePeakDetectionSetting() {
            return new PeakDetectionSettingModel(storage.Parameter.PeakPickBaseParam, process);
        }

        public IMethodModel BuildMethod() {
            return new DimsMethodModel(storage, _analysisFileBeanModelCollection, _alignmentFileBeanModelCollection, _projectBaseParameter, _messageBroker);
        }
    }

    sealed class LcmsMethodSettingModelFactory : IMethodSettingModelFactory
    {
        private readonly AnalysisFileBeanModelCollection _analysisFileBeanModelCollection;
        private readonly AlignmentFileBeanModelCollection _alignmentFileBeanModelCollection;
        private readonly IMsdialDataStorage<MsdialLcmsParameter> storage;
        private readonly ProjectBaseParameterModel _projectBaseParameter;
        private readonly ProcessOption process;
        private readonly IMessageBroker _broker;

        public LcmsMethodSettingModelFactory(AnalysisFileBeanModelCollection analysisFileBeanModelCollection, AlignmentFileBeanModelCollection alignmentFileBeanModelCollection, IMsdialDataStorage<MsdialLcmsParameter> storage, ProjectBaseParameterModel projectBaseParameter, ProcessOption process, IMessageBroker broker) {
            _analysisFileBeanModelCollection = analysisFileBeanModelCollection;
            _alignmentFileBeanModelCollection = alignmentFileBeanModelCollection;
            this.storage = storage;
            _projectBaseParameter = projectBaseParameter ?? throw new ArgumentNullException(nameof(projectBaseParameter));
            this.process = process;
            _broker = broker;
            if (this.storage.Parameter.TargetOmics == TargetOmics.Proteomics) {
                this.storage.Parameter.MaxChargeNumber = 8;
                this.storage.Parameter.RemoveAfterPrecursor = false;
            }
        }

        public AdductIonSettingModel CreateAdductIonSetting() {
            return new AdductIonSettingModel(storage.Parameter, process);
        }

        public AlignmentParameterSettingModel CreateAlignmentParameterSetting() {
            return new AlignmentParameterSettingModel(storage.Parameter, DateTime.Now, storage.AnalysisFiles, _alignmentFileBeanModelCollection, process);
        }

        public DataCollectionSettingModel CreateDataCollectionSetting() {
            return new DataCollectionSettingModel(storage.Parameter, storage.AnalysisFiles, process);
        }

        public DeconvolutionSettingModel CreateDeconvolutionSetting() {
            return new DeconvolutionSettingModel(storage.Parameter.ChromDecBaseParam, process);
        }

        public IdentifySettingModel CreateIdentifySetting() {
            var parameter = storage.Parameter;
            var model = new IdentifySettingModel(storage.Parameter, new LcmsAnnotatorSettingFactory(parameter), process, _broker, storage.DataBases);

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
            return null;
        }

        public PeakDetectionSettingModel CreatePeakDetectionSetting() {
            return new PeakDetectionSettingModel(storage.Parameter.PeakPickBaseParam, process);
        }

        public IMethodModel BuildMethod() {
            return new LcmsMethodModel(_analysisFileBeanModelCollection, _alignmentFileBeanModelCollection, storage, new StandardDataProviderFactory(retry: 5, isGuiProcess: true), _projectBaseParameter, _broker);
        }
    }

    sealed class ImmsMethodSettingModelFactory : IMethodSettingModelFactory
    {
        private readonly AnalysisFileBeanModelCollection _analysisFileBeanModelCollection;
        private readonly AlignmentFileBeanModelCollection _alignmentFileBeanModelCollection;
        private readonly IMsdialDataStorage<MsdialImmsParameter> storage;
        private readonly ProjectBaseParameterModel _projectBaseParameter;
        private readonly ProcessOption process;
        private readonly IMessageBroker _broker;

        public ImmsMethodSettingModelFactory(AnalysisFileBeanModelCollection analysisFileBeanModelCollection, AlignmentFileBeanModelCollection alignmentFileBeanModelCollection, IMsdialDataStorage<MsdialImmsParameter> storage, ProjectBaseParameterModel projectBaseParameter, ProcessOption process, IMessageBroker broker) {
            _analysisFileBeanModelCollection = analysisFileBeanModelCollection;
            _alignmentFileBeanModelCollection = alignmentFileBeanModelCollection;
            this.storage = storage;
            _projectBaseParameter = projectBaseParameter ?? throw new ArgumentNullException(nameof(projectBaseParameter));
            this.process = process;
            _broker = broker;
        }

        public AdductIonSettingModel CreateAdductIonSetting() {
            return new AdductIonSettingModel(storage.Parameter, process);
        }

        public AlignmentParameterSettingModel CreateAlignmentParameterSetting() {
            return new AlignmentParameterSettingModel(storage.Parameter, DateTime.Now, storage.AnalysisFiles, _alignmentFileBeanModelCollection, process);
        }

        public DataCollectionSettingModel CreateDataCollectionSetting() {
            return new DataCollectionSettingModel(storage.Parameter, storage.AnalysisFiles, process);
        }

        public DeconvolutionSettingModel CreateDeconvolutionSetting() {
            return new DeconvolutionSettingModel(storage.Parameter.ChromDecBaseParam, process);
        }

        public IdentifySettingModel CreateIdentifySetting() {
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

        public PeakDetectionSettingModel CreatePeakDetectionSetting() {
            return new PeakDetectionSettingModel(storage.Parameter.PeakPickBaseParam, process);
        }

        public IMethodModel BuildMethod() {
            return new ImmsMethodModel(_analysisFileBeanModelCollection, _alignmentFileBeanModelCollection, storage, _projectBaseParameter, _broker);
        }
    }

    sealed class LcimmsMethodSettingModelFactory : IMethodSettingModelFactory
    {
        private readonly AnalysisFileBeanModelCollection _analysisFileBeanModelCollection;
        private readonly AlignmentFileBeanModelCollection _alignmentFileBeanModelCollection;
        private readonly IMsdialDataStorage<MsdialLcImMsParameter> storage;
        private readonly ProjectBaseParameterModel _projectBaseParameter;
        private readonly ProcessOption process;
        private readonly IMessageBroker _broker;

        public LcimmsMethodSettingModelFactory(AnalysisFileBeanModelCollection analysisFileBeanModelCollection, AlignmentFileBeanModelCollection alignmentFileBeanModelCollection, IMsdialDataStorage<MsdialLcImMsParameter> storage, ProjectBaseParameterModel projectBaseParameter, ProcessOption process, IMessageBroker broker) {
            _analysisFileBeanModelCollection = analysisFileBeanModelCollection;
            _alignmentFileBeanModelCollection = alignmentFileBeanModelCollection;
            this.storage = storage;
            _projectBaseParameter = projectBaseParameter ?? throw new ArgumentNullException(nameof(projectBaseParameter));
            this.process = process;
            _broker = broker;
        }

        public AdductIonSettingModel CreateAdductIonSetting() {
            return new AdductIonSettingModel(storage.Parameter, process);
        }

        public AlignmentParameterSettingModel CreateAlignmentParameterSetting() {
            return new AlignmentParameterSettingModel(storage.Parameter, DateTime.Now, storage.AnalysisFiles, _alignmentFileBeanModelCollection, process);
        }

        public DataCollectionSettingModel CreateDataCollectionSetting() {
            return new DataCollectionSettingModel(storage.Parameter, storage.AnalysisFiles, process);
        }

        public DeconvolutionSettingModel CreateDeconvolutionSetting() {
            return new DeconvolutionSettingModel(storage.Parameter.ChromDecBaseParam, process);
        }

        public IdentifySettingModel CreateIdentifySetting() {
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

        public PeakDetectionSettingModel CreatePeakDetectionSetting() {
            return new PeakDetectionSettingModel(storage.Parameter.PeakPickBaseParam, process);
        }

        public IMethodModel BuildMethod() {
            return new LcimmsMethodModel(_analysisFileBeanModelCollection, _alignmentFileBeanModelCollection, storage, _projectBaseParameter, _broker);
        }
    }

    sealed class ImagingImmsMethodSettingModelFactory : IMethodSettingModelFactory
    {
        private readonly AnalysisFileBeanModelCollection _analysisFileBeanModelCollection;
        private readonly AlignmentFileBeanModelCollection _alignmentFileBeanModelCollection;
        private readonly IMsdialDataStorage<MsdialImmsParameter> storage;
        private readonly ProjectBaseParameterModel _projectBaseParameter;
        private readonly ProcessOption process;
        private readonly IMessageBroker _broker;

        public ImagingImmsMethodSettingModelFactory(AnalysisFileBeanModelCollection analysisFileBeanModelCollection, IMsdialDataStorage<MsdialImmsParameter> storage, ProjectBaseParameterModel projectBaseParameter, ProcessOption process, IMessageBroker broker, AlignmentFileBeanModelCollection alignmentFileBeanModelCollection) {
            _analysisFileBeanModelCollection = analysisFileBeanModelCollection;
            _alignmentFileBeanModelCollection = alignmentFileBeanModelCollection;
            this.storage = storage;
            _projectBaseParameter = projectBaseParameter ?? throw new ArgumentNullException(nameof(projectBaseParameter));
            this.process = process;
            _broker = broker;
        }

        public AdductIonSettingModel CreateAdductIonSetting() {
            return new AdductIonSettingModel(storage.Parameter, process);
        }

        public AlignmentParameterSettingModel CreateAlignmentParameterSetting() {
            return new AlignmentParameterSettingModel(storage.Parameter, DateTime.Now, storage.AnalysisFiles, _alignmentFileBeanModelCollection, process);
        }

        public DataCollectionSettingModel CreateDataCollectionSetting() {
            return new DataCollectionSettingModel(storage.Parameter, storage.AnalysisFiles, process);
        }

        public DeconvolutionSettingModel CreateDeconvolutionSetting() {
            return new DeconvolutionSettingModel(storage.Parameter.ChromDecBaseParam, process);
        }

        public IdentifySettingModel CreateIdentifySetting() {
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

        public PeakDetectionSettingModel CreatePeakDetectionSetting() {
            return new PeakDetectionSettingModel(storage.Parameter.PeakPickBaseParam, process);
        }

        public IMethodModel BuildMethod() {
            var method = new ImagingImmsMethodModel(_analysisFileBeanModelCollection, _alignmentFileBeanModelCollection, storage, _projectBaseParameter, _broker);
            return method;
        }
    }
}
