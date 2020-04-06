using System;
using System.Collections.Generic;
using System.Text;
using CompMs.Common.Interfaces;

namespace CompMs.Common.Components
{
    public class MoleculeProperty: IMoleculeProperty
    {
        #region Properties
        public int ID { get; set; }
        public double PrecursorMz { get; set; }
        public Times Times { get; set; }
        #endregion

        public MoleculeProperty() { }
        public MoleculeProperty(int id, double mz, RetentionTime rt) {
            ID = id;
            PrecursorMz = mz;
            Times = new Times() { Absolute = rt };            
        }

        public MoleculeProperty(int id, double mz, RetentionIndex ri)
        {
            ID = id;
            PrecursorMz = mz;
            Times = new Times() { Relative = ri, IsAbsolute = false };
        }
    }
}
