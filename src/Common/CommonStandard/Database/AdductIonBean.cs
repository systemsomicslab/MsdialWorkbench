using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using MessagePack;

namespace Rfx.Riken.OsakaUniv
{
    public enum IonType
    {
        Positive, Negative, Unknown
    }

    /// <summary>
    /// This adduct ion storage is the old version as the adduct ion storages. 
    /// However, it still be used in MS-DIAL program since a lot of users already stated to use MS-DIAL progarm with this storage...
    /// </summary>
    [DataContract]
    [MessagePackObject]
    public class AdductIonBean
    {
        [DataMember]
        private float adductIonAccurateMass;
        [DataMember]
        private int adductIonXmer;
        [DataMember]
        private string adductIonName;
        [DataMember]
        private int chargeNumber;
        [DataMember]
        private IonType ionType;
        [DataMember]
        private bool formatCheck;

        public AdductIonBean()
        {
            adductIonAccurateMass = -1;
            adductIonName = string.Empty;
            chargeNumber = -1;
            adductIonXmer = -1;
            ionType = IonType.Unknown;
            formatCheck = false;
        }

        [Key(0)]
        public float AdductIonAccurateMass
        {
            get { return adductIonAccurateMass; }
            set { adductIonAccurateMass = value; }
        }

        [Key(1)]
        public int AdductIonXmer
        {
            get { return adductIonXmer; }
            set { adductIonXmer = value; }
        }

        [Key(2)]
        public string AdductIonName
        {
            get { return adductIonName; }
            set { adductIonName = value; }
        }

        [Key(3)]
        public int ChargeNumber
        {
            get { return chargeNumber; }
            set { chargeNumber = value; }
        }

        [Key(4)]
        public IonType IonType
        {
            get { return ionType; }
            set { ionType = value; }
        }

        [Key(5)]
        public bool FormatCheck
        {
            get { return formatCheck; }
            set { formatCheck = value; }
        }
    }


   

}
