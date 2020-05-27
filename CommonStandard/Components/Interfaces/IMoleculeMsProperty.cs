using CompMs.Common.DataObj.Property;
using System;
using System.Collections.Generic;
using System.Text;

namespace CompMs.Common.Interfaces {
    public interface IMoleculeMsProperty : IMSScanProperty, IMoleculeProperty { // especially used for library record
        // Molecule ion metadata
        AdductIon AdductType { get; set; } 
        double CollisionCrossSection { get; set; }
    }
}
