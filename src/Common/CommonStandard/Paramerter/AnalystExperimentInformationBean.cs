using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization;
using System.Text;
using MessagePack;

namespace Rfx.Riken.OsakaUniv
{
    [Obfuscation(Feature = "renaming")]
    public enum MsType
    {
        SCAN = 0, SWATH = 1, AIF = 2
    }

    [DataContract]
    [Obfuscation(Feature = "renaming", ApplyToMembers = true)]
    [MessagePackObject]
    public class AnalystExperimentInformationBean
    {
        [DataMember]
        int experimentNumber;
        [DataMember]
        MsType msType;
        [DataMember]
        float startMz;
        [DataMember]
        float endMz;

        // for AIF
        [DataMember]
        string name;
        [DataMember]
        float collisionEnergy;
        [DataMember]
        int checkDecTarget;

        [Key(0)]
        public int ExperimentNumber
        {
            get { return experimentNumber; }
            set { experimentNumber = value; }
        }

        [Key(1)]
        public MsType MsType
        {
            get { return msType; }
            set { msType = value; }
        }

        [Key(2)]
        public float StartMz
        {
            get { return startMz; }
            set { startMz = value; }
        }

        [Key(3)]
        public float EndMz
        {
            get { return endMz; }
            set { endMz = value; }
        }

        [Key(4)]
        public string Name {
            get { return name; }
            set { name = value; }
        }

        [Key(5)]
        public float CollisionEnergy {
            get { return collisionEnergy; }
            set { collisionEnergy = value; }
        }

        [Key(6)]
        public int CheckDecTarget {
            get { return checkDecTarget; }
            set { checkDecTarget = value; }
        }

    }
}
