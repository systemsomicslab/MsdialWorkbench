using MessagePack;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Rfx.Riken.OsakaUniv {
    [MessagePackObject]
    public class StandardCompound {
        private string standardName;
        private double molecularWeight;
        private double concentration; // uM
        private string targetClass; // for lipids. "Any others" means that the standard is applied to all of peaks.
        private double dilutionRate; // default 1
        private int peakID; // is used for normalization

        public StandardCompound() {
            dilutionRate = 1.0;
            peakID = -1;
        }

        [Key(0)]
        public string StandardName {
            get { return standardName; }
            set { standardName = value; }
        }

        [Key(1)]
        public double MolecularWeight {
            get { return molecularWeight; }
            set { molecularWeight = value; }
        }

        [Key(2)]
        public double Concentration {
            get { return concentration; }
            set { concentration = value; }
        }

        [Key(3)]
        public string TargetClass {
            get { return targetClass; }
            set { targetClass = value; }
        }

        [Key(4)]
        public double DilutionRate {
            get { return dilutionRate; }
            set { dilutionRate = value; }
        }

        [Key(5)]
        public int PeakID {
            get { return peakID; }
            set { peakID = value; }
        }
    }
}
