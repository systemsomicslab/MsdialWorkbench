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
    internal sealed class EadLipidSqliteDatabase : ILipidDatabase
    {
        public EadLipidSqliteDatabase(string id) : this(Path.GetTempFileName(), id) {

        }

        public EadLipidSqliteDatabase(string dbPath, string id) {
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

        private static readonly LipidGenerator ShortHandGenerator = new LipidGenerator(new ShortHandGenerator());

        public List<MoleculeMsReference> Generates(IEnumerable<ILipid> lipids, ILipid seed, AdductIon adduct, MoleculeMsReference baseReference) {
            if (connection is null) {
                throw new ObjectDisposedException(nameof(connection));
            }
            var shortLipid = seed.Generate(ShortHandGenerator).SingleOrDefault();
            if (shortLipid is null) {
                return new List<MoleculeMsReference>();
            }
            var references = new List<MoleculeMsReference>();
            if (cache.Contains(shortLipid.Name.GetHashCode())) {
                var descendantsList = GetDescendant(shortLipid, adduct).ToList();
                var descendants = descendantsList.ToDictionary(reference => reference.Name, reference => reference);
                var needToConverts = new Dictionary<int, LipidReference>();
                var needToRegisters = new List<ILipid>();
                foreach (var lipid in lipids) {
                    if (descendants.TryGetValue(lipid.Name, out var reference)) {
                        needToConverts.Add(reference.ScanID, reference);
                    }
                    else {
                        needToRegisters.Add(lipid);
                    }
                }
                references.AddRange(BatchConvert(needToConverts));
                var needToRegisterReferences = needToRegisters.Select(lipid => GenerateReference(lipid, adduct, baseReference)).Where(n => n != null).ToArray();
                Register(needToRegisterReferences, shortLipid);
                references.AddRange(needToRegisterReferences);
            }
            else {
                var needToRegisterReferences = lipids.Select(lipid => GenerateReference(lipid, adduct, baseReference)).Where(n => n != null).ToArray();
                Register(needToRegisterReferences, shortLipid);
                references.AddRange(needToRegisterReferences);
            }
            return references;
        }

        private IEnumerable<LipidReference> GetDescendant(ILipid ancestor, AdductIon adduct) {
            var command = connection.CreateCommand();
            command.CommandText = $"SELECT * FROM {ReferenceTableName} WHERE ShortName = '{ancestor.Name}' AND AdductType = '{adduct.AdductIonName}'";
            var reader = command.ExecuteReader();
            while (reader.Read()) {
                yield return LipidReference.ParseLipidReference(reader);
            }
        }

        private MoleculeMsReference GenerateReference(ILipid lipid, AdductIon adduct, MoleculeMsReference baseReference) {
            if (!lipidGenerator.CanGenerate(lipid, adduct)) {
                return null;
            }
            var reference = lipid.GenerateSpectrum(lipidGenerator, adduct, baseReference) as MoleculeMsReference;
            if (!(reference is null)) {
                reference.ScanID = scanId++;
            }
            return reference;
        }

        private IEnumerable<MoleculeMsReference> BatchConvert(Dictionary<int, LipidReference> lipidReferences) {
            if (lipidReferences.Count == 0) {
                yield break;
            }
            var command = connection.CreateCommand();
            command.CommandText = $"SELECT * FROM {SpectrumTableName} WHERE ScanID IN ({string.Join(",", lipidReferences.Values.Select(reference => $"'{reference.ScanID}'"))})";
            var reader = command.ExecuteReader();
            foreach (var group in LipidReference.ParseSpectrumAndId(reader).GroupBy(pair => pair.Item2, pair => pair.Item1)) {
                lipidReferences[group.Key].Spectrum.AddRange(group);
                yield return lipidReferences[group.Key].ConvertToReference();
            }
        }

        private readonly object check = new object();

        private void Register(IEnumerable<MoleculeMsReference> moleculeMsReferences, ILipid seed) {
            if (connection is null) {
                throw new ObjectDisposedException(nameof(connection));
            }

            var lipidReferences = moleculeMsReferences.Where(reference => reference != null).Select(reference => new LipidReference(reference)).ToList();

            lock (check) {
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
                        command.CommandText = string.Format(commandText, ReferenceTableName, reference.ToReferenceValues(seed.Name));
                        command.ExecuteNonQuery();
                    }

                    transaction.Commit();
                }
            }
            cache.Add(seed.Name.GetHashCode());
            // cache.UnionWith(lipidReferences.Select(r => r.Name.GetHashCode()));
        }

        string IMatchResultRefer<MoleculeMsReference, MsScanMatchResult>.Key => Id;

        public MoleculeMsReference Refer(MsScanMatchResult result) {
            //if (!string.IsNullOrEmpty(result.Name) && !cache.Contains(result.Name.GetHashCode())) {
            //    return null;
            //}
            if (result.LibraryID < 0 || result.LibraryID >= scanId) return null;

            lock (check) {
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
        }

        // ILipidDatabase
        List<MoleculeMsReference> ILipidDatabase.GetReferences() {
            var command = connection.CreateCommand();
            command.CommandText = $"SELECT * FROM {ReferenceTableName}";
            var reader = command.ExecuteReader();
            var lipidReferenceDictionary = new Dictionary<int, LipidReference>();
            while (reader.Read()) {
                var lipidReference = LipidReference.ParseLipidReference(reader);
                lipidReferenceDictionary[lipidReference.ScanID] = lipidReference;
            }
            command = connection.CreateCommand();
            command.CommandText = $"SELECT * FROM {SpectrumTableName}";
            reader = command.ExecuteReader();
            var result = new List<MoleculeMsReference>();
            foreach (var group in LipidReference.ParseSpectrumAndId(reader).GroupBy(pair => pair.Item2, pair => pair.Item1)) {
                lipidReferenceDictionary[group.Key].Spectrum.AddRange(group);
                result.Add(lipidReferenceDictionary[group.Key].ConvertToReference());
            }
            return result;
        }

        void ILipidDatabase.SetReferences(IEnumerable<MoleculeMsReference> references) {
            foreach (var reference in references) {
                var lipid = FacadeLipidParser.Default.Parse(reference.Name);
                Register(new[] { reference, }, lipid);
            }
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
        ~EadLipidSqliteDatabase()
        {
            // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
            Dispose(disposing: false);
        }

        public void Save(Stream stream, bool forceSerialize = false) {
            CloseConnection(connection);
            Retry(5, TimeSpan.FromMilliseconds(500), () =>
            {
                if (dbPath == ":memory:") {
                    return;
                }
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
                if (dbPath == ":memory:") {
                    return;
                }
                using (var fs = File.Open(dbPath, FileMode.Create)) {
                    stream.CopyTo(fs);
                }
            });
            connection = CreateConnection(dbPath);

            using (var command = connection.CreateCommand()) {
                command.CommandText = $"PRAGMA table_info('{ReferenceTableName}');";
                var shortNameExists = false;
                using (var reader = command.ExecuteReader()) {
                    while (reader.Read()) {
                        if (reader.GetString(1) == "ShortName") {
                            shortNameExists = true;
                            break;
                        }
                    }
                }
                if (shortNameExists) {
                    command.CommandText = $"SELECT ShortName FROM {ReferenceTableName}";
                    using (var reader = command.ExecuteReader()) {
                        while (reader.Read()) {
                            cache.Add(reader.GetString(0).GetHashCode());
                        }
                        reader.Close();
                    }
                }
                else {
                    command.CommandText = $"ALTER TABLE '{ReferenceTableName}' ADD COLUMN ShortName TEXT;";
                    command.ExecuteNonQuery();
                }
                command.CommandText = $"SELECT MAX(ScanID) FROM {ReferenceTableName}";
                try {
                    scanId = 1 + int.Parse(command.ExecuteScalar().ToString());
                }
                catch (FormatException){
                    scanId = 0;
                }
                command.CommandText = $"SELECT MAX(SpectrumID) FROM {SpectrumTableName}";
                try {
                    spectrumId = 1 + int.Parse(command.ExecuteScalar().ToString());
                }
                catch (FormatException) {
                    spectrumId = 0;
                }
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

            public static readonly string ReferenceColumns = "ScanID, PrecursorMz, RT, RI, Drift, Mz, IonMode, Name, Formula, Ontology, SMILES, InChIKey, AdductType, CollisionCrossSection, CompoundClass, Comment, CollisionEnergy, DatabaseID, Charge, MsLevel, SpectrumIdFrom, SpectrumIdTo, ShortName";

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
                "SpectrumIdTo INTEGER NOT NULL," +
                "ShortName TEXT";

            public static readonly int ReferenceNameColumn = 7;

            public string ToReferenceValues(string shortName) => $"({ScanID}," +
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
                $"{SpectrumIdTo}," +
                $"'{shortName ?? Name}')";

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
                reference.ChromXs.RT = new RetentionTime(reader.GetDouble(2), unit: ChromXUnit.Min);
                reference.ChromXs.RI = new RetentionIndex(reader.GetDouble(3), unit: ChromXUnit.None);
                reference.ChromXs.Drift = new DriftTime(reader.GetDouble(4), unit: ChromXUnit.Msec);
                reference.ChromXs.Mz = new MzValue(reader.GetDouble(5), unit: ChromXUnit.Mz);
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
                "Comment TEXT," +
                "Resolution REAL," +
                "Charge INTEGER," +
                "IsotopeFrag INTEGER," +
                "PeakQuality INTEGER," +
                "PeakID INTEGER," +
                "IsotopeParentPeakID INTEGER," +
                "IsotopeWeightNumber INTEGER," +
                "IsMatched INTEGER," +
                "SpectrumComment INTEGER," +
                "IsAbsolutelyRequiredFragmentForAnnotation INTEGER";

            public string ToSpectrumValues() {
                return string.Join(",", Spectrum.Select((peak, i) => $"(" +
                $"{i + SpectrumIdFrom}," +
                $"{ScanID}," +
                $"{peak.Mass}," +
                $"{peak.Intensity}," +
                $"'{peak.Comment}'," +
                $"{peak.Resolution}," +
                $"{peak.Charge}," +
                $"{Convert.ToInt32(peak.IsotopeFrag)}," +
                $"{(int)peak.PeakQuality}," +
                $"{peak.PeakID}," +
                $"{peak.IsotopeParentPeakID}," +
                $"{peak.IsotopeWeightNumber}," +
                $"{Convert.ToInt32(peak.IsMatched)}," +
                $"{(int)peak.SpectrumComment}," +
                $"{peak.IsAbsolutelyRequiredFragmentForAnnotation})"));
            }

            public static IEnumerable<SpectrumPeak> ParseSpectrum(SQLiteDataReader reader) {
                while (reader.Read()) {
                    yield return new SpectrumPeak(reader.GetDouble(2), reader.GetDouble(3), reader.GetString(4))
                    {
                        Resolution = reader.GetDouble(5),
                        Charge = reader.GetInt32(6),
                        IsotopeFrag = reader.GetBoolean(7),
                        PeakQuality = (PeakQuality)reader.GetInt32(8),
                        PeakID = reader.GetInt32(9),
                        IsotopeParentPeakID = reader.GetInt32(10),
                        IsotopeWeightNumber = reader.GetInt32(11),
                        IsMatched = reader.GetBoolean(12),
                        SpectrumComment = (SpectrumComment)reader.GetInt32(13),
                        IsAbsolutelyRequiredFragmentForAnnotation = reader.GetBoolean(14),
                    };
                }
            }

            public static IEnumerable<(SpectrumPeak, int)> ParseSpectrumAndId(SQLiteDataReader reader) {
                while (reader.Read()) {
                    yield return (new SpectrumPeak(reader.GetDouble(2), reader.GetDouble(3), reader.GetString(4))
                        {
                            Resolution = reader.GetDouble(5),
                            Charge = reader.GetInt32(6),
                            IsotopeFrag = reader.GetBoolean(7),
                            PeakQuality = (PeakQuality)reader.GetInt32(8),
                            PeakID = reader.GetInt32(9),
                            IsotopeParentPeakID = reader.GetInt32(10),
                            IsotopeWeightNumber = reader.GetInt32(11),
                            IsMatched = reader.GetBoolean(12),
                            SpectrumComment = (SpectrumComment)reader.GetInt32(13),
                            IsAbsolutelyRequiredFragmentForAnnotation = reader.GetBoolean(14),
                        },
                        reader.GetInt32(1));
                }
            }
        }
    }
}
