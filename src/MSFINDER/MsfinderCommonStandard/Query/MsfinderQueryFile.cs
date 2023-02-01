using Rfx.Riken.OsakaUniv;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
//using System.Windows.Media;

namespace Riken.Metabolomics.MsfinderCommon.Query {
    /// <summary>
    /// mat: raw data file
    /// fgt: formula finder result file
    /// sfd: structure finder result file
    /// fdb: formula database
    /// ndb: neutral loss database
    /// </summary>
    public enum SaveFileFormat { msp, mat, fgt, sfd, fdb, ndb, efd, esd, apf, anf, msd }

    public class MsfinderQueryFile {
        private string rawDataFilePath;
        private string rawDataFileName;
        private string formulaFilePath;
        private string formulaFileName;
        private string structureFolderPath;
        private string structureFolderName;
        //private SolidColorBrush bgColor; // temp
        public MsfinderQueryFile() {
            //this.bgColor = Brushes.White;
        }

        public string RawDataFilePath {
            get { return rawDataFilePath; }
            set { rawDataFilePath = value; }
        }

        public string RawDataFileName {
            get { return rawDataFileName; }
            set { rawDataFileName = value; }
        }

        public string FormulaFilePath {
            get { return formulaFilePath; }
            set { formulaFilePath = value; }
        }

        public string FormulaFileName {
            get { return formulaFileName; }
            set { formulaFileName = value; }
        }

        public string StructureFolderPath {
            get { return structureFolderPath; }
            set { structureFolderPath = value; }
        }

        public string StructureFolderName {
            get { return structureFolderName; }
            set { structureFolderName = value; }
        }

        //public SolidColorBrush BgColor {
        //    get {
        //        return bgColor;
        //    }

        //    set {
        //        bgColor = value; OnPropertyChanged("BgColor");
        //    }
        //}
    }
}
