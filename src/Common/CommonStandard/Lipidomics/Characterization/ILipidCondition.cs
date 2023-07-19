using System;
using System.Collections.Generic;
using System.Text;

namespace CompMs.Common.Lipidomics.Characterization
{
    internal interface ILipidCondition
    {
        bool Filter(LipidMolecule molecule);
    }
}
