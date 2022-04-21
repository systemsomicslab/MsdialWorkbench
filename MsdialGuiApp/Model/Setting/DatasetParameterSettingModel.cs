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
using CompMs.MsdialGcMsApi.Parameter;
using CompMs.MsdialImmsCore.DataObj;
using CompMs.MsdialImmsCore.Parameter;
using CompMs.MsdialLcImMsApi.DataObj;
using CompMs.MsdialLcImMsApi.Parameter;
using CompMs.MsdialLcmsApi.Parameter;
using CompMs.MsdialLcMsApi.DataObj;
using System;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Windows;

namespace CompMs.App.Msdial.Model.Setting
{
    public class DatasetParameterSettingModel : BindableBase
    {
        private readonly DatasetFileSettingModel fileSettingModel;
        private readonly Action<DatasetModel> next;

        public DatasetParameterSettingModel(DateTime dt, DatasetFileSettingModel fileSettingModel, Action<DatasetModel> next) {
            this.fileSettingModel = fileSettingModel;
            this.next = next;
            fileSettingModel.PropertyChanged += UpdateDatasetFolderPath;
            DatasetFileName = $"Dataset_{dt:yyyy_MM_dd_hh_mm_ss}";

            IsReadOnly = false;
        }

        public DatasetParameterSettingModel(ParameterBase parameter, DatasetFileSettingModel fileSettingModel) {
            this.fileSettingModel = fileSettingModel;
            fileSettingModel.PropertyChanged += UpdateDatasetFolderPath;
            this.next = null;

            var projectParameter = parameter.ProjectParam;
            DatasetFileName = projectParameter.ProjectFileName;
            Ionization = projectParameter.Ionization;
            SeparationType = GetSeparationType(parameter);
            AcquisitionType = projectParameter.AcquisitionType;
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
            CollisionType = parameter.ProteomicsParam.CollistionType;

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
        private string datasetFileName;

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

        public AcquisitionType AcquisitionType {
            get => acquisitionType;
            set => SetProperty(ref acquisitionType, value);
        }
        private AcquisitionType acquisitionType = AcquisitionType.DDA;

        public MSDataType MS1DataType {
            get => ms1DataType;
            set => SetProperty(ref ms1DataType, value);
        }
        private MSDataType ms1DataType = MSDataType.Profile;

        public MSDataType MS2DataType {
            get => ms2DataType;
            set => SetProperty(ref ms2DataType, value);
        }
        private MSDataType ms2DataType = MSDataType.Profile;

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

            var parameter = ParameterFactory.CreateParameter(Ionization, SeparationType);
            var projectParameter = parameter.ProjectParam;
            fileSettingModel.CommitFileParameters(projectParameter);
            projectParameter.ProjectFileName = DatasetFileName;
            projectParameter.ProjectFolderPath = DatasetFolderPath;
            projectParameter.Ionization = Ionization;
            projectParameter.AcquisitionType = AcquisitionType;
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

            parameter.ProteomicsParam.CollistionType = CollisionType;

            var storage = CreateDataStorage(parameter);
            storage.AnalysisFiles = fileSettingModel.Files.Where(file => file.AnalysisFileIncluded).ToList();
            var counter = 0;
            foreach (var file in storage.AnalysisFiles) {
                file.AnalysisFileId = counter++;
            }
            storage.IupacDatabase = IupacResourceParser.GetIUPACDatabase(); //Get IUPAC reference
            storage.DataBaseMapper = new DataBaseMapper();
            storage.DataBases = DataBaseStorage.CreateEmpty();

            var dataset = new DatasetModel(storage);
            next?.Invoke(dataset);
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
