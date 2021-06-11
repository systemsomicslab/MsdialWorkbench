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
            keyToRefer.Clear();
            using (var archive = new ZipArchive(stream, ZipArchiveMode.Read, leaveOpen: true)) {
                foreach (var kvp in InnerKeyToRestorationKey) {
                    var entry = archive.GetEntry(kvp.Key);
                    using (var entry_stream = entry.Open()) {
                        keyToRefer[kvp.Key] = kvp.Value.Accept(visitor, entry_stream);
                    }
                }
            }
        }

        public void Save(Stream stream) {
            InnerKeyToRestorationKey.Clear();
            using (var archive = new ZipArchive(stream, ZipArchiveMode.Update, leaveOpen: true)) {
                foreach (var refer in keyToRefer.Values.OfType<IRestorableRefer>()) {
                    var entry = archive.CreateEntry(refer.Key, CompressionLevel.Optimal);
                    using (var entry_stream = entry.Open()) {
                        InnerKeyToRestorationKey[refer.Key] = refer.Save(entry_stream);
                    }
                }
            }
        }

        [Key(0)]
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