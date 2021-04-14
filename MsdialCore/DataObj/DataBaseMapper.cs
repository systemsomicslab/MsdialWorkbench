using CompMs.Common.Components;
using CompMs.Common.DataObj.Result;
using CompMs.MsdialCore.Algorithm.Annotation;
using CompMs.MsdialCore.Parameter;
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
                return new ReadOnlyDictionary<string, IMatchResultRefer>(cacheKeyToRefer);
            }
        }

        private Dictionary<string, IMatchResultRefer> cacheKeyToRefer;

        public void Restore(ParameterBase param) {
            cacheKeyToRefer = KeyToRestorationKey.ToDictionary(kvp => kvp.Key, kvp => kvp.Value.Restore(param));
        }

        [Key(0)]
        public ReadOnlyDictionary<string, IReferRestorationKey> KeyToRestorationKey {
            get {
                if (keyToRestorationKey == null) {
                    keyToRestorationKey = new Dictionary<string, IReferRestorationKey>();
                }
                return new ReadOnlyDictionary<string, IReferRestorationKey>(keyToRestorationKey);
            }
            set {
                keyToRestorationKey = new Dictionary<string, IReferRestorationKey>(value);
            }
        }

        private Dictionary<string, IReferRestorationKey> keyToRestorationKey;

        public void Add(string SourceKey, IReferRestorationKey restorationKey) {
            keyToRestorationKey.Add(SourceKey, restorationKey);
        }

        public void Remove(string sourceKey) {
            keyToRestorationKey.Remove(sourceKey);
        }

        public MoleculeMsReference Refer(MsScanMatchResult result) {
            if (KeyToRefer.TryGetValue(result.SourceKey, out var refer)) {
                return refer.Refer(result);
            }
            return null;
        }
    }
}