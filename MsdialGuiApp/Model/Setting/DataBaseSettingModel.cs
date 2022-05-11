using CompMs.Common.DataObj.Result;
using CompMs.Common.Enum;
using CompMs.Common.Parser;
using CompMs.Common.Query;
using CompMs.CommonMVVM;
using CompMs.MsdialCore.DataObj;
using CompMs.MsdialCore.Parameter;
using CompMs.MsdialCore.Utility;
using System;

namespace CompMs.App.Msdial.Model.Setting
{
    public class DataBaseSettingModel : BindableBase
    {
        public DataBaseSettingModel(ParameterBase parameter) {
            this.parameter = parameter ?? throw new ArgumentNullException(nameof(parameter));
            LipidQueryContainer = parameter.LipidQueryContainer;
            LipidQueryContainer.IonMode = parameter.ProjectParam.IonMode;

            ProteomicsParameter = parameter.ProteomicsParam ?? new ProteomicsParameter();
            IsLoaded = false;
        }

        public DataBaseSettingModel(ParameterBase parameter, IReferenceDataBase database) {
            this.parameter = parameter ?? throw new ArgumentNullException(nameof(parameter));
            switch (database) {
                case MoleculeDataBase mdb:
                    metabolomicsDB = mdb;
                    DBSource = mdb.DataBaseSource;
                    break;
                case ShotgunProteomicsDB pdb:
                    proteomicsDB = pdb;
                    DBSource = pdb.DataBaseSource;
                    ProteomicsParameter = pdb.ProteomicsParameter;
                    break;
                case EadLipidDatabase ldb:
                    this.eadLipidDatabase = ldb;
                    DBSource = DataBaseSource.EadLipid;
                    break;
            }
            DataBaseID = database.Id;
            IsLoaded = true;
        }

        public string DataBasePath {
            get => dataBasePath;
            set => SetProperty(ref dataBasePath, value);
        }
        private string dataBasePath = string.Empty;

        public string DataBaseID {
            get => dataBaseID;
            set => SetProperty(ref dataBaseID, value);
        }
        private string dataBaseID = string.Empty;

        public DataBaseSource DBSource {
            get => dBSource;
            set => SetProperty(ref dBSource, value);
        }
        private DataBaseSource dBSource = DataBaseSource.None;

        public double MassRangeBegin {
            get => massRangeBegin;
            set => SetProperty(ref massRangeBegin, value);
        }
        private double massRangeBegin;

        public double MassRangeEnd {
            get => massRangeEnd;
            set => SetProperty(ref massRangeEnd, value);
        }
        private double massRangeEnd;

        public LipidQueryBean LipidQueryContainer { get; }

        public ProteomicsParameter ProteomicsParameter { get; }

        private readonly ParameterBase parameter;
        public TargetOmics TargetOmics => parameter.TargetOmics;

        public bool IsLoaded { get; }

        private readonly MoleculeDataBase metabolomicsDB = null;

        private readonly ShotgunProteomicsDB proteomicsDB = null;

        private readonly EadLipidDatabase eadLipidDatabase = null;

        public override string ToString() {
            return $"{DataBaseID}({DBSource})";
        }

        public IReferenceDataBase Create() {
            switch (DBSource) {
                case DataBaseSource.Msp:
                case DataBaseSource.Lbm:
                case DataBaseSource.Text:
                    return CreateMoleculeDataBase();
                case DataBaseSource.Fasta:
                    return CreatePorteomicsDB();
                case DataBaseSource.EadLipid:
                    return CreateEadLipidDatabase();
                default:
                    throw new NotSupportedException(DBSource.ToString());
            }
        }

        public MoleculeDataBase CreateMoleculeDataBase() {
            switch (DBSource) {
                case DataBaseSource.Msp:
                    return metabolomicsDB ?? LoadMspDataBase();
                case DataBaseSource.Lbm:
                    return metabolomicsDB ?? LoadLipidDataBase();
                case DataBaseSource.Text:
                    return metabolomicsDB ?? LoadTextDataBase();
                default:
                    return null;
            }
        }

        public ShotgunProteomicsDB CreatePorteomicsDB() {
            switch (DBSource) {
                case DataBaseSource.Fasta:
                    return proteomicsDB ?? new ShotgunProteomicsDB(DataBasePath, DataBaseID, ProteomicsParameter, this.parameter.ProjectFolderPath, this.parameter.Ms2MassRangeBegin, this.parameter.Ms2MassRangeEnd, this.parameter.CollistionType);
                default:
                    return null;
            }
        }

        public EadLipidDatabase CreateEadLipidDatabase() {
            switch (DBSource) {
                case DataBaseSource.EadLipid:
                    return eadLipidDatabase ?? new EadLipidDatabase(DataBaseID);
                default:
                    return null;
            }
        }

        private MoleculeDataBase LoadMspDataBase() {
            return new MoleculeDataBase(LibraryHandler.ReadMspLibrary(DataBasePath), DataBaseID, DataBaseSource.Msp, SourceType.MspDB);
        }

        private MoleculeDataBase LoadLipidDataBase() {
            return new MoleculeDataBase(LibraryHandler.ReadLipidMsLibrary(DataBasePath, parameter), DataBaseID, DataBaseSource.Lbm, SourceType.MspDB);
        }

        private MoleculeDataBase LoadTextDataBase() {
            var textdb = TextLibraryParser.TextLibraryReader(DataBasePath, out string error);
            if (!string.IsNullOrEmpty(error)) {
                throw new Exception(error);
            }
            return new MoleculeDataBase(textdb, DataBaseID, DataBaseSource.Text, SourceType.TextDB);
        }
    }
}
