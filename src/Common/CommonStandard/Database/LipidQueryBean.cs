using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using MessagePack;

namespace Rfx.Riken.OsakaUniv
{
    [DataContract]
    [MessagePackObject]
    public class LipidQueryBean
    {
        [DataMember]
        private IonMode ionMode;
        [DataMember]
        private CollisionType collisionType;
        [DataMember]
        private SolventType solventType;
        [DataMember]
        private List<LbmQuery> lbmQueries;

        public LipidQueryBean()
        {
            this.lbmQueries = new List<LbmQuery>();
        }

        [Key(0)]
        public IonMode IonMode
        {
            get { return ionMode; }
            set { ionMode = value; }
        }

        [Key(1)]
        public CollisionType CollisionType
        {
            get { return collisionType; }
            set { collisionType = value; }
        }

        [Key(2)]
        public SolventType SolventType
        {
            get { return solventType; }
            set { solventType = value; }
        }

        [Key(3)]
        public List<LbmQuery> LbmQueries
        {
            get { return lbmQueries; }
            set { lbmQueries = value; }
        }
    }
}
