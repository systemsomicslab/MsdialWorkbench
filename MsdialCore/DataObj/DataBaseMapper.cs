using CompMs.Common.Components;
using CompMs.Common.DataObj.Result;
using CompMs.MsdialCore.Algorithm.Annotation;
using CompMs.MsdialCore.Parser;
using MessagePack;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.IO.Compression;
using System.Linq;

namespace CompMs.MsdialCore.DataObj
{
    [MessagePackObject]
    public class DataBaseMapper : IMatchResultRefer
    {
        public DataBaseMapper() {
            Databases = new List<MoleculeDataBase>();
        }

        public DataBaseMapper(List<MoleculeDataBase> databases) {
            Databases = databases;
        }

        [IgnoreMember]
        string IMatchResultRefer.Key { get; } = string.Empty;

        [IgnoreMember]
        public ReadOnlyDictionary<string, IMatchResultRefer> KeyToRefer {
            get {
                return new ReadOnlyDictionary<string, IMatchResultRefer>(keyToRefer);
            }
        }

        private Dictionary<string, IMatchResultRefer> keyToRefer = new Dictionary<string, IMatchResultRefer>();

        public void Restore(IRestorationVisitor visitor, Stream stream) {
            using (var archive = new ZipArchive(stream, ZipArchiveMode.Read, leaveOpen: true)) {
                foreach (var db in Databases) {
                    var entry = archive.GetEntry(db.Id);
                    using (var entry_stream = entry.Open()) {
                        db.Load(entry_stream);
                    }
                }
            }

            foreach (var kvp in InnerKeyToRestorationKey) {
                var database = Databases.FirstOrDefault(db => db.Id == kvp.Key);
                if (database is null) {
                    continue;
                }
                keyToRefer[kvp.Key] = kvp.Value.Accept(visitor, database);
            }
        }

        public void Save(Stream stream) {
            using (var archive = new ZipArchive(stream, ZipArchiveMode.Update, leaveOpen: true)) {
                foreach (var db in Databases) {
                    var entry = archive.CreateEntry(db.Id, CompressionLevel.Optimal);
                    using (var entry_stream = entry.Open()) {
                        db.Save(entry_stream);
                    }
                }
            }

            foreach (var kvp in KeyToRefer) {
                if (kvp.Value is IRestorableRefer refer) {
                    InnerKeyToRestorationKey[refer.Key] = refer.Save();
                }
            }
        }

        [Key(1)]
        // Should not use setter.
        public List<MoleculeDataBase> Databases { get; set; }

        [Key(0)]
        // Should not use setter.
        public Dictionary<string, IReferRestorationKey> KeyToRestorationKey { get; set; }

        private Dictionary<string, IReferRestorationKey> InnerKeyToRestorationKey {
            get {
                if (KeyToRestorationKey == null) {
                    KeyToRestorationKey = new Dictionary<string, IReferRestorationKey>();
                }
                return KeyToRestorationKey;
            }
        }

        public void Add(IMatchResultRefer refer) {
            keyToRefer[refer.Key] = refer;
        }

        public void Remove(string sourceKey) {
            keyToRefer.Remove(sourceKey);
        }

        public MoleculeMsReference Refer(MsScanMatchResult result) {
            if (result?.SourceKey != null && KeyToRefer.TryGetValue(result.SourceKey, out var refer)) {
                return refer.Refer(result);
            }
            return null;
        }
    }
}