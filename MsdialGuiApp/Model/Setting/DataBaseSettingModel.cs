using CompMs.Common.DataObj.Result;
using CompMs.Common.Enum;
using CompMs.Common.Parser;
using CompMs.Common.Query;
using CompMs.CommonMVVM;
using CompMs.MsdialCore.DataObj;
using CompMs.MsdialCore.Parameter;
using CompMs.MsdialCore.Utility;
using System;
using System.IO;

namespace CompMs.App.Msdial.Model.Setting
{
    public sealed class DataBaseSettingModel : BindableBase
    {
        private readonly MoleculeDataBase _metabolomicsDB = null;
        private readonly ShotgunProteomicsDB _proteomicsDB = null;
        private readonly EadLipidDatabase _eadLipidDatabase = null;
        private readonly ParameterBase _parameter;

        public DataBaseSettingModel(ParameterBase parameter) {
            _parameter = parameter ?? throw new ArgumentNullException(nameof(parameter));
            LipidQueryContainer = parameter.LipidQueryContainer;
            LipidQueryContainer.IonMode = parameter.ProjectParam.IonMode;

            ProteomicsParameter = parameter.ProteomicsParam ?? new ProteomicsParameter();
            IsLoaded = false;
        }

        public DataBaseSettingModel(ParameterBase parameter, IReferenceDataBase database) {
            _parameter = parameter ?? throw new ArgumentNullException(nameof(parameter));
            switch (database) {
                case MoleculeDataBase mdb:
                    _metabolomicsDB = mdb;
                    DBSource = mdb.DataBaseSource;
                    break;
                case ShotgunProteomicsDB pdb:
                    _proteomicsDB = pdb;
                    DBSource = pdb.DataBaseSource;
                    ProteomicsParameter = pdb.ProteomicsParameter;
                    break;
                case EadLipidDatabase ldb:
                    _eadLipidDatabase = ldb;
                    if (parameter.CollistionType == CollisionType.OAD) {
                        DBSource = DataBaseSource.OadLipid;
                    }
                    else {
                        DBSource = DataBaseSource.EieioLipid;
                    }
                    break;
            }
            DataBaseID = database.Id;
            IsLoaded = true;
        }

        public string DataBasePath {
            get => _dataBasePath;
            set => SetProperty(ref _dataBasePath, value);
        }
        private string _dataBasePath = string.Empty;

        public string DataBaseID {
            get => _dataBaseID;
            set => SetProperty(ref _dataBaseID, value);
        }
        private string _dataBaseID = string.Empty;

        public DataBaseSource DBSource {
            get => _dBSource;
            set => SetProperty(ref _dBSource, value);
        }
        private DataBaseSource _dBSource = DataBaseSource.None;

        public LipidQueryBean LipidQueryContainer { get; }

        public ProteomicsParameter ProteomicsParameter { get; }

        public TargetOmics TargetOmics => _parameter.TargetOmics;
        public CollisionType CollisionType => _parameter.CollistionType;

        public bool IsLoaded { get; }

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
                case DataBaseSource.EieioLipid:
                    return CreateEieioLipidDatabase();
                case DataBaseSource.OadLipid:
                    return CreateOadLipidDatabase();
                default:
                    throw new NotSupportedException(DBSource.ToString());
            }
        }

        public MoleculeDataBase CreateMoleculeDataBase() {
            switch (DBSource) {
                case DataBaseSource.Msp:
                    return _metabolomicsDB ?? LoadMspDataBase();
                case DataBaseSource.Lbm:
                    return _metabolomicsDB ?? LoadLipidDataBase();
                case DataBaseSource.Text:
                    return _metabolomicsDB ?? LoadTextDataBase();
                default:
                    return null;
            }
        }

        public ShotgunProteomicsDB CreatePorteomicsDB() {
            switch (DBSource) {
                case DataBaseSource.Fasta:
                    return _proteomicsDB ?? new ShotgunProteomicsDB(DataBasePath, DataBaseID, ProteomicsParameter, _parameter.ProjectFolderPath);
                default:
                    return null;
            }
        }

        public EadLipidDatabase CreateEieioLipidDatabase() {
            switch (DBSource) {
                case DataBaseSource.EieioLipid:
                    return _eadLipidDatabase ?? new EadLipidDatabase(Path.GetTempFileName(), DataBaseID, LipidDatabaseFormat.Dictionary, DataBaseSource.EieioLipid);
                default:
                    return null;
            }
        }

        public EadLipidDatabase CreateOadLipidDatabase() {
            switch (DBSource) {
                case DataBaseSource.OadLipid:
                    return _eadLipidDatabase ?? new EadLipidDatabase(Path.GetTempFileName(), DataBaseID, LipidDatabaseFormat.Dictionary, DataBaseSource.OadLipid);
                default:
                    return null;
            }
        }

        private MoleculeDataBase LoadMspDataBase() {
            return new MoleculeDataBase(LibraryHandler.ReadMspLibrary(DataBasePath), DataBaseID, DataBaseSource.Msp, SourceType.MspDB);
        }

        private MoleculeDataBase LoadLipidDataBase() {
            return new MoleculeDataBase(LibraryHandler.ReadLipidMsLibrary(DataBasePath, _parameter), DataBaseID, DataBaseSource.Lbm, SourceType.MspDB);
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
