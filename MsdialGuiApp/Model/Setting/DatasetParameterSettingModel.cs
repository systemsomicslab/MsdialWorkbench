using CompMs.App.Msdial.Model.Core;
using CompMs.App.Msdial.Utility;
using CompMs.Common.Enum;
using CompMs.Common.Parser;
using CompMs.CommonMVVM;
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
using System.Linq;

namespace CompMs.App.Msdial.Model.Setting
{
    public class DatasetParameterSettingModel : BindableBase
    {
        private readonly Action<IDatasetModel> continuous;
        private readonly DatasetFileSettingModel fileSettingModel;

        public DatasetParameterSettingModel(Action<IDatasetModel> continuous, DatasetFileSettingModel fileSettingModel) {
            this.continuous = continuous;
            this.fileSettingModel = fileSettingModel;
        }

        public bool IsComplete {
            get => isComplete;
            private set => SetProperty(ref isComplete, value);
        }
        private bool isComplete;

        public string DatasetFolderPath {
            get => fileSettingModel.ProjectFolderPath;
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

        public DatasetModel Build() {
            if (!string.IsNullOrEmpty(Comment))
                Comment = Comment.Replace("\r", "").Replace("\n", " ");

            var parameter = ParameterFactory.CreateParameter(Ionization, SeparationType);
            ParameterFactory.SetParameterFromDatasetParameterSettingModel(parameter, this);
            var storage = CreateDataStorage(parameter);
            storage.AnalysisFiles = fileSettingModel.Files.ToList();
            storage.IupacDatabase = IupacResourceParser.GetIUPACDatabase(); //Get IUPAC reference
            storage.DataBaseMapper = new DataBaseMapper();
            storage.DataBases = DataBaseStorage.CreateEmpty();

            var dataset = new DatasetModel(storage);
            continuous?.Invoke(dataset);

            return dataset;
        }

        // TODO: How can I remove direct dependency to each methods?
        private IMsdialDataStorage<ParameterBase> CreateDataStorage(ParameterBase parameter) {
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
    }
}
