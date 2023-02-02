using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using MessagePack;

namespace Rfx.Riken.OsakaUniv
{
    /// <summary>
    /// This is the old storage of adduct ion information. But still be used in MS-DIAL program since a lot of users already stated to use MS-DIAL progarm with this storage...
    /// </summary>
    [DataContract]
    [MessagePackObject]
    public class AdductIonInformationBean
    {
        [DataMember]
        private string adductName;
        [DataMember]
        private int charge;
        [DataMember]
        private double accurateMass;
        [DataMember]
        private bool included;
        [DataMember]
        private int xmer;
        [DataMember]
        private IonMode ionMode;

        [Key(0)]
        public int Xmer
        {
            get { return xmer; }
            set { xmer = value; }
        }

        [Key(1)]
        public IonMode IonMode
        {
            get { return ionMode; }
            set { ionMode = value; }
        }

        [Key(2)]
        public string AdductName
        {
            get { return adductName; }
            set { adductName = value; }
        }

        [Key(3)]
        public int Charge
        {
            get { return charge; }
            set { charge = value; }
        }

        [Key(4)]
        public double AccurateMass
        {
            get { return accurateMass; }
            set { accurateMass = value; }
        }

        [Key(5)]
        public bool Included
        {
            get { return included; }
            set { included = value; }
        }
    }
}
