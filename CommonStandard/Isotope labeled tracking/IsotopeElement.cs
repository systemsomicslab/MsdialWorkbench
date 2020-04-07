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
    public class IsotopeElement
    {
        [DataMember]
        private string elementName;
        [DataMember]
        private float massDifference;

        public IsotopeElement()
        {
            elementName = string.Empty;
            massDifference = 0.0F;
        }

        [Key(0)]
        public string ElementName
        {
            get { return elementName; }
            set { elementName = value; }
        }

        [Key(1)]
        public float MassDifference
        {
            get { return massDifference; }
            set { massDifference = value; }
        }
    }
}
