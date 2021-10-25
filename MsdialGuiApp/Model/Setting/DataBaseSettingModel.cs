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

        public LipidQueryBean LipidQueryContainer {
            get => lipidQueryContainer;
            set => SetProperty(ref lipidQueryContainer, value);
        }
        private LipidQueryBean lipidQueryContainer;

        private readonly ParameterBase parameter;
        public TargetOmics TargetOmics => parameter.TargetOmics;

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
                default:
                    throw new NotSupportedException(DBSource.ToString());
            }
        }

        public MoleculeDataBase CreateMoleculeDataBase() {
            switch (DBSource) {
                case DataBaseSource.Msp:
                    return LoadMspDataBase();
                case DataBaseSource.Lbm:
                    return LoadLipidDataBase();
                case DataBaseSource.Text:
                    return LoadTextDataBase();
                default:
                    return null;
            }
        }

        public ShotgunProteomicsDB CreatePorteomicsDB() {
            switch (DBSource) {
                case DataBaseSource.Fasta:
                    return new ShotgunProteomicsDB(DataBasePath, DataBaseID, parameter.ProteomicsParam, parameter.MspSearchParam);
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
