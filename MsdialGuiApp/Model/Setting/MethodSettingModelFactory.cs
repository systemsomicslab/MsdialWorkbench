using CompMs.App.Msdial.Common;
using CompMs.App.Msdial.Model.Core;
using CompMs.App.Msdial.Model.Dims;
using CompMs.App.Msdial.Model.Imms;
using CompMs.App.Msdial.Model.Lcimms;
using CompMs.App.Msdial.Model.Lcms;
using CompMs.App.Msdial.Model.Loader;
using CompMs.Common.DataObj.Result;
using CompMs.Common.Enum;
using CompMs.MsdialCore.Algorithm;
using CompMs.MsdialCore.DataObj;
using CompMs.MsdialCore.Parameter;
using CompMs.MsdialDimsCore.Parameter;
using CompMs.MsdialImmsCore.Parameter;
using CompMs.MsdialLcImMsApi.Parameter;
using CompMs.MsdialLcmsApi.Parameter;
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

    public sealed class MethodSettingModelFactory : IMethodSettingModelFactory
    {
        public MethodSettingModelFactory(IMsdialDataStorage<ParameterBase> storage, IObservable<Unit> observeParameterChanged, ProcessOption process)
            : this(storage,
                  observeParameterChanged.Select(_ => storage.Parameter),
                  observeParameterChanged.Select(_ => new HeightBarItemsLoader(storage.Parameter.FileID_ClassName)),
                  process) {

        }

        public MethodSettingModelFactory(IMsdialDataStorage<ParameterBase> storage, IObservable<ParameterBase> parameterAsObservable, IObservable<IBarItemsLoader> loader, ProcessOption process) {
            switch (storage) {
                case IMsdialDataStorage<MsdialLcImMsParameter> lcimmsStorage:
                    factoryImpl = new LcimmsMethodSettingModelFactory(lcimmsStorage, process);
                    break;
                case IMsdialDataStorage<MsdialLcmsParameter> lcmsStorage:
                    factoryImpl = new LcmsMethodSettingModelFactory(lcmsStorage, parameterAsObservable, loader, process);
                    break;
                case IMsdialDataStorage<MsdialImmsParameter> immsStorage:
                    factoryImpl = new ImmsMethodSettingModelFactory(immsStorage, process);
                    break;
                case IMsdialDataStorage<MsdialDimsParameter> dimsStorage:
                    factoryImpl = new DimsMethodSettingModelFactory(dimsStorage, process);
                    break;
                default:
                    throw new ArgumentException(nameof(storage));
            }
        }

        private readonly IMethodSettingModelFactory factoryImpl;

        public MethodModelBase BuildMethod() => factoryImpl.BuildMethod();
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
        private readonly IMsdialDataStorage<MsdialDimsParameter> storage;
        private readonly ProcessOption process;

        public DimsMethodSettingModelFactory(IMsdialDataStorage<MsdialDimsParameter> storage, ProcessOption process) {
            this.storage = storage;
            this.process = process;
        }

        public AdductIonSettingModel CreateAdductIonSetting() {
            return new AdductIonSettingModel(storage.Parameter, process);
        }

        public AlignmentParameterSettingModel CreateAlignmentParameterSetting() {
            return new AlignmentParameterSettingModel(storage.Parameter, DateTime.Now, storage.AnalysisFiles, storage.AlignmentFiles, process);
        }

        public DataCollectionSettingModel CreateDataCollectionSetting() {
            return new DataCollectionSettingModel(storage.Parameter, storage.AnalysisFiles, process);
        }

        public DeconvolutionSettingModel CreateDeconvolutionSetting() {
            return new DeconvolutionSettingModel(storage.Parameter.ChromDecBaseParam, process);
        }

        public IdentifySettingModel CreateIdentifySetting() {
            var parameter = storage.Parameter;
            var model = new IdentifySettingModel(parameter, new DimsAnnotatorSettingModelFactory(), process, storage.DataBases);

            if (parameter.TargetOmics == TargetOmics.Lipidomics) {
                if (model.DataBaseModels.Count == 0) {
                    if (parameter.CollistionType == CollisionType.EIEIO
                        && model.DataBaseModels.All(m => m.DBSource != DataBaseSource.EadLipid)) {
                        var databaseModel = model.AddDataBaseZZZ();
                        databaseModel.DBSource = DataBaseSource.EadLipid;
                    }

                    string mainDirectory = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
                    var lbmFiles = Directory.GetFiles(mainDirectory, "*." + SaveFileFormat.lbm + "?", SearchOption.TopDirectoryOnly);
                    var lbmFile = lbmFiles.FirstOrDefault();
                    if (!(lbmFile is null)
                        && model.DataBaseModels.All(m => m.DBSource != DataBaseSource.Msp)) {
                        var databaseModel = model.AddDataBaseZZZ();
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

        public MethodModelBase BuildMethod() {
            return new DimsMethodModel(storage, storage.AnalysisFiles, storage.AlignmentFiles);
        }
    }

    sealed class LcmsMethodSettingModelFactory : IMethodSettingModelFactory
    {
        private readonly IMsdialDataStorage<MsdialLcmsParameter> storage;
        private readonly IObservable<ParameterBase> parameterAsObservable;
        private readonly IObservable<IBarItemsLoader> loader;
        private readonly ProcessOption process;

        public LcmsMethodSettingModelFactory(
            IMsdialDataStorage<MsdialLcmsParameter> storage, 
            IObservable<ParameterBase> parameterAsObservable,
            IObservable<IBarItemsLoader> observeBarItemsLoader,
            ProcessOption process) {

            this.storage = storage;
            this.parameterAsObservable = parameterAsObservable;
            loader = observeBarItemsLoader;
            this.process = process;
        }

        public AdductIonSettingModel CreateAdductIonSetting() {
            return new AdductIonSettingModel(storage.Parameter, process);
        }

        public AlignmentParameterSettingModel CreateAlignmentParameterSetting() {
            return new AlignmentParameterSettingModel(storage.Parameter, DateTime.Now, storage.AnalysisFiles, storage.AlignmentFiles, process);
        }

        public DataCollectionSettingModel CreateDataCollectionSetting() {
            return new DataCollectionSettingModel(storage.Parameter, storage.AnalysisFiles, process);
        }

        public DeconvolutionSettingModel CreateDeconvolutionSetting() {
            return new DeconvolutionSettingModel(storage.Parameter.ChromDecBaseParam, process);
        }

        public IdentifySettingModel CreateIdentifySetting() {
            var parameter = storage.Parameter;
            var model = new IdentifySettingModel(storage.Parameter, new LcmsAnnotatorSettingFactory(), process, storage.DataBases);

            if (parameter.TargetOmics == TargetOmics.Lipidomics) {
                if (model.DataBaseModels.Count == 0) {
                    if (parameter.CollistionType == CollisionType.EIEIO
                        && model.DataBaseModels.All(m => m.DBSource != DataBaseSource.EadLipid)) {
                        var databaseModel = model.AddDataBaseZZZ();
                        databaseModel.DBSource = DataBaseSource.EadLipid;
                    }

                    string mainDirectory = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
                    var lbmFiles = Directory.GetFiles(mainDirectory, "*." + SaveFileFormat.lbm + "?", SearchOption.TopDirectoryOnly);
                    var lbmFile = lbmFiles.FirstOrDefault();
                    if (!(lbmFile is null)
                        && model.DataBaseModels.All(m => m.DBSource != DataBaseSource.Msp)) {
                        var databaseModel = model.AddDataBaseZZZ();
                        databaseModel.DataBasePath = lbmFile;
                    }
                }
            }
            else if (parameter.TargetOmics == TargetOmics.Proteomics) {
                parameter.MaxChargeNumber = 8;
                parameter.RemoveAfterPrecursor = false;
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

        public MethodModelBase BuildMethod() {
            return new LcmsMethodModel(storage, new StandardDataProviderFactory(retry: 5, isGuiProcess: true), parameterAsObservable, loader);
        }
    }

    sealed class ImmsMethodSettingModelFactory : IMethodSettingModelFactory
    {
        private readonly IMsdialDataStorage<MsdialImmsParameter> storage;
        private readonly ProcessOption process;

        public ImmsMethodSettingModelFactory(IMsdialDataStorage<MsdialImmsParameter> storage, ProcessOption process) {
            this.storage = storage;
            this.process = process;
        }

        public AdductIonSettingModel CreateAdductIonSetting() {
            return new AdductIonSettingModel(storage.Parameter, process);
        }

        public AlignmentParameterSettingModel CreateAlignmentParameterSetting() {
            return new AlignmentParameterSettingModel(storage.Parameter, DateTime.Now, storage.AnalysisFiles, storage.AlignmentFiles, process);
        }

        public DataCollectionSettingModel CreateDataCollectionSetting() {
            return new DataCollectionSettingModel(storage.Parameter, storage.AnalysisFiles, process);
        }

        public DeconvolutionSettingModel CreateDeconvolutionSetting() {
            return new DeconvolutionSettingModel(storage.Parameter.ChromDecBaseParam, process);
        }

        public IdentifySettingModel CreateIdentifySetting() {
            throw new NotImplementedException("ImmsAnnotatorSettingModelFactory is not implemented!");
            var parameter = storage.Parameter;
            var model = new IdentifySettingModel(storage.Parameter, null, process, storage.DataBases);

            if (parameter.TargetOmics == TargetOmics.Lipidomics) {
                if (model.DataBaseModels.Count == 0) {
                    if (parameter.CollistionType == CollisionType.EIEIO
                        && model.DataBaseModels.All(m => m.DBSource != DataBaseSource.EadLipid)) {
                        var databaseModel = model.AddDataBaseZZZ();
                        databaseModel.DBSource = DataBaseSource.EadLipid;
                    }

                    string mainDirectory = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
                    var lbmFiles = Directory.GetFiles(mainDirectory, "*." + SaveFileFormat.lbm + "?", SearchOption.TopDirectoryOnly);
                    var lbmFile = lbmFiles.FirstOrDefault();
                    if (!(lbmFile is null)
                        && model.DataBaseModels.All(m => m.DBSource != DataBaseSource.Msp)) {
                        var databaseModel = model.AddDataBaseZZZ();
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

        public MethodModelBase BuildMethod() {
            return new ImmsMethodModel(storage);
        }
    }

    sealed class LcimmsMethodSettingModelFactory : IMethodSettingModelFactory
    {
        private readonly IMsdialDataStorage<MsdialLcImMsParameter> storage;
        private readonly ProcessOption process;

        public LcimmsMethodSettingModelFactory(IMsdialDataStorage<MsdialLcImMsParameter> storage, ProcessOption process) {
            this.storage = storage;
            this.process = process;
        }

        public AdductIonSettingModel CreateAdductIonSetting() {
            return new AdductIonSettingModel(storage.Parameter, process);
        }

        public AlignmentParameterSettingModel CreateAlignmentParameterSetting() {
            return new AlignmentParameterSettingModel(storage.Parameter, DateTime.Now, storage.AnalysisFiles, storage.AlignmentFiles, process);
        }

        public DataCollectionSettingModel CreateDataCollectionSetting() {
            return new DataCollectionSettingModel(storage.Parameter, storage.AnalysisFiles, process);
        }

        public DeconvolutionSettingModel CreateDeconvolutionSetting() {
            return new DeconvolutionSettingModel(storage.Parameter.ChromDecBaseParam, process);
        }

        public IdentifySettingModel CreateIdentifySetting() {
            var parameter = storage.Parameter;
            var model = new IdentifySettingModel(storage.Parameter, new LcimmsAnnotatorSettingFactory(), process, storage.DataBases);

            if (parameter.TargetOmics == TargetOmics.Lipidomics) {
                if (model.DataBaseModels.Count == 0) {
                    if (parameter.CollistionType == CollisionType.EIEIO
                        && model.DataBaseModels.All(m => m.DBSource != DataBaseSource.EadLipid)) {
                        var databaseModel = model.AddDataBaseZZZ();
                        databaseModel.DBSource = DataBaseSource.EadLipid;
                    }

                    string mainDirectory = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
                    var lbmFiles = Directory.GetFiles(mainDirectory, "*." + SaveFileFormat.lbm + "?", SearchOption.TopDirectoryOnly);
                    var lbmFile = lbmFiles.FirstOrDefault();
                    if (!(lbmFile is null)
                        && model.DataBaseModels.All(m => m.DBSource != DataBaseSource.Msp)) {
                        var databaseModel = model.AddDataBaseZZZ();
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

        public MethodModelBase BuildMethod() {
            return new LcimmsMethodModel(storage);
        }
    }
}
