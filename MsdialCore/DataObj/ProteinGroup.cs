using MessagePack;
using System;
using System.Collections.Generic;
using System.Text;

namespace CompMs.MsdialCore.DataObj {
    [MessagePackObject]
    public class ProteinGroup {
        public ProteinGroup() { }

        [SerializationConstructor]
        public ProteinGroup(int groupID, List<ProteinMsResult> proteinResults) {
            GroupID = groupID;
            ProteinMsResults = proteinResults;
        }
        [Key(0)]
        public int GroupID { get; } = -1;
        [Key(1)]
        public List<ProteinMsResult> ProteinMsResults { get; } = new List<ProteinMsResult>();
    }
}
