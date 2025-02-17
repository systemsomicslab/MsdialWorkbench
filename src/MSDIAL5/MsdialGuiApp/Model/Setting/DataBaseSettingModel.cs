using CompMs.App.Msdial.Common;
using CompMs.App.Msdial.ViewModel.Service;
using CompMs.Common.DataObj.Result;
using CompMs.Common.Enum;
using CompMs.Common.Parser;
using CompMs.Common.Query;
using CompMs.CommonMVVM;
using CompMs.MsdialCore.DataObj;
using CompMs.MsdialCore.Parameter;
using CompMs.MsdialCore.Utility;
using Reactive.Bindings.Notifiers;
using System;
using System.IO;
using System.Linq;
using System.Reflection;

namespace CompMs.App.Msdial.Model.Setting
{
    public sealed class DataBaseSettingModel : BindableBase
    {
        private readonly MoleculeDataBase? _metabolomicsDB;
        private readonly ShotgunProteomicsDB? _proteomicsDB;
        private readonly EadLipidDatabase? _eadLipidDatabase;
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
            LipidQueryContainer = parameter.LipidQueryContainer;
            LipidQueryContainer.IonMode = parameter.ProjectParam.IonMode;
            ProteomicsParameter = parameter.ProteomicsParam;
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
                    switch (parameter.CollistionType) {
                        case CollisionType.OAD: DBSource = DataBaseSource.OadLipid; break;
                        case CollisionType.EIEIO: DBSource = DataBaseSource.EieioLipid; break;
                        case CollisionType.EID: DBSource = DataBaseSource.EidLipid; break;
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

        public bool TrySetLbmLibrary() {
            string mainDirectory = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            var lbmFiles = Directory.GetFiles(mainDirectory, "*." + SaveFileFormat.lbm + "?", SearchOption.TopDirectoryOnly);
            var lbmFile = lbmFiles.FirstOrDefault();
            if (lbmFile is not null) {
                DataBasePath = lbmFile;
            }
            return lbmFile is not null;
        }

        public IReferenceDataBase? Create() {
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
                case DataBaseSource.EidLipid:
                    return CreateEidLipidDatabase();
                default:
                    throw new NotSupportedException(DBSource.ToString());
            }
        }

        public MoleculeDataBase? CreateMoleculeDataBase() {
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

        public ShotgunProteomicsDB? CreatePorteomicsDB() {
            switch (DBSource) {
                case DataBaseSource.Fasta:
                    return _proteomicsDB ?? new ShotgunProteomicsDB(DataBasePath, DataBaseID, ProteomicsParameter, _parameter.ProjectFolderPath);
                default:
                    return null;
            }
        }

        public EadLipidDatabase? CreateEieioLipidDatabase() {
            switch (DBSource) {
                case DataBaseSource.EieioLipid:
                    return _eadLipidDatabase ?? new EadLipidDatabase(Path.GetTempFileName(), DataBaseID, LipidDatabaseFormat.Dictionary, DataBaseSource.EieioLipid);
                default:
                    return null;
            }
        }

        public EadLipidDatabase? CreateEidLipidDatabase() {
            switch (DBSource) {
                case DataBaseSource.EidLipid:
                    return _eadLipidDatabase ?? new EadLipidDatabase(Path.GetTempFileName(), DataBaseID, LipidDatabaseFormat.Dictionary, DataBaseSource.EidLipid);
                default:
                    return null;
            }
        }

        public EadLipidDatabase? CreateOadLipidDatabase() {
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

        /// <summary>
        /// Attempts to load a text database from the path specified in the DataBasePath property.
        /// </summary>
        /// <remarks>
        /// This method will attempt to load the database up to three times. If an IOException is thrown (for example, if the file is locked by another process),
        /// it will wait for one second and then try again. If it still can't load the database after three attempts, it will throw an exception.
        /// </remarks>
        /// <returns>
        /// A MoleculeDataBase object representing the loaded database, or null if the database could not be loaded and the user chose not to retry.
        /// </returns>
        /// <exception cref="System.IO.IOException">
        /// Thrown when the database file could not be accessed, such as when it is locked by another process or does not exist.
        /// </exception>
        /// <exception cref="System.Exception">
        /// Thrown when an error occurs while reading the database, or when the database could not be loaded after three attempts.
        /// </exception>
        private MoleculeDataBase? LoadTextDataBase() {
            const int maxAttempts = 3;

            for (int attempt = 0; attempt < maxAttempts; attempt++)
            {
                try
                {
                    string error;
                    var textdb = TextLibraryParser.TextLibraryReader(DataBasePath, out error);
                    if (!string.IsNullOrEmpty(error))
                    {
                        throw new Exception($"Error while reading the text database: {error}");
                    }
                    return new MoleculeDataBase(textdb, DataBaseID, DataBaseSource.Text, SourceType.TextDB);
                }
                catch (IOException)
                {
                    if (attempt == maxAttempts - 1)
                    {
                        throw;
                    }
                    else
                    {
                        var request = new ErrorMessageBoxRequest()
                        {
                            ButtonType = System.Windows.MessageBoxButton.OKCancel,
                            Content = "Unable to load the text database. The file might be in use by another process or it may not exist.",
                            Caption = "Unable to load the text database.",
                        };
                        MessageBroker.Default.Publish(request);
                        if (request.Result != System.Windows.MessageBoxResult.OK)
                        {
                            return null;
                        }
                    }
                }
                catch (Exception ex)
                {
                    if (attempt == maxAttempts - 1)
                    {
                        throw;
                    }
                    else
                    {
                        var request = new ErrorMessageBoxRequest()
                        {
                            ButtonType = System.Windows.MessageBoxButton.OKCancel,
                            Content = ex.Message,
                            Caption = "Unable to load the text database.",
                        };
                        MessageBroker.Default.Publish(request);
                        if (request.Result != System.Windows.MessageBoxResult.OK)
                        {
                            return null;
                        }
                    }
                }
            }

            throw new Exception("Failed to load the text database after multiple attempts. Please check the file path and ensure the file is not in use by another process.");
        }
    }
}
