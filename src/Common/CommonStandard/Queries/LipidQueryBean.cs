using CompMs.Common.Enum;
using MessagePack;
using System.Collections.Generic;

namespace CompMs.Common.Query {
    [MessagePackObject]
    public class LipidQueryBean
    {
        public LipidQueryBean() { }

        [Key(0)]
        public IonMode IonMode { get; set; }
        [Key(1)]
        public CollisionType CollisionType { get; set; }
        [Key(2)]
        public SolventType SolventType { get; set; }
        [Key(3)]
        public List<LbmQuery> LbmQueries { get; set; } = new List<LbmQuery>();
    }
}
