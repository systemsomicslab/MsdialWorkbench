using MessagePack;
using System;
using System.Collections.Generic;
using System.Text;

namespace CompMs.MsdialCore.DataObj {
    [MessagePackObject]
    public class ProteinGroup {
        public ProteinGroup() { }
        [SerializationConstructor]
        public ProteinGroup(int id, List<ProteinMsResult> results) {
            GroupID = id;
            ProteinMsResults = results;
        }
        [Key(0)]
        public int GroupID { get; } = -1;
        [Key(1)]
        public List<ProteinMsResult> ProteinMsResults { get; } = new List<ProteinMsResult>();
    }
}
