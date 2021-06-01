using CompMs.Common.Components;
using CompMs.Common.DataObj.Result;
using CompMs.MsdialCore.Algorithm.Annotation;
using CompMs.MsdialCore.Parser;
using MessagePack;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace CompMs.MsdialCore.DataObj
{
    [MessagePackObject]
    public class DataBaseMapper : IMatchResultRefer
    {
        [IgnoreMember]
        public ReadOnlyDictionary<string, IMatchResultRefer> KeyToRefer {
            get {
                return new ReadOnlyDictionary<string, IMatchResultRefer>(keyToRefer);
            }
        }

        private Dictionary<string, IMatchResultRefer> keyToRefer = new Dictionary<string, IMatchResultRefer>();

        public void Restore(IRestorationVisitor visitor) {
            keyToRefer = InnerKeyToRestorationKey.ToDictionary(kvp => kvp.Key, kvp => kvp.Value.Accept(visitor));
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


        public void Add(string SourceKey, IReferRestorationKey restorationKey) {
            InnerKeyToRestorationKey[SourceKey] = restorationKey;
        }

        public void Remove(string sourceKey) {
            InnerKeyToRestorationKey.Remove(sourceKey);
        }

        public MoleculeMsReference Refer(MsScanMatchResult result) {
            if (result?.SourceKey != null && KeyToRefer.TryGetValue(result.SourceKey, out var refer)) {
                return refer.Refer(result);
            }
            return null;
        }
    }
}