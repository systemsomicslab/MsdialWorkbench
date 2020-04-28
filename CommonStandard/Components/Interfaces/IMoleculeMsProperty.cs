using CompMs.Common.DataObj;
using System;
using System.Collections.Generic;
using System.Text;

namespace CompMs.Common.Interfaces {
    public interface IMoleculeMsProperty : IMSScanProperty, IMoleculeProperty { // especially used for library record
        // Molecule ion metadata
        AdductType AdductType { get; set; } 
        double CollisionCrossSection { get; set; }
    }
}
