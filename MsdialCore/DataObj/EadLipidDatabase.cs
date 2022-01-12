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
using System.Linq;

namespace CompMs.MsdialCore.DataObj
{
    public class EadLipidDatabase : IMatchResultRefer<MoleculeMsReference, MsScanMatchResult>, IDisposable
    {
        public EadLipidDatabase(string dbPath, string id) {
            if (string.IsNullOrEmpty(dbPath)) {
                throw new ArgumentNullException(nameof(dbPath));
            }

            Id = id;
            lipidGenerator = FacadeLipidSpectrumGenerator.Default;

            var connectionStringBuilder = new SQLiteConnectionStringBuilder
            {
                DataSource = dbPath,
            };

            connection = new SQLiteConnection(connectionStringBuilder.ToString());

            connection.Open();
            var command = connection.CreateCommand();
            command.CommandText = $"CREATE TABLE IF NOT EXISTS {ReferenceTableName} ({LipidReference.ReferenceColumnsDefine})";
            command.ExecuteNonQuery();

            command.CommandText = $"CREATE TABLE IF NOT EXISTS {SpectrumTableName} ({LipidReference.SpectrumColumnDefine})";
            command.ExecuteNonQuery();
        }

        private readonly string ReferenceTableName = "LipidReferenceTable";
        private readonly string SpectrumTableName = "ReferenceSpectrumTable";

        public string Id { get; }

        private readonly ILipidSpectrumGenerator lipidGenerator;

        private SQLiteConnection connection;

        private int scanId = 0;

        // TODO: Convert IMSScanProperty to MoleculeMsReference
        public MoleculeMsReference Generate(ILipid lipid, AdductIon adduct, MoleculeMsReference baseReference) {
            var reference = (MoleculeMsReference)lipid.GenerateSpectrum(lipidGenerator, adduct, baseReference);
            reference.ScanID = scanId++;
            return reference;
        }

        public void Register(IEnumerable<MoleculeMsReference> moleculeMsReferences) {
            var lipidReferences = moleculeMsReferences.Select(reference => new LipidReference(reference)).ToList();
            using (var transaction = connection.BeginTransaction()) {
                var command = connection?.CreateCommand();
                command.Transaction = transaction;

                var commandText = "INSERT OR IGNORE INTO {0} VALUES {1}";
                foreach (var reference in lipidReferences) {
                    command.CommandText = string.Format(commandText, ReferenceTableName, reference.ToReferenceValues());
                    command.ExecuteNonQuery();
                }

                foreach (var reference in lipidReferences) {
                    command.CommandText = string.Format(commandText, SpectrumTableName, reference.ToSpectrumValues(reference.ScanID));
                    command.ExecuteNonQuery();
                }

                transaction.Commit();
            }
        }

        string IMatchResultRefer<MoleculeMsReference, MsScanMatchResult>.Key => Id;

        public MoleculeMsReference Refer(MsScanMatchResult result) {
            var command = connection?.CreateCommand();
            command.CommandText = $"SELECT * FROM {ReferenceTableName} WHERE ScanID = {result.LibraryID}";
            var reader = command.ExecuteReader();
            if (!reader.Read()) {
                return null;
            }
            var reference = LipidReference.ParseLipidReference(reader);
            reader.Close();

            command.CommandText = $"SELECT * FROM {SpectrumTableName} WHERE ScanID = {result.LibraryID}";
            reader = command.ExecuteReader();
            reference.Spectrum.AddRange(LipidReference.ParseSpectrum(reader));

            return reference.ConvertToReference();
        }

        private bool disposedValue;

        protected virtual void Dispose(bool disposing) {
            if (!disposedValue) {
                if (disposing) {
                    connection.Dispose();
                    connection = null;
                }

                disposedValue = true;
            }
        }

        public void Dispose() {
            // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
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

            public static readonly string ReferenceColumns = "ScanID, PrecursorMz, RT, RI, Drift, Mz, IonMode, Name, Formula, Ontology, SMILES, InChIKey, AdductType, CollisionCrossSection, CompoundClass, Comment, CollisionEnergy, DatabaseID, Charge, MsLevel";

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
                "MsLevel INTEGER NOT NULL";

            // public string ToReferenceValues() => string.Join(",",
            //         ScanID,
            //         PrecursorMz,
            //         ChromXs.RT.Value,
            //         ChromXs.RI.Value,
            //         ChromXs.Drift.Value,
            //         ChromXs.Mz.Value,
            //         (int)IonMode,
            //         Name,
            //         Formula,
            //         Ontology,
            //         SMILES,
            //         InChIKey,
            //         AdductType,
            //         CollisionCrossSection,
            //         CompoundClass,
            //         Comment,
            //         CollisionEnergy,
            //         DatabaseID,
            //         Charge,
            //         MsLevel
            //     );

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
                $"{MsLevel})";

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
                    MsLevel = reader.GetInt32(19)
                };
                reference.ChromXs.RT.Value = reader.GetDouble(2);
                reference.ChromXs.RI.Value = reader.GetDouble(3);
                reference.ChromXs.Drift.Value = reader.GetDouble(4);
                reference.ChromXs.Mz.Value = reader.GetDouble(5);
                return reference;
            }

            public static readonly string SpectrumColumns = "ScanID, Mass, Intensity, Comment";

            public static readonly string SpectrumColumnDefine = "ScanID INTEGER NOT NULL," +
                "Mass REAL NOT NULL," +
                "Intensity REAL NOT NULL," +
                "Comment TEXT";

            public string ToSpectrumValues(int scanId) => string.Join(",", Spectrum.Select(peak => $"({scanId},{peak.Mass},{peak.Intensity},'{peak.Comment}')"));

            public static IEnumerable<SpectrumPeak> ParseSpectrum(SQLiteDataReader reader) {
                while (reader.Read()) {
                    yield return new SpectrumPeak(reader.GetDouble(1), reader.GetDouble(2), reader.GetString(3));
                }
            }
        }
    }
}
