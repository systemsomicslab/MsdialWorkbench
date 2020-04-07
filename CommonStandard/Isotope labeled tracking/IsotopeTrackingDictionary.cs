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
    public class IsotopeTrackingDictionary
    {
        [DataMember]
        private List<IsotopeElement> isotopeElements;
        [DataMember]
        private int selectedID;

        public IsotopeTrackingDictionary()
        {
            isotopeElements = new List<IsotopeElement>();

            isotopeElements.Add(new IsotopeElement() { ElementName = "13C", MassDifference = 1.003354838F });
            isotopeElements.Add(new IsotopeElement() { ElementName = "15N", MassDifference = 0.997034893F });
            isotopeElements.Add(new IsotopeElement() { ElementName = "34S", MassDifference = 1.9957959F });
            isotopeElements.Add(new IsotopeElement() { ElementName = "18O", MassDifference = 1.9979535F });
            isotopeElements.Add(new IsotopeElement() { ElementName = "13C+15N", MassDifference = 2.000389731F });
            isotopeElements.Add(new IsotopeElement() { ElementName = "13C+34S", MassDifference = 2.999150738F });
            isotopeElements.Add(new IsotopeElement() { ElementName = "15N+34S", MassDifference = 2.992830793F });
            isotopeElements.Add(new IsotopeElement() { ElementName = "13C+15S+34S", MassDifference = 3.996185631F });
            isotopeElements.Add(new IsotopeElement() { ElementName = "Deuterium", MassDifference = 1.006276745F });

            selectedID = 0;
        }

        [Key(0)]
        public List<IsotopeElement> IsotopeElements
        {
            get { return isotopeElements; }
            set { isotopeElements = value; }
        }

        [Key(1)]
        public int SelectedID
        {
            get { return selectedID; }
            set { selectedID = value; }
        }
    }
}
