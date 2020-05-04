using MessagePack;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CompMs.MsdialCore.DataObj {
    public class StandardCompound {
        public string StandardName { get; set; }
        public double MolecularWeight { get; set; }
        public double Concentration { get; set; } // uM
        public string TargetClass { get; set; } // for lipids. "Any others" means that the standard is applied to all of peaks.
        public double DilutionRate { get; set; } // default 1
        public int PeakID { get; set; } // is used for normalization
    }
}
