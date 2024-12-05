using CompMs.App.Msdial.Common;
using CompMs.App.Msdial.Model.Core;
using CompMs.App.Msdial.Utility;
using CompMs.Common.Enum;
using CompMs.Common.Parser;
using CompMs.CommonMVVM;
using CompMs.MsdialCore.DataObj;
using CompMs.MsdialCore.Parameter;
using CompMs.MsdialDimsCore.DataObj;
using CompMs.MsdialDimsCore.Parameter;
using CompMs.MsdialGcMsApi.DataObj;
using CompMs.MsdialGcMsApi.Parameter;
using CompMs.MsdialImmsCore.DataObj;
using CompMs.MsdialImmsCore.Parameter;
using CompMs.MsdialLcImMsApi.DataObj;
using CompMs.MsdialLcImMsApi.Parameter;
using CompMs.MsdialLcmsApi.Parameter;
using CompMs.MsdialLcMsApi.DataObj;
using Reactive.Bindings.Notifiers;
using System;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Windows;

namespace CompMs.App.Msdial.Model.Setting
{
    internal sealed class DatasetParameterSettingModel : BindableBase
    {
        private readonly DatasetFileSettingModel fileSettingModel;
        private readonly Action<DatasetModel>? next;
        private readonly IMessageBroker _broker;

        public DatasetParameterSettingModel(DateTime dt, DatasetFileSettingModel fileSettingModel, Action<DatasetModel> next, IMessageBroker broker) {
            this.fileSettingModel = fileSettingModel;
            this.next = next;
            _broker = broker;
            fileSettingModel.PropertyChanged += UpdateDatasetFolderPath;
            DatasetFileName = $"Dataset_{dt:yyyy_MM_dd_HH_mm_ss}.mddata";

            IsReadOnly = false;
        }

        public DatasetParameterSettingModel(ParameterBase parameter, DatasetFileSettingModel fileSettingModel, IMessageBroker broker) {
            this.fileSettingModel = fileSettingModel;
            _broker = broker;
            fileSettingModel.PropertyChanged += UpdateDatasetFolderPath;
            this.next = null;

            var projectParameter = parameter.ProjectParam;
            DatasetFileName = projectParameter.ProjectFileName;
            Ionization = projectParameter.Ionization;
            SeparationType = GetSeparationType(parameter);
            MS1DataType = projectParameter.MSDataType;
            MS2DataType = projectParameter.MS2DataType;
            IonMode = projectParameter.IonMode;
            TargetOmics = projectParameter.TargetOmics;
            InstrumentType = projectParameter.InstrumentType;
            Instrument = projectParameter.Instrument;
            Authors = projectParameter.Authors;
            License = projectParameter.License;
            CollisionEnergy = projectParameter.CollisionEnergy;
            Comment = projectParameter.Comment;
            CollisionType = parameter.ProteomicsParam.CollisionType;

            IsReadOnly = true;
        }

        public string DatasetFolderPath {
            get => fileSettingModel.ProjectFolderPath;
        }

        private void UpdateDatasetFolderPath(object sender, PropertyChangedEventArgs e) {
            if (e.PropertyName == nameof(DatasetFileSettingModel.ProjectFolderPath)) {
                OnPropertyChanged(nameof(DatasetFolderPath));
            }
        }

        public string DatasetFileName {
            get => datasetFileName;
            set => SetProperty(ref datasetFileName, value);
        }
        private string datasetFileName = string.Empty;

        public Ionization Ionization {
            get => ionization;
            set => SetProperty(ref ionization, value);
        }
        private Ionization ionization = Ionization.ESI;

        public SeparationType SeparationType {
            get => separationType;
            set => SetProperty(ref separationType, value);
        }
        private SeparationType separationType = SeparationType.Chromatography;

        public CollisionType CollisionType {
            get => collisionType;
            set => SetProperty(ref collisionType, value);
        }
        private CollisionType collisionType = CollisionType.HCD;

        public MSDataType MS1DataType {
            get => ms1DataType;
            set => SetProperty(ref ms1DataType, value);
        }
        private MSDataType ms1DataType = MSDataType.Centroid;

        public MSDataType MS2DataType {
            get => ms2DataType;
            set => SetProperty(ref ms2DataType, value);
        }
        private MSDataType ms2DataType = MSDataType.Centroid;

        public IonMode IonMode {
            get => ionMode;
            set => SetProperty(ref ionMode, value);
        }
        private IonMode ionMode = IonMode.Positive;

        public TargetOmics TargetOmics {
            get => targetOmics;
            set => SetProperty(ref targetOmics, value);
        }
        private TargetOmics targetOmics = TargetOmics.Metabolomics;

        public string InstrumentType {
            get => instrumentType;
            set => SetProperty(ref instrumentType, value);
        }
        private string instrumentType = string.Empty;

        public string Instrument {
            get => instrument;
            set => SetProperty(ref instrument, value);
        }
        private string instrument = string.Empty;

        public string Authors {
            get => authors;
            set => SetProperty(ref authors, value);
        }
        private string authors = string.Empty;

        public string License {
            get => license;
            set => SetProperty(ref license, value);
        }
        private string license = string.Empty;

        public string CollisionEnergy {
            get => collisionEnergy;
            set => SetProperty(ref collisionEnergy, value);
        }
        private string collisionEnergy = string.Empty;

        public string Comment {
            get => comment;
            set => SetProperty(ref comment, value);
        }
        private string comment = string.Empty;

        public bool IsReadOnly { get; }

        public void Prepare() {
            if (IsReadOnly) {
                return;
            }

            if (TargetOmics == TargetOmics.Lipidomics && Ionization == Ionization.ESI) {
                var wd = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
                if (Directory.GetFiles(wd, $"*.{SaveFileFormat.lbm}?", SearchOption.TopDirectoryOnly).Length != 1)
                {
                    MessageBox.Show("There is no LBM file or several LBM files are existed in this application folder. Please see the tutorial.",
                                    "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }
            }


            if (!string.IsNullOrEmpty(Comment))
                Comment = Comment.Replace("\r", "").Replace("\n", " ");

            if (!DatasetFileName.EndsWith(".mddata")) {
                DatasetFileName += ".mddata";
            }

            var fileID = 0;
            foreach (var file in fileSettingModel.IncludedFileModels) {
                file.AnalysisFileId = fileID++;
            }
            var parameter = CreateParameter();
            var projectParameter = parameter.ProjectParam;
            fileSettingModel.CommitFileParameters(projectParameter);
            projectParameter.ProjectFileName = DatasetFileName;
            projectParameter.ProjectFolderPath = DatasetFolderPath;
            projectParameter.Ionization = Ionization;
            projectParameter.MSDataType = MS1DataType;
            projectParameter.MS2DataType = MS2DataType;
            projectParameter.IonMode = IonMode;
            projectParameter.TargetOmics = TargetOmics;
            projectParameter.InstrumentType = InstrumentType;
            projectParameter.Instrument = Instrument;
            projectParameter.Authors = Authors;
            projectParameter.License = License;
            projectParameter.CollisionEnergy = CollisionEnergy;
            projectParameter.Comment = Comment;

            parameter.ProteomicsParam.CollisionType = CollisionType;
            if (projectParameter.TargetOmics == TargetOmics.Proteomics) parameter.ProteomicsParam.IsDoAndromedaMs2Deconvolution = true;

            var storage = CreateDataStorage(parameter);
            storage.AnalysisFiles = fileSettingModel.IncludedFileModels.Select(f => f.File).ToList();
            storage.IupacDatabase = IupacResourceParser.GetIUPACDatabase(); //Get IUPAC reference
            storage.DataBases = DataBaseStorage.CreateEmpty();
            storage.DataBaseMapper = new DataBaseMapper();

            var dataset = new DatasetModel(storage, _broker);
            next?.Invoke(dataset);
        }

        private ParameterBase CreateParameter() {
            if (SeparationType == (SeparationType.Imaging | SeparationType.IonMobility))
                return new MsdialImmsParameter(isImaging: true, GlobalResources.Instance.IsLabPrivate);
            if (Ionization == Ionization.EI && SeparationType == SeparationType.Chromatography) {
                var parameter = new MsdialGcmsParameter(GlobalResources.Instance.IsLabPrivate);
                parameter.PeakPickBaseParam.MassRangeEnd = 1000;
                parameter.PeakPickBaseParam.CentroidMs1Tolerance = .025f;
                parameter.PeakPickBaseParam.MinimumDatapoints = 20;
                parameter.ChromDecBaseParam.AccuracyType = AccuracyType.IsNominal;
                parameter.ChromDecBaseParam.AmplitudeCutoff = 10;
                parameter.RetentionType = RetentionType.RI;
                parameter.RefSpecMatchBaseParam.MspSearchParam.RiTolerance = 20;
                parameter.RefSpecMatchBaseParam.MspSearchParam.RtTolerance = .5f;
                parameter.RefSpecMatchBaseParam.MspSearchParam.Ms1Tolerance = .5f;
                parameter.RefSpecMatchBaseParam.MspSearchParam.WeightedDotProductCutOff = .7f;
                parameter.RefSpecMatchBaseParam.MspSearchParam.TotalScoreCutoff = .7f;
                parameter.RefSpecMatchBaseParam.MspSearchParam.IsUseTimeForAnnotationScoring = true;
                parameter.RefSpecMatchBaseParam.OnlyReportTopHitInMspSearch = true;
                parameter.RefSpecMatchBaseParam.FileIdRiInfoDictionary = fileSettingModel.IncludedFileModels.ToDictionary(f => f.AnalysisFileId, _ => new RiDictionaryInfo());
                parameter.RetentionIndexAlignmentTolerance = 20;
                parameter.AlignmentBaseParam.RetentionTimeAlignmentTolerance = .075f;
                parameter.AlignmentBaseParam.Ms1AlignmentTolerance = .7f;
                return parameter;
            }
            if (Ionization == Ionization.ESI && SeparationType == SeparationType.Chromatography)
                return new MsdialLcmsParameter(GlobalResources.Instance.IsLabPrivate);
            if (Ionization == Ionization.ESI && SeparationType == (SeparationType.Chromatography | SeparationType.IonMobility))
                return new MsdialLcImMsParameter(GlobalResources.Instance.IsLabPrivate);
            if (Ionization == Ionization.ESI && SeparationType == SeparationType.Infusion)
                return new MsdialDimsParameter(GlobalResources.Instance.IsLabPrivate);
            if (Ionization == Ionization.ESI && SeparationType == (SeparationType.Infusion | SeparationType.IonMobility))
                return new MsdialImmsParameter(isImaging: false, GlobalResources.Instance.IsLabPrivate);
            throw new Exception("Not supported separation type is selected.");
        }

        // TODO: How can I remove direct dependency to each methods?
        private static IMsdialDataStorage<ParameterBase> CreateDataStorage(ParameterBase parameter) {
            switch (parameter) {
                case MsdialLcImMsParameter lcimmsParameter:
                    return new MsdialLcImMsDataStorage() { MsdialLcImMsParameter = lcimmsParameter };
                case MsdialLcmsParameter lcmsParameter:
                    return new MsdialLcmsDataStorage() { MsdialLcmsParameter = lcmsParameter };
                case MsdialImmsParameter immsParameter:
                    return new MsdialImmsDataStorage() { MsdialImmsParameter = immsParameter };
                case MsdialDimsParameter dimsParameter:
                    return new MsdialDimsDataStorage() { MsdialDimsParameter = dimsParameter };
                case MsdialGcmsParameter gcmsParameter:
                    return new MsdialGcmsDataStorage() { MsdialGcmsParameter = gcmsParameter };
            }
            throw new NotImplementedException("This method is not implemented");
        }

        private static SeparationType GetSeparationType(ParameterBase parameter) {
            switch (parameter) {
                case MsdialLcImMsParameter _:
                    return SeparationType.Chromatography | SeparationType.IonMobility;
                case MsdialLcmsParameter _:
                case MsdialGcmsParameter _:
                    return SeparationType.Chromatography;
                case MsdialImmsParameter _:
                    return SeparationType.Infusion | SeparationType.IonMobility;
                case MsdialDimsParameter _:
                    return SeparationType.Infusion;
            }
            throw new NotImplementedException("This method is not implemented");
        }
    }
}
