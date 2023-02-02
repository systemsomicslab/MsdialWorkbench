using MessagePack;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Windows.Input;

namespace Rfx.Riken.OsakaUniv
{
    [DataContract]
    [MessagePackObject]
    public class ExtractedIonChromatogramDisplaySettingBean 
    {
        [DataMember]
        private string eicName;
        [DataMember]
        private double? exactMass;
        [DataMember]
        private double? massTolerance;

        [Key(0)]
        public string EicName
        {
            get { return eicName; }
            set { eicName = value; }
        }

        [Key(1)]
        public double? ExactMass
        {
            get { return exactMass; }
            set { exactMass = value; }
        }

        [Key(2)]
        public double? MassTolerance
        {
            get { return massTolerance; }
            set { massTolerance = value; }
        }
    }
}
