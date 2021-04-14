using CompMs.Common.Components;
using CompMs.Common.DataObj.Result;
using CompMs.MsdialCore.Algorithm.Annotation;
using MessagePack;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace CompMs.MsdialCore.DataObj
{
    [MessagePackObject]
    public class DataBaseMapper : IMatchResultRefer
    {
        [IgnoreMember]
        public ReadOnlyDictionary<string, IMatchResultRefer> KeyToRefer {
            get {
                if (keyToRefer == null) {
                    keyToRefer = new Dictionary<string, IMatchResultRefer>();
                }
                return new ReadOnlyDictionary<string, IMatchResultRefer>(keyToRefer);
            }
        }

        [Key(0)]
        private Dictionary<string, IMatchResultRefer> keyToRefer;

        public MoleculeMsReference Refer(MsScanMatchResult result) {
            if (keyToRefer.TryGetValue(result.SourceKey, out var refer)) {
                return refer.Refer(result);
            }
            return null;
        }
    }
}