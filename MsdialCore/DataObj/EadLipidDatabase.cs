using CompMs.Common.Components;
using CompMs.Common.DataObj.Property;
using CompMs.Common.DataObj.Result;
using CompMs.Common.Enum;
using CompMs.Common.FormulaGenerator.Parser;
using CompMs.Common.Lipidomics;
using CompMs.MsdialCore.Algorithm.Annotation;
using System;
using System.Collections.Generic;
using System.Data.SQLite;
using System.IO;
using System.Linq;
using System.Threading;

namespace CompMs.MsdialCore.DataObj
{
    [MessagePack.MessagePackObject]
    public sealed class EadLipidDatabase : IMatchResultRefer<MoleculeMsReference, MsScanMatchResult>, IReferenceDataBase, IDisposable
    {
        public EadLipidDatabase(string id) : this(Path.GetTempFileName(), id) {

        }

        public EadLipidDatabase(string dbPath, string id) {
            if (string.IsNullOrEmpty(dbPath)) {
                throw new ArgumentNullException(nameof(dbPath));
            }

            this.dbPath = dbPath;
            Id = id;
            lipidGenerator = FacadeLipidSpectrumGenerator.Default;
            cache = new HashSet<int>();

            connection = CreateConnection(dbPath);
            var command = connection.CreateCommand();
            command.CommandText = $"CREATE TABLE IF NOT EXISTS {ReferenceTableName} ({LipidReference.ReferenceColumnsDefine})";
            command.ExecuteNonQuery();

            command.CommandText = $"CREATE TABLE IF NOT EXISTS {SpectrumTableName} ({LipidReference.SpectrumColumnDefine})";
            command.ExecuteNonQuery();
        }

        private readonly string ReferenceTableName = "LipidReferenceTable";
        private readonly string SpectrumTableName = "ReferenceSpectrumTable";

        [MessagePack.Key(nameof(Id))]
        public string Id { get; }

        private readonly string dbPath;

        private readonly ILipidSpectrumGenerator lipidGenerator;
        private readonly HashSet<int> cache;
        private SQLiteConnection connection;

        private int scanId = 0;
        private int spectrumId = 0;

        private static SQLiteConnection CreateConnection(string dbPath) {
            var connectionStringBuilder = new SQLiteConnectionStringBuilder
            {
                DataSource = dbPath,
            };

            var connection = new SQLiteConnection(connectionStringBuilder.ToString());
            connection.Open();
            return connection;
        }

        private static void CloseConnection(SQLiteConnection connection) {
            connection.Dispose();
            GC.Collect();
            GC.WaitForPendingFinalizers();
        }

        public MoleculeMsReference Generate(ILipid lipid, AdductIon adduct, MoleculeMsReference baseReference) {
            if (connection is null) {
                throw new ObjectDisposedException(nameof(connection));
            }
            if (!lipidGenerator.CanGenerate(lipid, adduct)) {
                return null;
            }
            if (cache.Contains(lipid.Name.GetHashCode())) {
                var command = connection.CreateCommand();
                command.CommandText = $"SELECT * FROM {ReferenceTableName} WHERE Name = '{lipid.Name}' AND AdductType = '{adduct.AdductIonName}'";
                var reader = command.ExecuteReader();
                if (reader.Read()) {
                    var reference = LipidReference.ParseLipidReference(reader);
                    reader.Close();

                    command.CommandText = $"SELECT * FROM {SpectrumTableName} WHERE SpectrumId BETWEEN {reference.SpectrumIdFrom} AND {reference.SpectrumIdTo}";
                    reader = command.ExecuteReader();
                    reference.Spectrum.AddRange(LipidReference.ParseSpectrum(reader));
                    return reference.ConvertToReference();
                }
            }
            var reference_ = lipid.GenerateSpectrum(lipidGenerator, adduct, baseReference) as MoleculeMsReference;
            if (reference_ != null) {
                reference_.ScanID = scanId++;
            }
            return reference_;
        }

        public void Register(IEnumerable<MoleculeMsReference> moleculeMsReferences) {
            if (connection is null) {
                throw new ObjectDisposedException(nameof(connection));
            }
            var lipidReferences = moleculeMsReferences.Where(reference => reference != null).Select(reference => new LipidReference(reference)).ToList();
            using (var transaction = connection.BeginTransaction()) {
                var command = connection?.CreateCommand();
                command.Transaction = transaction;

                var commandText = "INSERT OR IGNORE INTO {0} VALUES {1}";
                foreach (var reference in lipidReferences) {
                    var prev = spectrumId;
                    spectrumId += reference.Spectrum.Count;
                    reference.SetSpectrumId(prev, spectrumId - 1);
                    command.CommandText = string.Format(commandText, SpectrumTableName, reference.ToSpectrumValues());
                    command.ExecuteNonQuery();
                }

                foreach (var reference in lipidReferences) {
                    command.CommandText = string.Format(commandText, ReferenceTableName, reference.ToReferenceValues());
                    command.ExecuteNonQuery();
                }

                transaction.Commit();
            }
            cache.UnionWith(lipidReferences.Select(r => r.Name.GetHashCode()));
        }

        string IMatchResultRefer<MoleculeMsReference, MsScanMatchResult>.Key => Id;

        public MoleculeMsReference Refer(MsScanMatchResult result) {
            if (!string.IsNullOrEmpty(result.Name) && !cache.Contains(result.Name.GetHashCode())) {
                return null;
            }
            var command = connection?.CreateCommand();
            command.CommandText = $"SELECT * FROM {ReferenceTableName} WHERE ScanID = {result.LibraryID}";
            var reader = command.ExecuteReader();
            if (!reader.Read()) {
                return null;
            }
            var reference = LipidReference.ParseLipidReference(reader);
            reader.Close();

            command.CommandText = $"SELECT * FROM {SpectrumTableName} WHERE SpectrumId BETWEEN {reference.SpectrumIdFrom} AND {reference.SpectrumIdTo}";
            reader = command.ExecuteReader();
            reference.Spectrum.AddRange(LipidReference.ParseSpectrum(reader));

            return reference.ConvertToReference();
        }

        private bool disposedValue;

        private void Dispose(bool disposing) {
            if (!disposedValue) {
                if (disposing) {

                }

                CloseConnection(connection);
                connection = null;
                try {
                    Retry(5, TimeSpan.FromMilliseconds(500), () =>
                    {
                        if (File.Exists(dbPath)) {
                            File.Delete(dbPath);
                        }
                    });
                }
                catch (IOException) {

                }

                disposedValue = true;
            }
        }

        public void Dispose() {
            // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }

        // TODO: override finalizer only if 'Dispose(bool disposing)' has code to free unmanaged resources
        ~EadLipidDatabase()
        {
            // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
            Dispose(disposing: false);
        }

        public void Save(Stream stream) {
            CloseConnection(connection);
            Retry(5, TimeSpan.FromMilliseconds(500), () =>
            {
                using (var fs = File.Open(dbPath, FileMode.Open)) {
                    fs.CopyTo(stream);
                }
            });
            connection = CreateConnection(dbPath);
        }

        public void Load(Stream stream, string folderpath) {
            CloseConnection(connection);
            Retry(5, TimeSpan.FromMilliseconds(500), () =>
            {
                using (var fs = File.Open(dbPath, FileMode.Create)) {
                    stream.CopyTo(fs);
                }
            });
            connection = CreateConnection(dbPath);

            using (var command = connection.CreateCommand()) {
                command.CommandText = $"SELECT Name FROM {ReferenceTableName}";
                using (var reader = command.ExecuteReader()) {
                    while (reader.Read()) {
                        cache.Add(reader.GetString(0).GetHashCode());
                    }
                    reader.Close();
                }
                command.CommandText = $"SELECT MAX(ScanID) From {ReferenceTableName}";
                scanId = 1 + (int)command.ExecuteScalar();
                command.CommandText = $"SELECT MAX(SpectrumID) From {SpectrumTableName}";
                spectrumId = 1 + (int)command.ExecuteScalar();
            }
        }

        private static void Retry(int retryCount, TimeSpan wait, Action action) {
            var i = 0;
            while (true) {
                try {
                    action();
                }
                catch (IOException) {
                    if (i++ >= retryCount) {
                        throw;
                    }
                    Thread.Sleep(wait);
                    continue;
                }
                break;
            }
        }

        class LipidReference
        {
            public int ScanID { get; private set; }
            public double PrecursorMz { get; private set; }
            public ChromXs ChromXs { get; private set; }
            public IonMode IonMode { get; private set; }
            public List<SpectrumPeak> Spectrum { get; private set; }
            public string Name { get; private set; }
            public string Formula { get; private set; }
            public string Ontology { get; private set; }
            public string SMILES { get; private set; }
            public string InChIKey { get; private set; }
            public string AdductType { get; private set; }
            public double CollisionCrossSection { get; private set; }
            public string CompoundClass { get; private set; }
            public string Comment { get; private set; }
            public float CollisionEnergy { get; private set; }
            public int DatabaseID { get; private set; }
            public int Charge { get; private set; }
            public int MsLevel { get; private set; }

            public int SpectrumIdFrom { get; private set; }
            public int SpectrumIdTo { get; private set; }

            public LipidReference(MoleculeMsReference reference) {
                ScanID = reference.ScanID;
                PrecursorMz = reference.PrecursorMz;
                ChromXs = reference.ChromXs;
                IonMode = reference.IonMode;
                Spectrum = reference.Spectrum;
                Name = reference.Name;
                Formula = reference.Formula.FormulaString;
                Ontology = reference.Ontology;
                SMILES = reference.SMILES;
                InChIKey = reference.InChIKey;
                AdductType = reference.AdductType.AdductIonName;
                CollisionCrossSection = reference.CollisionCrossSection;
                CompoundClass = reference.CompoundClass;
                Comment = reference.Comment;
                CollisionEnergy = reference.CollisionEnergy;
                DatabaseID = reference.DatabaseID;
                Charge = reference.Charge;
                MsLevel = reference.MsLevel;
                SpectrumIdFrom = -1;
                SpectrumIdTo = -1;
            }

            private LipidReference() {

            }

            public MoleculeMsReference ConvertToReference() {
                return new MoleculeMsReference
                {
                    ScanID = ScanID,
                    PrecursorMz = PrecursorMz,
                    ChromXs = ChromXs,
                    IonMode = IonMode,
                    Spectrum = Spectrum,
                    Name = Name,
                    Formula = FormulaStringParcer.Convert2FormulaObjV2(Formula),
                    Ontology = Ontology,
                    SMILES = SMILES,
                    InChIKey = InChIKey,
                    AdductType = AdductIon.GetAdductIon(AdductType),
                    CollisionCrossSection = CollisionCrossSection,
                    CompoundClass = CompoundClass,
                    Comment = Comment,
                    CollisionEnergy = CollisionEnergy,
                    DatabaseID = DatabaseID,
                    Charge = Charge,
                    MsLevel = MsLevel,
                };
            }

            public static readonly string ReferenceColumns = "ScanID, PrecursorMz, RT, RI, Drift, Mz, IonMode, Name, Formula, Ontology, SMILES, InChIKey, AdductType, CollisionCrossSection, CompoundClass, Comment, CollisionEnergy, DatabaseID, Charge, MsLevel, SpectrumIdFrom, SpectrumIdTo";

            public static readonly string ReferenceColumnsDefine = "ScanID INTEGER NOT NULL PRIMARY KEY," +
                "PrecursorMz REAL NOT NULL," +
                "RT REAL NOT NULL," +
                "RI REAL NOT NULL," +
                "Drift REAL NOT NULL," +
                "Mz REAL NOT NULL," +
                "IonMode INTEGER NOT NULL," +
                "Name TEXT," +
                "Formula TEXT," +
                "Ontology TEXT," +
                "SMILES TEXT," +
                "InChIKey TEXT," +
                "AdductType TEXT," +
                "CollisionCrossSection REAL NOT NULL," +
                "CompoundClass TEXT," +
                "Comment TEXT," +
                "CollisionEnergy REAL NOT NULL," +
                "DatabaseID INTEGER NOT NULL," +
                "Charge INTEGER NOT NULL," +
                "MsLevel INTEGER NOT NULL," +
                "SpectrumIdFrom INTEGER NOT NULL," +
                "SpectrumIdTo INTEGER NOT NULL";

            public static readonly int ReferenceNameColumn = 7;

            public string ToReferenceValues() => $"({ScanID}," +
                $"{PrecursorMz}," +
                $"{ChromXs.RT.Value}," +
                $"{ChromXs.RI.Value}," +
                $"{ChromXs.Drift.Value}," +
                $"{ChromXs.Mz.Value}," +
                $"{(int)IonMode}," +
                $"'{Name}'," +
                $"'{Formula}'," +
                $"'{Ontology}'," +
                $"'{SMILES}'," +
                $"'{InChIKey}'," +
                $"'{AdductType}'," +
                $"{CollisionCrossSection}," +
                $"'{CompoundClass}'," +
                $"'{Comment}'," +
                $"{CollisionEnergy}," +
                $"{DatabaseID}," +
                $"{Charge}," +
                $"{MsLevel}," +
                $"{SpectrumIdFrom}," +
                $"{SpectrumIdTo})";

            public static LipidReference ParseLipidReference(SQLiteDataReader reader) {
                var reference = new LipidReference
                {
                    ScanID = reader.GetInt32(0),
                    PrecursorMz = reader.GetDouble(1),
                    ChromXs = new ChromXs(),
                    IonMode = (IonMode)reader.GetInt32(6),
                    Spectrum = new List<SpectrumPeak>(),
                    Name = reader.GetString(7),
                    Formula = reader.GetString(8),
                    Ontology = reader.GetString(9),
                    SMILES = reader.GetString(10),
                    InChIKey = reader.GetString(11),
                    AdductType = reader.GetString(12),
                    CollisionCrossSection = reader.GetDouble(13),
                    CompoundClass = reader.GetString(14),
                    Comment = reader.GetString(15),
                    CollisionEnergy = (float)reader.GetDouble(16),
                    DatabaseID = reader.GetInt32(17),
                    Charge = reader.GetInt32(18),
                    MsLevel = reader.GetInt32(19),
                    SpectrumIdFrom = reader.GetInt32(20),
                    SpectrumIdTo = reader.GetInt32(21),
                };
                reference.ChromXs.RT.Value = reader.GetDouble(2);
                reference.ChromXs.RI.Value = reader.GetDouble(3);
                reference.ChromXs.Drift.Value = reader.GetDouble(4);
                reference.ChromXs.Mz.Value = reader.GetDouble(5);
                return reference;
            }

            public void SetSpectrumId(int from, int to) {
                SpectrumIdFrom = from;
                SpectrumIdTo = to;
            }

            public static readonly string SpectrumColumns = "SpectrumID, ScanID, Mass, Intensity, Comment";

            public static readonly string SpectrumColumnDefine = "SpectrumID INTEGER NOT NULL PRIMARY KEY," +
                "ScanID INTEGER NOT NULL," +
                "Mass REAL NOT NULL," +
                "Intensity REAL NOT NULL," +
                "Comment TEXT";

            public string ToSpectrumValues() {
                return string.Join(",", Spectrum.Select((peak, i) => $"({i + SpectrumIdFrom},{ScanID},{peak.Mass},{peak.Intensity},'{peak.Comment}')"));
            }

            public static IEnumerable<SpectrumPeak> ParseSpectrum(SQLiteDataReader reader) {
                while (reader.Read()) {
                    yield return new SpectrumPeak(reader.GetDouble(2), reader.GetDouble(3), reader.GetString(4));
                }
            }
        }
    }
}
