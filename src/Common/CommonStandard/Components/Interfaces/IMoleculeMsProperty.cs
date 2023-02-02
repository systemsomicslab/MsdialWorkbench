using CompMs.Common.DataObj.Property;
using System;
using System.Collections.Generic;
using System.Text;

namespace CompMs.Common.Interfaces {
    public interface IMoleculeMsProperty : IMSScanProperty, IMoleculeProperty, IMSIonProperty { // especially used for library record
    }
}
